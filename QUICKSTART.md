# Quick Start Guide

This guide will help you get started with the Document Validation Tool.

## Installation

1. Clone the repository:
```bash
git clone https://github.com/alejandroriverovaldescaro/documentvalidation.git
cd documentvalidation
```

2. Install Python dependencies (optional, only if you want to use OCR or Azure AI Vision):
```bash
pip install -r requirements.txt
```

## Basic Usage (No Installation Required)

The Basic Validation option works without any external dependencies:

```bash
python3 document_validator.py -m basic yourfile.pdf
python3 document_validator.py -m basic image.png
```

This will check:
- File exists
- File size is reasonable
- File type is supported
- File is readable

## Using OCR (Option 2)

To use OCR text extraction:

1. Install system dependencies:
   - Ubuntu/Debian: `sudo apt-get install tesseract-ocr`
   - macOS: `brew install tesseract`
   - Windows: Download from https://github.com/UB-Mannheim/tesseract/wiki

2. Install Python packages:
```bash
pip install pytesseract pillow
```

3. Run OCR analysis:
```bash
python3 document_validator.py -m ocr scanned_document.png -v
```

## Using Azure AI Vision (Option 3)

To use Azure AI Vision for advanced analysis:

1. Create an Azure account and AI Vision resource at https://portal.azure.com

2. Get your credentials from Azure Portal (Keys and Endpoint section)

3. Configure credentials:
```bash
export AZURE_VISION_ENDPOINT="https://your-resource.cognitiveservices.azure.com/"
export AZURE_VISION_KEY="your-key-here"
```

   Or copy `.env.example` to `.env` and fill in your credentials

4. Install the Azure SDK:
```bash
pip install azure-ai-vision-imageanalysis
```

5. Run Azure AI Vision analysis:
```bash
python3 document_validator.py -m azure image.jpg -v
```

This provides:
- Image captioning
- OCR text extraction
- Tag detection
- Object detection
- People detection

## Examples

### Validate a document without installing anything
```bash
python3 document_validator.py -m basic contract.pdf
```

### Extract text from a scanned image
```bash
python3 document_validator.py -m ocr receipt.jpg -v
```

### Comprehensive image analysis
```bash
python3 document_validator.py -m azure photo.jpg -v
```

### Use Azure with command-line credentials (no environment variables)
```bash
python3 document_validator.py -m azure image.png \
  --endpoint https://myresource.cognitiveservices.azure.com/ \
  --key mykey123
```

## Testing

Run the test suite:
```bash
python3 test_validator.py
```

Run unit tests:
```bash
python3 -m unittest test_document_validator.py -v
```

## Getting Help

View all options:
```bash
python3 document_validator.py --help
```

## Troubleshooting

### "No module named 'pytesseract'"
Install with: `pip install pytesseract pillow`

### "No module named 'azure'"
Install with: `pip install azure-ai-vision-imageanalysis`

### "Azure credentials not configured"
Set environment variables or use `--endpoint` and `--key` flags

### "tesseract is not installed"
Install Tesseract OCR on your system (see OCR section above)

## Next Steps

- Try all three analysis methods on your documents
- Integrate into your workflow or scripts
- Extend with additional validation rules
- Add support for more file formats
