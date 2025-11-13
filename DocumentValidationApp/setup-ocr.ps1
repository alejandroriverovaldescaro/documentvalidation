# Tesseract Language Data Setup Script for Windows
# This script downloads the required Tesseract language data files

Write-Host "=========================================="
Write-Host "Tesseract OCR Setup" -ForegroundColor Cyan
Write-Host "=========================================="
Write-Host ""

# Define paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TessdataDir = Join-Path $ScriptDir "wwwroot\tessdata"
$TessdataFile = Join-Path $TessdataDir "eng.traineddata"

# Create tessdata directory if it doesn't exist
if (-not (Test-Path $TessdataDir)) {
    Write-Host "Creating tessdata directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $TessdataDir -Force | Out-Null
}

# Check if the file already exists
if (Test-Path $TessdataFile) {
    Write-Host "✓ eng.traineddata already exists!" -ForegroundColor Green
    Write-Host "  Location: $TessdataFile"
    Write-Host ""
    Write-Host "OCR is ready to use!" -ForegroundColor Green
    exit 0
}

# Download the file
Write-Host "Downloading Tesseract English language data..." -ForegroundColor Yellow
Write-Host "Source: https://github.com/tesseract-ocr/tessdata"
Write-Host "File size: ~23 MB"
Write-Host ""

try {
    $url = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
    
    # Use TLS 1.2
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    
    Write-Host "Downloading..." -ForegroundColor Yellow
    
    # Download with progress
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile($url, $TessdataFile)
    
    # Verify download
    if (Test-Path $TessdataFile) {
        $fileSize = (Get-Item $TessdataFile).Length / 1MB
        Write-Host ""
        Write-Host "=========================================="
        Write-Host "✓ Setup Complete!" -ForegroundColor Green
        Write-Host "=========================================="
        Write-Host "Downloaded: eng.traineddata ($([math]::Round($fileSize, 2)) MB)"
        Write-Host "Location: $TessdataFile"
        Write-Host ""
        Write-Host "OCR is now ready to use!" -ForegroundColor Green
        Write-Host "You can now run the application and upload images for text extraction."
    }
    else {
        throw "Download verification failed"
    }
}
catch {
    Write-Host ""
    Write-Host "❌ Download failed!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please manually download the file from:"
    Write-Host "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
    Write-Host ""
    Write-Host "Save it to: $TessdataFile"
    exit 1
}
