'use strict';

/**
 * Item model factory.
 * Cosmos DB document shape for the "items" container.
 * Partition key: /id
 */

const { v4: uuidv4 } = require('uuid');

/**
 * Creates a new Item document ready for insertion.
 * @param {object} data - { name, description, category, price }
 * @returns {object} Cosmos DB document
 */
function createItem({ name, description = '', category = 'general', price = 0 }) {
  if (!name || typeof name !== 'string' || name.trim() === '') {
    throw new Error('Item name is required and must be a non-empty string');
  }
  if (typeof price !== 'number' || price < 0) {
    throw new Error('Item price must be a non-negative number');
  }

  return {
    id: uuidv4(),
    name: name.trim(),
    description: String(description).trim(),
    category: String(category).trim().toLowerCase(),
    price: Number(price),
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
}

/**
 * Merges update fields onto an existing item document.
 * @param {object} existing - Current Cosmos DB document
 * @param {object} updates  - Partial fields to update
 * @returns {object} Updated document
 */
function updateItem(existing, updates) {
  const allowed = ['name', 'description', 'category', 'price'];
  const merged = { ...existing };

  for (const key of allowed) {
    if (updates[key] !== undefined) {
      merged[key] = updates[key];
    }
  }

  if (merged.price < 0) {
    throw new Error('Item price must be a non-negative number');
  }

  merged.updatedAt = new Date().toISOString();
  return merged;
}

module.exports = { createItem, updateItem };
