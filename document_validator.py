#!/usr/bin/env python3
"""
Document Validation Tool
Supports three analysis methods:
1. Basic Validation - File type, size, and format checks
2. OCR Text Extraction - Extract text using Tesseract OCR
3. Azure AI Vision - Advanced document analysis using Azure AI Vision
"""

import os
import sys
import argparse
import mimetypes
from pathlib import Path
from typing import Dict, Any, Optional

# Option 1: Basic Validation
class BasicValidator:
    """Basic file validation - checks file type, size, and readability"""
    
    SUPPORTED_FORMATS = {
        'image/png', 'image/jpeg', 'image/jpg', 'image/gif', 'image/bmp',
        'image/tiff', 'application/pdf', 'image/webp'
    }
    
    MAX_FILE_SIZE = 50 * 1024 * 1024  # 50 MB
    
    def __init__(self):
        self.name = "Basic Validation"
    
    def analyze(self, file_path: str) -> Dict[str, Any]:
        """Perform basic validation on the document"""
        result = {
            'method': self.name,
            'file_path': file_path,
            'status': 'unknown',
            'checks': {}
        }
        
        # Check if file exists
        if not os.path.exists(file_path):
            result['status'] = 'failed'
            result['error'] = 'File does not exist'
            return result
        
        # Check if it's a file
        if not os.path.isfile(file_path):
            result['status'] = 'failed'
            result['error'] = 'Path is not a file'
            return result
        
        # Get file size
        file_size = os.path.getsize(file_path)
        result['checks']['file_size'] = file_size
        result['checks']['file_size_readable'] = self._format_size(file_size)
        
        # Check file size
        if file_size > self.MAX_FILE_SIZE:
            result['status'] = 'failed'
            result['error'] = f'File size exceeds maximum allowed size ({self._format_size(self.MAX_FILE_SIZE)})'
            return result
        
        if file_size == 0:
            result['status'] = 'failed'
            result['error'] = 'File is empty'
            return result
        
        # Check MIME type
        mime_type, _ = mimetypes.guess_type(file_path)
        result['checks']['mime_type'] = mime_type
        
        if mime_type in self.SUPPORTED_FORMATS:
            result['checks']['format_supported'] = True
        else:
            result['checks']['format_supported'] = False
            result['status'] = 'warning'
            result['warning'] = f'File type {mime_type} may not be fully supported'
        
        # Check if file is readable
        try:
            with open(file_path, 'rb') as f:
                f.read(1)
            result['checks']['readable'] = True
        except Exception as e:
            result['status'] = 'failed'
            result['error'] = f'File is not readable: {str(e)}'
            return result
        
        # Get file extension
        result['checks']['extension'] = Path(file_path).suffix
        
        if result['status'] == 'unknown':
            result['status'] = 'passed'
        
        return result
    
    @staticmethod
    def _format_size(size_bytes: int) -> str:
        """Format bytes to human readable format"""
        for unit in ['B', 'KB', 'MB', 'GB']:
            if size_bytes < 1024.0:
                return f"{size_bytes:.2f} {unit}"
            size_bytes /= 1024.0
        return f"{size_bytes:.2f} TB"


# Option 2: OCR Text Extraction
class OCRValidator:
    """OCR-based text extraction using Tesseract"""
    
    def __init__(self):
        self.name = "OCR Text Extraction"
        self._check_dependencies()
    
    def _check_dependencies(self):
        """Check if required dependencies are available"""
        try:
            import pytesseract
            from PIL import Image
            self.pytesseract = pytesseract
            self.Image = Image
            self.available = True
        except ImportError as e:
            self.available = False
            self.import_error = str(e)
    
    def analyze(self, file_path: str) -> Dict[str, Any]:
        """Extract text from document using OCR"""
        result = {
            'method': self.name,
            'file_path': file_path,
            'status': 'unknown'
        }
        
        if not self.available:
            result['status'] = 'failed'
            result['error'] = f'OCR dependencies not available: {self.import_error}'
            result['note'] = 'Install with: pip install pytesseract pillow'
            return result
        
        # Check if file exists
        if not os.path.exists(file_path):
            result['status'] = 'failed'
            result['error'] = 'File does not exist'
            return result
        
        try:
            # Open image
            image = self.Image.open(file_path)
            
            # Extract text
            text = self.pytesseract.image_to_string(image)
            
            # Get additional data
            data = self.pytesseract.image_to_data(image, output_type=self.pytesseract.Output.DICT)
            
            result['text'] = text
            result['text_length'] = len(text.strip())
            result['word_count'] = len([w for w in text.split() if w.strip()])
            result['confidence'] = sum(data['conf']) / len([c for c in data['conf'] if c != -1]) if data['conf'] else 0
            result['status'] = 'success'
            
        except Exception as e:
            result['status'] = 'failed'
            result['error'] = f'OCR processing failed: {str(e)}'
        
        return result


