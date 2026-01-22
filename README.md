# Azure AI Face Liveness & Document Verification Demo

## Setup

1. Install dependencies:

   ```bash
   npm install express multer axios dotenv
   ```

2. Copy `.env.example` to `.env` and enter your Azure Face API endpoint and key:

   ````
   AZURE_FACE_ENDPOINT=https://<your-face-api-endpoint>.cognitiveservices.azure.com
   AZURE_FACE_KEY=<your-face-api-key>
   ```

3. Start the app:

   ```bash
   node app.js
   ```

4. Open [http://localhost:3000](http://localhost:3000) in your browser.

## Usage

- Use webcam to capture a live selfie (for liveness detection)
- Upload a passport or ID document photo
- Click 'Verify' to check liveness and match the live face against the document's face

---

**Note:** The actual Azure Face Liveness API integration should replace the `detectLiveness()` function in `services/azureFaceService.js`. The Face API (`detectFace` and `verifyFaces`) must be enabled in your Azure account.
