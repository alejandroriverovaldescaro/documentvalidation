# Tesseract Language Data

This directory contains Tesseract OCR language data files required for text extraction from images.

## Quick Setup

The easiest way to set up OCR is to run the setup script from the `DocumentValidationApp` directory:

**Linux/Mac:**
```bash
./setup-ocr.sh
```

**Windows (PowerShell):**
```powershell
.\setup-ocr.ps1
```

These scripts will automatically download the required `eng.traineddata` file.

## Manual Setup

If you prefer to download manually:

### Required Files

- **eng.traineddata** - English language data file (required for OCR to work)

### Download Instructions

```bash
curl -L -o eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
```

Or download manually from: https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata

## File Size

- eng.traineddata: ~23 MB

## Additional Languages (Optional)

If you need to support other languages, download the corresponding .traineddata files from:
https://github.com/tesseract-ocr/tessdata

For example:
- Spanish: `spa.traineddata`
- French: `fra.traineddata`
- German: `deu.traineddata`

## Notes

- Without these files, the application will still work but won't be able to extract text from images
- The app will display a helpful message with download instructions if the file is missing
- The tessdata files are excluded from git via `.gitignore` due to their large size
- Users must download them separately after cloning the repository
