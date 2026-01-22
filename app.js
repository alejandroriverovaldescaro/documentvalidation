// Basic Node.js Express server setup for liveness detection and face verification

const express = require('express');
const path = require('path');
const multer = require('multer');
const dotenv = require('dotenv');
const livenessRoutes = require('./routes/liveness');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 3000;

// File upload setup for ID documents
const upload = multer({ dest: 'uploads/' });

app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

// API routes
app.use('/api/liveness', livenessRoutes);

// For file uploads (ID document)
app.post('/upload-id', upload.single('id_image'), (req, res) => {
  if (!req.file) return res.status(400).json({error: 'No file'});
  res.json({ filepath: req.file.path, filename: req.file.originalname });
});

// Start
app.listen(PORT, () => console.log(`Server running on http://localhost:${PORT}`));
