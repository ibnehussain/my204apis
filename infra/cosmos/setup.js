'use strict';

/**
 * Cosmos DB provisioning script.
 * Creates the database and container if they don't already exist.
 *
 * Usage (Managed Identity / Azure CLI login):
 *   $env:COSMOS_ENDPOINT="https://<account>.documents.azure.com:443/"
 *   node infra/cosmos/setup.js
 *
 * No key required — authenticates via DefaultAzureCredential
 * (Azure CLI locally, Managed Identity on Azure).
 */

require('dotenv').config();

const { CosmosClient } = require('@azure/cosmos');
const { DefaultAzureCredential } = require('@azure/identity');

const endpoint = process.env.COSMOS_ENDPOINT;
const databaseId = process.env.COSMOS_DATABASE_ID || 'my204db';
const containerId = process.env.COSMOS_CONTAINER_ID || 'items';

if (!endpoint) {
  console.error('Error: COSMOS_ENDPOINT must be set.');
  process.exit(1);
}

async function setup() {
  const credential = new DefaultAzureCredential();
  const client = new CosmosClient({ endpoint, aadCredentials: credential });

  console.log(`Creating database "${databaseId}" if not exists...`);
  const { database } = await client.databases.createIfNotExists({ id: databaseId });
  console.log(`  ✓ Database: ${database.id}`);

  console.log(`Creating container "${containerId}" if not exists...`);
  const { container } = await database.containers.createIfNotExists({
    id: containerId,
    partitionKey: { paths: ['/id'] },
    indexingPolicy: {
      indexingMode: 'consistent',
      automatic: true,
      includedPaths: [{ path: '/*' }],
      excludedPaths: [{ path: '/"_etag"/?' }],
    },
  });
  console.log(`  ✓ Container: ${container.id}`);

  // Seed sample data only if container is empty
  const { resources: existingItems } = await container.items
    .query('SELECT VALUE COUNT(1) FROM c')
    .fetchAll();

  if (existingItems[0] === 0) {
    console.log('Seeding sample data...');
    const sampleData = require('./sampleData.json');
    for (const item of sampleData) {
      await container.items.create(item);
      console.log(`  ✓ Seeded item: ${item.name}`);
    }
  } else {
    console.log(`  ℹ Container already has ${existingItems[0]} item(s), skipping seed.`);
  }

  console.log('\nSetup complete.');
}

setup().catch((err) => {
  console.error('Setup failed:', err.message);
  process.exit(1);
});
