# Document Validation Tool

A Python tool for validating and analyzing documents with three different analysis methods:

1. **Basic Validation** - File type, size, and format checks
2. **OCR Text Extraction** - Extract text using Tesseract OCR
3. **Azure AI Vision** - Advanced document analysis using Azure AI Vision

## Installation

### Basic Installation

```bash
pip install -r requirements.txt
```

### For OCR Support (Option 2)

You'll also need to install Tesseract OCR on your system:

**Ubuntu/Debian:**
```bash
sudo apt-get install tesseract-ocr
```

**macOS:**
```bash
brew install tesseract
```

**Windows:**
Download and install from: https://github.com/UB-Mannheim/tesseract/wiki

### For Azure AI Vision Support (Option 3)

You'll need an Azure account and AI Vision resource:

1. Create an Azure AI Vision resource in the Azure Portal
2. Get your endpoint and key
3. Set environment variables:
```bash
export AZURE_VISION_ENDPOINT="https://your-resource.cognitiveservices.azure.com/"
export AZURE_VISION_KEY="your-key-here"
```

Or use command-line arguments (see Usage below).

## Usage

### Option 1: Basic Validation

Validates file existence, type, size, and format:

```bash
python document_validator.py -m basic document.pdf
python document_validator.py -m basic image.png
```

### Option 2: OCR Text Extraction

Extracts text from images using Tesseract OCR:

```bash
python document_validator.py -m ocr document.png
python document_validator.py -m ocr scanned_page.jpg -v  # verbose output with extracted text
```

### Option 3: Azure AI Vision

Advanced document analysis using Azure AI Vision:

```bash
# Using environment variables
python document_validator.py -m azure photo.jpg

# Using command-line arguments
python document_validator.py -m azure document.png \
  --endpoint https://your-resource.cognitiveservices.azure.com/ \
  --key your-key-here

# Verbose output
python document_validator.py -m azure image.jpg -v
```

## Features by Analysis Method

### Basic Validation
- File existence check
- File size validation
- MIME type detection
- Format support verification
- File readability check

### OCR Text Extraction
- Text extraction from images
- Word count
- Confidence scores
- Support for multiple image formats (PNG, JPG, TIFF, etc.)

### Azure AI Vision
- Image captioning with confidence scores
- OCR text detection and extraction
- Tag detection (objects, concepts)
- Object detection with bounding boxes
- People detection
- Multiple visual features in a single analysis

## Examples

### Validating a PDF file
```bash
python document_validator.py -m basic report.pdf
```

### Extracting text from a scanned document
```bash
python document_validator.py -m ocr scanned_invoice.png -v
```

### Comprehensive analysis with Azure AI Vision
```bash
python document_validator.py -m azure business_card.jpg -v
```

## Exit Codes

- `0` - Analysis completed successfully
- `1` - Analysis failed (error occurred)
- `2` - Analysis completed with warnings

## Configuration

### Environment Variables

For Azure AI Vision (Option 3):
- `AZURE_VISION_ENDPOINT` - Your Azure AI Vision endpoint URL
- `AZURE_VISION_KEY` - Your Azure AI Vision subscription key

### Supported File Formats

- Images: PNG, JPEG, JPG, GIF, BMP, TIFF, WEBP
- Documents: PDF (basic validation only)

## Requirements

- Python 3.7+
- See `requirements.txt` for Python package dependencies
- Tesseract OCR (for Option 2)
- Azure subscription (for Option 3)

## Error Handling

The tool provides clear error messages and suggestions:

- Missing dependencies show installation instructions
- Missing Azure credentials show configuration guidance
- File validation errors indicate the specific issue
- All errors include helpful notes for resolution

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.