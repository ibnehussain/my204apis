'use strict';

/**
 * Bulk upload script — uploads all documents from documents.json
 * into the Cosmos DB container.
 *
 * Authentication: DefaultAzureCredential
 *   - Locally : az login
 *   - Azure   : Managed Identity
 *
 * Usage:
 *   $env:COSMOS_ENDPOINT="https://<account>.documents.azure.com:443/"
 *   node infra/cosmos/upload.js
 *
 * Options:
 *   --upsert    Replace existing documents (default: skip if id exists)
 */

require('dotenv').config();

const { CosmosClient } = require('@azure/cosmos');
const { DefaultAzureCredential } = require('@azure/identity');
const documents = require('./documents.json');

const endpoint = process.env.COSMOS_ENDPOINT;
const databaseId = process.env.COSMOS_DATABASE_ID || 'my204db';
const containerId = process.env.COSMOS_CONTAINER_ID || 'items';
const useUpsert = process.argv.includes('--upsert');

if (!endpoint) {
  console.error('Error: COSMOS_ENDPOINT must be set.');
  process.exit(1);
}

async function upload() {
  const credential = new DefaultAzureCredential();
  const client = new CosmosClient({ endpoint, aadCredentials: credential });
  const container = client.database(databaseId).container(containerId);

  console.log(`\nUploading ${documents.length} document(s) to "${databaseId}/${containerId}"`);
  console.log(`Mode: ${useUpsert ? 'upsert (overwrite)' : 'create (skip existing)'}\n`);

  let succeeded = 0;
  let skipped = 0;
  let failed = 0;

  for (const doc of documents) {
    try {
      if (useUpsert) {
        await container.items.upsert(doc);
      } else {
        await container.items.create(doc);
      }
      console.log(`  ✓ ${doc.id}  "${doc.name}"`);
      succeeded++;
    } catch (err) {
      if (err.code === 409) {
        console.log(`  ⟳ ${doc.id}  "${doc.name}" — already exists, skipped`);
        skipped++;
      } else {
        console.error(`  ✗ ${doc.id}  "${doc.name}" — ${err.message}`);
        failed++;
      }
    }
  }

  console.log(`\n── Summary ──────────────────────`);
  console.log(`  Uploaded : ${succeeded}`);
  console.log(`  Skipped  : ${skipped}`);
  console.log(`  Failed   : ${failed}`);
  console.log(`─────────────────────────────────\n`);

  if (failed > 0) process.exit(1);
}

upload().catch((err) => {
  console.error('Upload failed:', err.message);
  process.exit(1);
});
