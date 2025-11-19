// Minimal test to check if server can start
const express = require('express');
const app = express();
const PORT = 3001;

app.get('/test', (req, res) => {
  res.json({ message: 'Test successful' });
});

const server = app.listen(PORT, () => {
  console.log(`✅ Minimal server running on http://localhost:${PORT}`);
  console.log(`Test with: curl http://localhost:${PORT}/test`);
});

server.on('error', (err) => {
  console.error('❌ Server error:', err.message);
  process.exit(1);
});

// Keep alive
process.on('SIGINT', () => {
  console.log('\n🛑 Shutting down...');
  server.close(() => {
    console.log('✅ Server closed');
    process.exit(0);
  });
});
