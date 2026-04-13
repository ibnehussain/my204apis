'use strict';

const { container } = require('../config/cosmosClient');
const { createItem, updateItem } = require('../models/item');

// ── GET /api/items ────────────────────────────────────────────────────────────
async function getAllItems(req, res, next) {
  try {
    const category = req.query.category;
    let querySpec;

    if (category) {
      querySpec = {
        query: 'SELECT * FROM c WHERE c.category = @category ORDER BY c.createdAt DESC',
        parameters: [{ name: '@category', value: category.toLowerCase() }],
      };
    } else {
      querySpec = { query: 'SELECT * FROM c ORDER BY c.createdAt DESC' };
    }

    const { resources } = await container.items.query(querySpec).fetchAll();
    res.status(200).json({ count: resources.length, items: resources });
  } catch (err) {
    next(err);
  }
}

// ── GET /api/items/:id ────────────────────────────────────────────────────────
async function getItemById(req, res, next) {
  try {
    const { id } = req.params;
    const { resource } = await container.item(id, id).read();

    if (!resource) {
      return res.status(404).json({ error: `Item '${id}' not found` });
    }

    res.status(200).json(resource);
  } catch (err) {
    if (err.code === 404) {
      return res.status(404).json({ error: `Item '${req.params.id}' not found` });
    }
    next(err);
  }
}

// ── POST /api/items ───────────────────────────────────────────────────────────
async function createNewItem(req, res, next) {
  try {
    const doc = createItem(req.body);
    const { resource } = await container.items.create(doc);
    res.status(201).json(resource);
  } catch (err) {
    if (err.message.includes('required') || err.message.includes('must be')) {
      return res.status(400).json({ error: err.message });
    }
    next(err);
  }
}

// ── PUT /api/items/:id ────────────────────────────────────────────────────────
async function updateExistingItem(req, res, next) {
  try {
    const { id } = req.params;
    const { resource: existing } = await container.item(id, id).read();

    if (!existing) {
      return res.status(404).json({ error: `Item '${id}' not found` });
    }

    const updated = updateItem(existing, req.body);
    const { resource } = await container.item(id, id).replace(updated);
    res.status(200).json(resource);
  } catch (err) {
    if (err.code === 404) {
      return res.status(404).json({ error: `Item '${req.params.id}' not found` });
    }
    if (err.message.includes('must be')) {
      return res.status(400).json({ error: err.message });
    }
    next(err);
  }
}

// ── DELETE /api/items/:id ─────────────────────────────────────────────────────
async function deleteItem(req, res, next) {
  try {
    const { id } = req.params;
    await container.item(id, id).delete();
    res.status(204).send();
  } catch (err) {
    if (err.code === 404) {
      return res.status(404).json({ error: `Item '${req.params.id}' not found` });
    }
    next(err);
  }
}

module.exports = { getAllItems, getItemById, createNewItem, updateExistingItem, deleteItem };