# Option 3: Azure AI Vision
class AzureAIVisionValidator:
    """Advanced document analysis using Azure AI Vision"""
    
    def __init__(self, endpoint: Optional[str] = None, key: Optional[str] = None):
        self.name = "Azure AI Vision"
        self.endpoint = endpoint or os.environ.get('AZURE_VISION_ENDPOINT')
        self.key = key or os.environ.get('AZURE_VISION_KEY')
        self._check_dependencies()
    
    def _check_dependencies(self):
        """Check if Azure AI Vision SDK is available"""
        try:
            from azure.ai.vision.imageanalysis import ImageAnalysisClient
            from azure.ai.vision.imageanalysis.models import VisualFeatures
            from azure.core.credentials import AzureKeyCredential
            
            self.ImageAnalysisClient = ImageAnalysisClient
            self.VisualFeatures = VisualFeatures
            self.AzureKeyCredential = AzureKeyCredential
            self.available = True
        except ImportError as e:
            self.available = False
            self.import_error = str(e)
    
    def analyze(self, file_path: str) -> Dict[str, Any]:
        """Analyze document using Azure AI Vision"""
        result = {
            'method': self.name,
            'file_path': file_path,
            'status': 'unknown'
        }
        
        if not self.available:
            result['status'] = 'failed'
            result['error'] = f'Azure AI Vision SDK not available: {self.import_error}'
            result['note'] = 'Install with: pip install azure-ai-vision-imageanalysis'
            return result
        
        if not self.endpoint or not self.key:
            result['status'] = 'failed'
            result['error'] = 'Azure credentials not configured'
            result['note'] = 'Set AZURE_VISION_ENDPOINT and AZURE_VISION_KEY environment variables'
            return result
        
        # Check if file exists
        if not os.path.exists(file_path):
            result['status'] = 'failed'
            result['error'] = 'File does not exist'
            return result
        
        try:
            # Create client
            client = self.ImageAnalysisClient(
                endpoint=self.endpoint,
                credential=self.AzureKeyCredential(self.key)
            )
            
            # Read image data
            with open(file_path, 'rb') as f:
                image_data = f.read()
            
            # Analyze image with multiple features
            analysis_result = client.analyze(
                image_data=image_data,
                visual_features=[
                    self.VisualFeatures.CAPTION,
                    self.VisualFeatures.READ,
                    self.VisualFeatures.TAGS,
                    self.VisualFeatures.OBJECTS,
                    self.VisualFeatures.PEOPLE,
                ]
            )
            
            # Extract results
            result['analysis'] = {}
            
            # Caption
            if analysis_result.caption:
                result['analysis']['caption'] = {
                    'text': analysis_result.caption.text,
                    'confidence': analysis_result.caption.confidence
                }
            
            # Text (OCR)
            if analysis_result.read:
                blocks = []
                for block in analysis_result.read.blocks:
                    block_data = {
                        'lines': [line.text for line in block.lines]
                    }
                    blocks.append(block_data)
                result['analysis']['text'] = {
                    'blocks': blocks,
                    'full_text': ' '.join([line for block in blocks for line in block['lines']])
                }
            
            # Tags
            if analysis_result.tags:
                result['analysis']['tags'] = [
                    {'name': tag.name, 'confidence': tag.confidence}
                    for tag in analysis_result.tags.list
                ]
            
            # Objects
            if analysis_result.objects:
                result['analysis']['objects'] = [
                    {'name': obj.tags[0].name if obj.tags else 'unknown', 
                     'confidence': obj.tags[0].confidence if obj.tags else 0}
                    for obj in analysis_result.objects.list
                ]
            
            # People
            if analysis_result.people:
                result['analysis']['people_count'] = len(analysis_result.people.list)
            
            result['status'] = 'success'
            
        except Exception as e:
            result['status'] = 'failed'
            result['error'] = f'Azure AI Vision analysis failed: {str(e)}'
        
        return result


