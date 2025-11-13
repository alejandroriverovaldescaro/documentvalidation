# Tesseract Language Data

This directory contains Tesseract OCR language data files required for text extraction from images.

## Required Files

- **eng.traineddata** - English language data file (required for OCR to work)

## Download Instructions

To enable OCR functionality, download the English language data file:

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
- The tessdata files are excluded from git via `.gitignore` due to their large size
- Users must download them separately after cloning the repository
