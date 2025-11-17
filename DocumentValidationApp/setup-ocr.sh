#!/bin/bash

# Tesseract Language Data Setup Script
# This script downloads the required Tesseract language data files

echo "=========================================="
echo "Tesseract OCR Setup"
echo "=========================================="
echo ""

# Define paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TESSDATA_DIR="$SCRIPT_DIR/wwwroot/tessdata"
TESSDATA_FILE="$TESSDATA_DIR/eng.traineddata"

# Create tessdata directory if it doesn't exist
if [ ! -d "$TESSDATA_DIR" ]; then
    echo "Creating tessdata directory..."
    mkdir -p "$TESSDATA_DIR"
fi

# Check if the file already exists
if [ -f "$TESSDATA_FILE" ]; then
    echo "✓ eng.traineddata already exists!"
    echo "  Location: $TESSDATA_FILE"
    echo ""
    echo "OCR is ready to use!"
    exit 0
fi

# Download the file
echo "Downloading Tesseract English language data..."
echo "Source: https://github.com/tesseract-ocr/tessdata"
echo "File size: ~23 MB"
echo ""

if command -v curl &> /dev/null; then
    echo "Using curl to download..."
    curl -L -o "$TESSDATA_FILE" https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
elif command -v wget &> /dev/null; then
    echo "Using wget to download..."
    wget -O "$TESSDATA_FILE" https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
else
    echo "❌ Error: Neither curl nor wget is installed."
    echo ""
    echo "Please install curl or wget, or manually download the file from:"
    echo "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
    echo ""
    echo "Save it to: $TESSDATA_FILE"
    exit 1
fi

# Verify download
if [ -f "$TESSDATA_FILE" ]; then
    FILE_SIZE=$(du -h "$TESSDATA_FILE" | cut -f1)
    echo ""
    echo "=========================================="
    echo "✓ Setup Complete!"
    echo "=========================================="
    echo "Downloaded: eng.traineddata ($FILE_SIZE)"
    echo "Location: $TESSDATA_FILE"
    echo ""
    echo "OCR is now ready to use!"
    echo "You can now run the application and upload images for text extraction."
else
    echo ""
    echo "❌ Download failed!"
    echo ""
    echo "Please manually download the file from:"
    echo "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
    echo ""
    echo "Save it to: $TESSDATA_FILE"
    exit 1
fi
