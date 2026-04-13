'use strict';

const { CosmosClient } = require('@azure/cosmos');
const { DefaultAzureCredential } = require('@azure/identity');

const endpoint = process.env.COSMOS_ENDPOINT;
const databaseId = process.env.COSMOS_DATABASE_ID || 'my204db';
const containerId = process.env.COSMOS_CONTAINER_ID || 'items';

if (!endpoint) {
  throw new Error('COSMOS_ENDPOINT environment variable is required');
}

// Uses Managed Identity on Azure App Service automatically.
// Falls back to Azure CLI / VS Code credentials locally.
const credential = new DefaultAzureCredential();

const client = new CosmosClient({ endpoint, aadCredentials: credential });
const database = client.database(databaseId);
const container = database.container(containerId);

module.exports = { client, database, container };