def print_result(result: Dict[str, Any], verbose: bool = False):
    """Print analysis result in a readable format"""
    print(f"\n{'='*60}")
    print(f"Analysis Method: {result.get('method', 'Unknown')}")
    print(f"File: {result.get('file_path', 'Unknown')}")
    print(f"Status: {result.get('status', 'Unknown').upper()}")
    print(f"{'='*60}")
    
    if result.get('error'):
        print(f"\n❌ Error: {result['error']}")
        if result.get('note'):
            print(f"ℹ️  Note: {result['note']}")
    
    if result.get('warning'):
        print(f"\n⚠️  Warning: {result['warning']}")
    
    # Basic Validation results
    if result.get('checks'):
        print("\n📋 Validation Checks:")
        for key, value in result['checks'].items():
            print(f"  • {key.replace('_', ' ').title()}: {value}")
    
    # OCR results
    if result.get('text_length') is not None:
        print(f"\n📝 OCR Results:")
        print(f"  • Text Length: {result['text_length']} characters")
        print(f"  • Word Count: {result.get('word_count', 0)} words")
        print(f"  • Confidence: {result.get('confidence', 0):.2f}%")
        
        if verbose and result.get('text'):
            print(f"\n  Extracted Text (first 500 chars):")
            print(f"  {result['text'][:500]}")
    
    # Azure AI Vision results
    if result.get('analysis'):
        print(f"\n🔍 Azure AI Vision Analysis:")
        analysis = result['analysis']
        
        if analysis.get('caption'):
            print(f"  • Caption: {analysis['caption']['text']}")
            print(f"    Confidence: {analysis['caption']['confidence']:.2f}")
        
        if analysis.get('text'):
            text_data = analysis['text']
            print(f"  • Detected Text: {len(text_data.get('full_text', ''))} characters")
            if verbose and text_data.get('full_text'):
                print(f"    {text_data['full_text'][:200]}...")
        
        if analysis.get('tags'):
            tags = analysis['tags'][:5]  # Show top 5
            print(f"  • Top Tags: {', '.join([t['name'] for t in tags])}")
        
        if analysis.get('objects'):
            print(f"  • Objects Detected: {len(analysis['objects'])}")
            if verbose:
                for obj in analysis['objects'][:5]:
                    print(f"    - {obj['name']} (confidence: {obj['confidence']:.2f})")
        
        if analysis.get('people_count') is not None:
            print(f"  • People Detected: {analysis['people_count']}")
    
    print(f"\n{'='*60}\n")


def main():
    parser = argparse.ArgumentParser(
        description='Document Validation Tool with three analysis options',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Analysis Options:
  1. basic    - Basic file validation (type, size, format)
  2. ocr      - OCR text extraction using Tesseract
  3. azure    - Advanced analysis using Azure AI Vision

Examples:
  %(prog)s -m basic document.pdf
  %(prog)s -m ocr image.png
  %(prog)s -m azure photo.jpg -v
  %(prog)s -m azure document.png --endpoint https://xxx.cognitiveservices.azure.com/ --key YOUR_KEY
        """
    )
    
    parser.add_argument('file', help='Path to document file to analyze')
    parser.add_argument(
        '-m', '--method',
        choices=['basic', 'ocr', 'azure'],
        default='basic',
        help='Analysis method to use (default: basic)'
    )
    parser.add_argument(
        '-v', '--verbose',
        action='store_true',
        help='Show verbose output including extracted text'
    )
    parser.add_argument(
        '--endpoint',
        help='Azure AI Vision endpoint (or set AZURE_VISION_ENDPOINT env var)'
    )
    parser.add_argument(
        '--key',
        help='Azure AI Vision key (or set AZURE_VISION_KEY env var)'
    )
    
    args = parser.parse_args()
    
    # Initialize validator based on method
    if args.method == 'basic':
        validator = BasicValidator()
    elif args.method == 'ocr':
        validator = OCRValidator()
    elif args.method == 'azure':
        validator = AzureAIVisionValidator(endpoint=args.endpoint, key=args.key)
    else:
        print(f"Error: Unknown method '{args.method}'")
        sys.exit(1)
    
    # Perform analysis
    result = validator.analyze(args.file)
    
    # Print result
    print_result(result, verbose=args.verbose)
    
    # Exit with appropriate code
    if result['status'] == 'failed':
        sys.exit(1)
    elif result['status'] == 'warning':
        sys.exit(2)
    else:
        sys.exit(0)


if __name__ == '__main__':
    main()
