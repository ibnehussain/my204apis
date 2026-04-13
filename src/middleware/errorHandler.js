'use strict';

/**
 * Global error handler middleware.
 * Must be registered LAST in the Express middleware chain.
 */
function errorHandler(err, req, res, _next) {
  console.error(`[Error] ${req.method} ${req.path} →`, err.message || err);

  const status = err.status || err.code || 500;
  const httpStatus = typeof status === 'number' && status >= 100 && status < 600 ? status : 500;

  res.status(httpStatus).json({
    error: httpStatus === 500 ? 'Internal server error' : err.message,
  });
}

module.exports = errorHandler;
