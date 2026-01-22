const express = require('express');
const router = express.Router();
const multer = require('multer');
const upload = multer();
const { detectFace, verifyFaces, detectLiveness } = require('../services/azureFaceService');

// Receive webcam image for liveness
router.post('/detect', upload.single('selfie'), async (req, res) => {
  try {
    const imageBuffer = req.file.buffer;
    const liveness = await detectLiveness(imageBuffer);
    const faceId = liveness.isLive ? await detectFace(imageBuffer) : null;
    res.json({
      liveness,
      faceId
    });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// Receive ID photo and verify against liveness
router.post('/verify', upload.fields([{ name: 'selfie' }, { name: 'id_image' }]), async (req, res) => {
  try {
    const selfieBuffer = req.files['selfie'][0].buffer;
    const idBuffer = req.files['id_image'][0].buffer;
    const faceIdLive = await detectFace(selfieBuffer);
    const faceIdDoc = await detectFace(idBuffer);

    if (!faceIdLive || !faceIdDoc) {
      return res.status(400).json({error: 'Face not detected in one or both images.'});
    }

    const verification = await verifyFaces(faceIdLive, faceIdDoc);
    res.json({ verification });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

module.exports = router;
