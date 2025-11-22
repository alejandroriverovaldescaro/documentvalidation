# Face Verification API Documentation

## Overview
This document describes the REST API endpoints for the Face Verification feature.

## Base URL
```
http://localhost:5000/api
https://yourdomain.com/api
```

## Endpoints

### 1. Verify Face

Compares a live photo against an ID document photo to verify identity.

**Endpoint:** `POST /FaceVerification/verify`

**Content-Type:** `multipart/form-data`

**Request Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| idPhoto | file | Yes | ID document photo (JPEG/PNG, max 10MB) |
| livePhoto | file | Yes | Live captured photo (JPEG/PNG, max 10MB) |
| documentId | string | No | Document identifier (GUID) |

**Example Request (curl):**
```bash
curl -X POST "http://localhost:5000/api/FaceVerification/verify" \
  -F "idPhoto=@/path/to/id-photo.jpg" \
  -F "livePhoto=@/path/to/live-photo.jpg" \
  -F "documentId=123e4567-e89b-12d3-a456-426614174000"
```

**Response (200 OK):**
```json
{
  "isMatch": true,
  "confidenceScore": 85.5,
  "message": "Face verification successful",
  "verificationTimestamp": "2025-11-22T21:30:00Z",
  "warnings": []
}
```

**Response (200 OK - Failed Match):**
```json
{
  "isMatch": false,
  "confidenceScore": 45.2,
  "message": "Face verification failed - confidence score below threshold (70%)",
  "verificationTimestamp": "2025-11-22T21:30:00Z",
  "warnings": [
    "Confidence score below threshold"
  ]
}
```

**Error Responses:**

- `400 Bad Request` - Missing or invalid parameters
```json
{
  "error": "Both ID photo and live photo are required"
}
```

- `500 Internal Server Error` - Server error during verification
```json
{
  "error": "Internal server error during verification"
}
```

---

### 2. Get Verification History

Retrieves the verification history for a specific document.

**Endpoint:** `GET /FaceVerification/history/{documentId}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| documentId | GUID | Yes | Document identifier |

**Example Request:**
```bash
curl "http://localhost:5000/api/FaceVerification/history/123e4567-e89b-12d3-a456-426614174000"
```

**Response (200 OK):**
```json
[
  {
    "verificationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "documentId": "123e4567-e89b-12d3-a456-426614174000",
    "verificationTimestamp": "2025-11-22T21:30:00Z",
    "isMatch": true,
    "confidenceScore": 85.5,
    "livePhotoPath": null,
    "verificationStatus": "Success",
    "errorMessage": null,
    "createdBy": null
  },
  {
    "verificationId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "documentId": "123e4567-e89b-12d3-a456-426614174000",
    "verificationTimestamp": "2025-11-22T20:15:00Z",
    "isMatch": false,
    "confidenceScore": 45.2,
    "livePhotoPath": null,
    "verificationStatus": "Failed",
    "errorMessage": "Face verification failed - confidence score below threshold (70%)",
    "createdBy": null
  }
]
```

**Error Responses:**

- `500 Internal Server Error` - Error retrieving history
```json
{
  "error": "Error retrieving verification history"
}
```

---

### 3. Get Verification Status

Retrieves the status of a specific verification.

**Endpoint:** `GET /FaceVerification/status/{verificationId}`

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| verificationId | GUID | Yes | Verification identifier |

**Example Request:**
```bash
curl "http://localhost:5000/api/FaceVerification/status/a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

**Response (200 OK):**
```json
{
  "verificationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "documentId": "123e4567-e89b-12d3-a456-426614174000",
  "verificationTimestamp": "2025-11-22T21:30:00Z",
  "isMatch": true,
  "confidenceScore": 85.5,
  "livePhotoPath": null,
  "verificationStatus": "Success",
  "errorMessage": null,
  "createdBy": null
}
```

**Response (404 Not Found):**
```json
{
  "error": "Verification record not found"
}
```

**Error Responses:**

- `500 Internal Server Error` - Error retrieving status
```json
{
  "error": "Error retrieving verification status"
}
```

---

## Data Models

### FaceVerificationResult

```typescript
{
  isMatch: boolean;          // Whether faces match
  confidenceScore: number;   // Confidence score (0-100)
  message: string;           // Result message
  verificationTimestamp: string; // ISO 8601 timestamp
  warnings: string[];        // Array of warning messages
}
```

### FaceVerificationRecord

```typescript
{
  verificationId: string;        // GUID
  documentId: string;            // GUID
  verificationTimestamp: string; // ISO 8601 timestamp
  isMatch: boolean;              // Match result
  confidenceScore: number;       // Decimal (0-100)
  livePhotoPath: string | null;  // Path to stored photo
  verificationStatus: string;    // 'Success', 'Failed', 'Error'
  errorMessage: string | null;   // Error details if any
  createdBy: string | null;      // User identifier
}
```

## Rate Limiting

Currently, there are no rate limits implemented. For production use, consider implementing rate limiting to prevent abuse.

## Authentication

The current implementation does not require authentication. For production use, implement appropriate authentication and authorization mechanisms.

## File Upload Constraints

- **Allowed formats:** image/jpeg, image/png
- **Maximum file size:** 10 MB
- **Validation:** Files are validated on both client and server side

## Error Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 400 | Bad Request - Invalid parameters |
| 404 | Not Found - Resource doesn't exist |
| 500 | Internal Server Error |

## Security Recommendations

1. **HTTPS Only:** Always use HTTPS in production
2. **Authentication:** Implement API key or OAuth authentication
3. **Rate Limiting:** Prevent abuse with rate limiting
4. **Input Validation:** Validate all inputs server-side
5. **CORS:** Configure CORS policies appropriately
6. **Audit Logging:** Log all API requests for security monitoring

## Testing the API

### Using cURL

```bash
# Verify face
curl -X POST "http://localhost:5000/api/FaceVerification/verify" \
  -F "idPhoto=@id-photo.jpg" \
  -F "livePhoto=@live-photo.jpg"

# Get history
curl "http://localhost:5000/api/FaceVerification/history/123e4567-e89b-12d3-a456-426614174000"

# Get status
curl "http://localhost:5000/api/FaceVerification/status/a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

### Using Postman

1. Create a new POST request to `/api/FaceVerification/verify`
2. Select "Body" tab
3. Choose "form-data"
4. Add keys: `idPhoto` (file), `livePhoto` (file), `documentId` (text)
5. Upload files and send request

## Support

For issues or questions, please create an issue in the GitHub repository.
