// Import Express
const express = require('express');

// Create an Express application
const app = express();

// Set the port to 3000 or use the value from process.env.PORT
const PORT = process.env.PORT || 3000;

// Local array of Azure certification facts and Microsoft Learn tips
const azureFacts = [
  "AZ-900 is the Microsoft Azure Fundamentals certification, ideal for beginners.",
  "AZ-104 is for Azure Administrators and covers managing Azure resources.",
  "AZ-400 focuses on DevOps solutions on Azure.",
  "AZ-305 is for designing Azure infrastructure solutions.",
  "SC-900 covers Microsoft Security, Compliance, and Identity Fundamentals.",
  "Microsoft Learn offers free interactive training for Azure certifications.",
  "Hands-on labs in Microsoft Learn help reinforce Azure concepts.",
  "You can schedule Azure certification exams online at your convenience.",
  "Azure certifications are updated regularly to match cloud technology changes.",
  "Passing AZ-900 does not require prior cloud experience.",
  "AZ-204 is targeted at developers building cloud solutions on Azure.",
  "The exam focuses on developing Azure compute solutions like Azure Functions and Web Apps.",
  "Covers Azure Storage including Blob, Cosmos DB, and Table Storage.",
  "Tests knowledge of secure cloud solutions, including authentication, authorization, and key management.",
  "Includes monitoring, troubleshooting, and performance tuning for Azure apps.",
  "Requires knowledge of app services, containers, and serverless architecture.",
  "Exam tests skills in integrating Azure services like Azure Event Grid, Service Bus, and Logic Apps.",
  "Hands-on experience with REST APIs and SDKs is highly recommended.",
  "Passing AZ-204 validates you can develop and deploy solutions on Azure, not just configure them.",
  "Microsoft Learn provides free learning paths and labs specifically for AZ-204 preparation."
];

// GET /fact endpoint returns a random Azure certification fact
app.get('/fact', (req, res) => {
  try {
    // Pick a random fact from the array
    const fact = azureFacts[Math.floor(Math.random() * azureFacts.length)];
    // Return the fact as JSON
    res.json({ fact });
  } catch (error) {
    // Handle errors and return a 500 status code
    res.status(500).json({ error: 'Failed to retrieve fact.' });
  }
});

// Start the server and listen on the specified port
app.listen(PORT, () => {
  console.log(`Server listening on port ${PORT}`);
});
