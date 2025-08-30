const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 3000;

const MIME_TYPES = {
  '.html': 'text/html',
  '.css': 'text/css',
  '.js': 'text/javascript',
  '.json': 'application/json',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif'
};

const server = http.createServer((req, res) => {
  console.log(`Request received: ${req.url}`);
  
  // Handle root path
  let filePath = req.url === '/' ? './test-image.html' : '.' + req.url;
  
  // Get the file extension
  const extname = path.extname(filePath).toLowerCase();
  
  // Set the content type
  const contentType = MIME_TYPES[extname] || 'application/octet-stream';
  
  // Read the file
  fs.readFile(filePath, (err, data) => {
    if (err) {
      if (err.code === 'ENOENT') {
        // Try looking in assets directly
        if (req.url.includes('images/')) {
          const assetPath = './assets/' + req.url.split('/').pop();
          console.log(`File not found, trying: ${assetPath}`);
          
          fs.readFile(assetPath, (err2, data2) => {
            if (err2) {
              res.writeHead(404);
              res.end('File not found');
              return;
            }
            
            res.writeHead(200, { 'Content-Type': contentType });
            res.end(data2);
          });
          return;
        }
        
        res.writeHead(404);
        res.end('File not found');
        return;
      }
      
      res.writeHead(500);
      res.end('Server error');
      return;
    }
    
    res.writeHead(200, { 'Content-Type': contentType });
    res.end(data);
  });
});

server.listen(PORT, () => {
  console.log(`Server running at http://localhost:${PORT}/`);
  console.log(`View the test page at http://localhost:${PORT}/test-image.html`);
}); 