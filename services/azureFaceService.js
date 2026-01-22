// Azure Face API and Liveness integration

const axios = require('axios');
const fs = require('fs');
const AZURE_FACE_ENDPOINT = process.env.AZURE_FACE_ENDPOINT;
const AZURE_FACE_KEY = process.env.AZURE_FACE_KEY;

// Detect faces and get faceIds
async function detectFace(imageBuffer) {
  const url = `${AZURE_FACE_ENDPOINT}/face/v1.0/detect?returnFaceId=true`;
  const resp = await axios.post(url, imageBuffer, {
    headers: {
      'Ocp-Apim-Subscription-Key': AZURE_FACE_KEY,
      'Content-Type': 'application/octet-stream'
    }
  });
  // Return first detected faceId
  return resp.data && resp.data[0]?.faceId;
}

// Face verification (compare two faceIds)
async function verifyFaces(faceId1, faceId2) {
  const url = `${AZURE_FACE_ENDPOINT}/face/v1.0/verify`;
  const resp = await axios.post(url, { faceId1, faceId2 }, {
    headers: {
      'Ocp-Apim-Subscription-Key': AZURE_FACE_KEY,
      'Content-Type': 'application/json'
    }
  });
  return resp.data; // {isIdentical, confidence}
}

// (Mock) Azure Face Liveness API integration
// Replace with actual Azure API when available
async function detectLiveness(imageBuffer) {
  // If you have Azure Liveness API, call that here
  // For demo, just always return "true"
  return { isLive: true, score: 0.99 };
}

module.exports = {
  detectFace,
  verifyFaces,
  detectLiveness
};
