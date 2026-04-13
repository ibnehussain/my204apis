'use strict';

const { Router } = require('express');
const {
  getAllItems,
  getItemById,
  createNewItem,
  updateExistingItem,
  deleteItem,
} = require('../controllers/itemsController');

const router = Router();

router.get('/', getAllItems);
router.get('/:id', getItemById);
router.post('/', createNewItem);
router.put('/:id', updateExistingItem);
router.delete('/:id', deleteItem);

module.exports = router;
