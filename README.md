# Document Validation - Face Verification Feature

A Blazor web application that provides facial verification capability by comparing a live camera capture of a user's face against a photo extracted from their uploaded ID document.

## Features

### 1. ID Document Upload
- Upload photos of ID documents (passport, driver's license, etc.)
- Supports JPEG and PNG formats
- Maximum file size: 10MB
- Real-time preview of uploaded document

### 2. Live Face Capture
- Access device camera using browser MediaDevices API
- Live camera preview with face guidance overlay
- Lighting quality detection with user feedback
- Capture and retake functionality
- Works on both desktop and mobile browsers

### 3. Face Verification
- Compares ID document photo with live captured photo
- Returns confidence score (0-100%)
- Minimum threshold: 70% for successful match
- Displays detailed verification results
- Side-by-side comparison view

### 4. API Endpoints
- `POST /api/FaceVerification/verify` - Verify face match
- `GET /api/FaceVerification/history/{documentId}` - Get verification history
- `GET /api/FaceVerification/status/{verificationId}` - Get verification status

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- Modern web browser with camera support (Chrome, Edge, Firefox, Safari)
- HTTPS connection (required for camera access)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/alejandroriverovaldescaro/documentvalidation.git
cd documentvalidation
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the project:
```bash
dotnet build
```

4. Run the application:
```bash
dotnet run
```

5. Navigate to the application in your browser (default: https://localhost:5001)

### Configuration

The face verification settings can be configured in `appsettings.json`:

```json
{
  "FaceVerification": {
    "Provider": "Mock",
    "ApiKey": "",
    "Endpoint": "",
    "ConfidenceThreshold": 70.0,
    "MaxPhotoSizeMB": 10,
    "AllowedFormats": ["image/jpeg", "image/png"],
    "StoreLivePhotos": true,
    "PhotoStoragePath": "verifications/live-photos"
  }
}
```

## Usage

### Using the Face Verification Feature

1. **Navigate to Face Verification** - Click on "Face Verification" in the navigation menu
2. **Upload ID Document Photo** - Select a photo of your ID document
3. **Capture Live Photo** - Use your camera to capture a live photo
4. **View Results** - See the verification results with confidence score

## Project Structure

```
documentvalidation/
├── Components/Pages/
│   ├── FaceCapture.razor       # Camera capture component
│   └── FaceVerification.razor  # Main verification page
├── Controllers/
│   └── FaceVerificationController.cs  # API endpoints
├── Models/                     # Data models
├── Services/                   # Face verification service
├── Database/                   # SQL scripts
└── wwwroot/js/
    └── camera.js              # Camera JavaScript interop
```

## Security Considerations

- **HTTPS Required**: Camera access requires HTTPS connection
- **File Validation**: Both client and server validate file types and sizes
- **Data Privacy**: Comply with GDPR and biometric data regulations
- **Audit Logging**: All verification attempts are logged

## Current Implementation

The current implementation uses a **mock face verification service** for demonstration purposes. For production use, integrate with Azure Face API, AWS Rekognition, or similar services.

## License

MIT License