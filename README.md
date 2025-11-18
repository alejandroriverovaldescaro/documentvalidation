# Document Validation

A Blazor Server application for validating and extracting information from identity documents.

## Features

- **Document Upload**: Support for PDF and image files (PNG, JPG, JPEG)
- **OCR Text Extraction**: Uses Tesseract OCR to extract text from images (passports, IDs, licenses)
- **Document Type Detection**: Automatically identifies:
  - Passports
  - Driver's Licenses
  - Identity Cards
  - Other document types
- **Data Extraction**: Extracts critical information including:
  - Expiration dates
  - Document numbers
  - Issuing authorities
- **Smart Validation**: Checks for expired documents and validates extracted data
- **Free & Open Source**: Uses entirely free libraries and technologies

## Technologies Used

- **Blazor Server** (.NET 9.0) - Web framework
- **iText7** - PDF text extraction
- **Tesseract OCR** - Image text extraction (free, open-source OCR engine)
- **SixLabors.ImageSharp** - Image processing and validation
- **Bootstrap** - UI styling
- **Bootstrap Icons** - Icons

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/alejandroriverovaldescaro/documentvalidation.git
   cd documentvalidation
   ```

2. Navigate to the application folder:
   ```bash
   cd DocumentValidationApp
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Build the application:
   ```bash
   dotnet build
   ```

5. **Setup Tesseract OCR** (required for image text extraction):
   
   **Option 1: Automated Setup (Recommended)**
   ```bash
   # Linux/Mac
   ./setup-ocr.sh
   
   # Windows (PowerShell)
   .\setup-ocr.ps1
   ```
   
   **Option 2: Manual Setup**
   ```bash
   cd wwwroot
   mkdir -p tessdata
   cd tessdata
   curl -L -o eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
   cd ../..
   ```
   
   **Note:** The `eng.traineddata` file (~23MB) is required for OCR to extract text from images. Without it, the app will display a helpful message with download instructions.

### Running the Application

1. Run the application:
   ```bash
   dotnet run
   ```

2. Open your browser and navigate to:
   - HTTPS: `https://localhost:5001`
   - HTTP: `http://localhost:5000`

3. Click on **Document Validation** in the navigation menu

4. Upload a document (PDF or image) and view the validation results

## Usage

1. **Upload a Document**: Click "Select Document" and choose a PDF or image file (max 10MB)
2. **View Results**: The system will:
   - Identify the document type
   - Extract text content
   - Find expiration dates
   - Locate document numbers
   - Detect issuing authorities
3. **Review Validation**: Check the validation messages for any warnings or issues

## Supported Document Types

- **Passports**: International travel documents
- **Driver's Licenses**: State-issued driving permits
- **Identity Cards**: National ID cards
- **Other Documents**: General document validation

## Project Structure

```
DocumentValidationApp/
├── Components/
│   ├── Layout/          # Layout components
│   └── Pages/           # Razor pages
│       └── DocumentValidation.razor
├── Models/              # Data models
│   ├── DocumentValidationResult.cs
│   └── DocumentType.cs
├── Services/            # Business logic
│   └── DocumentValidationService.cs
├── wwwroot/             # Static files
├── Program.cs           # Application entry point
└── DocumentValidationApp.csproj
```

## Privacy & Security

- Documents are processed in memory and are **not stored** on the server
- All processing happens server-side
- No data is sent to external services
- Files are validated for type and size before processing

## Limitations

- Maximum file size: 10MB
- Image OCR requires integration with external services (see future enhancements)
- Date format detection may vary by region
- Works best with clear, high-quality scans

## Future Enhancements

For advanced features, consider integrating:
- Azure Computer Vision API for OCR on images
- Google Cloud Vision API for enhanced text extraction
- Machine learning models for improved document classification
- Barcode/QR code scanning for MRZ (Machine Readable Zone) data

## License

This project is open source and available under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues or questions, please open an issue on GitHub.
