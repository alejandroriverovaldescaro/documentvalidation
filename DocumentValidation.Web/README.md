# Identity Verification Web Application

A modern Blazor Server application for identity verification with a clean, user-friendly interface.

## Features

- **Modern UI/UX**: Clean, gradient-based design with smooth animations
- **Step-by-step workflow**: Guided 2-step verification process
- **Real-time verification**: Instant face matching results
- **Responsive design**: Works on desktop and mobile devices
- **Interactive components**: Progress indicators, visual feedback, and status messages

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later

### Running the Application

1. Navigate to the project directory:
   ```bash
   cd DocumentValidation.Web
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to:
   ```
   http://localhost:5000
   ```

## Application Structure

### Pages

- **Home** (`/`): Landing page with feature overview
- **Verification** (`/verification`): Main identity verification workflow

### Verification Workflow

1. **Initial Welcome**: Introduction to the 2-step process
2. **Capture Selfie**: Upload or capture a selfie photo
3. **Upload ID Photo**: Upload a government-issued ID photo
4. **Results**: Display verification results with confidence scores

### Configuration

The application uses the Face Matching library with simulated verification by default. To use Azure Face API in production:

Edit `Program.cs`:

```csharp
builder.Services.AddFaceMatching(options =>
{
    options.VerificationMethod = VerificationMethod.AzureFaceAPI;
    options.FaceApiEndpoint = "https://your-endpoint.cognitiveservices.azure.com/";
    options.FaceApiKey = "your-api-key";
});
```

## UI Components

### Color Scheme

- Primary gradient: Purple to blue (`#667eea` to `#764ba2`)
- Success: Green tones
- Warning: Yellow/amber tones
- Error: Red tones

### Key Features

- **Animated cards**: Smooth hover effects and transitions
- **Progress indicators**: Visual feedback on verification steps
- **Status badges**: Clear success/warning/error states
- **Responsive layout**: Adapts to different screen sizes

## Development

### Technologies Used

- **Blazor Server**: For interactive web UI with .NET
- **DocumentValidation.FaceMatching**: Core face matching library
- **Bootstrap Icons**: For iconography
- **CSS3**: For modern styling and animations

### Project Structure

```
DocumentValidation.Web/
├── Components/
│   ├── Layout/            # Layout components (nav, main layout)
│   └── Pages/             # Page components
│       ├── Home.razor     # Landing page
│       ├── Verification.razor           # Main verification page
│       └── Verification.razor.css       # Verification page styles
├── wwwroot/               # Static files
├── Program.cs             # Application entry point
└── appsettings.json       # Configuration
```

## Customization

### Styling

Page-specific styles are defined in `.razor.css` files next to their corresponding `.razor` files. Global styles can be added to `wwwroot/css/app.css`.

### Verification Settings

Adjust verification parameters in `Program.cs`:

```csharp
builder.Services.AddFaceMatching(options =>
{
    options.BurstFrameCount = 5;    // Number of frames to capture
    options.FrameDelayMs = 200;      // Delay between frames
});
```

## Security Considerations

- Images are processed in memory and not permanently stored
- Use HTTPS in production
- Configure CORS appropriately
- Add rate limiting for verification requests
- Enable HSTS and other security headers

## License

MIT License - see the root repository LICENSE file for details.
