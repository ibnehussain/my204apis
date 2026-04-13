'use strict';

const { CosmosClient } = require('@azure/cosmos');

const endpoint = process.env.COSMOS_ENDPOINT;
const key = process.env.COSMOS_KEY;
const databaseId = process.env.COSMOS_DATABASE_ID || 'my204db';
const containerId = process.env.COSMOS_CONTAINER_ID || 'items';

if (!endpoint || !key) {
  throw new Error('COSMOS_ENDPOINT and COSMOS_KEY environment variables are required');
}

const client = new CosmosClient({ endpoint, key });
const database = client.database(databaseId);
const container = database.container(containerId);

module.exports = { client, database, container };
