# myAPI

A simple Node.js Express API that returns random Azure certification facts.

## Endpoints

- **GET /fact**  
  Returns a random Azure certification fact as JSON:  
  `{ "fact": "..." }`

## Usage

1. Install dependencies:
   ```
   npm install
   ```

2. Start the server:
   ```
   npm start
   ```

3. The API listens on port `3000` or the port specified in `process.env.PORT`.

## Example Response

```json
{
  "fact": "AZ-900 is the Microsoft Azure Fundamentals certification, ideal for beginners."
}
```

## Technologies

- Node.js
- Express

## Notes

- Facts are stored locally in the code.
- Ready to deploy to Azure Web App.
