'use strict';

/**
 * Simple request logger middleware.
 * Logs method, path, status, and response time to stdout.
 * Application Insights picks this up automatically when enabled.
 */
function requestLogger(req, res, next) {
  const start = Date.now();

  res.on('finish', () => {
    const duration = Date.now() - start;
    console.log(`${req.method} ${req.originalUrl} ${res.statusCode} ${duration}ms`);
  });

  next();
}

module.exports = requestLogger;
