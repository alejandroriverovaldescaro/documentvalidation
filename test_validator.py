#!/usr/bin/env python3
"""
Test script for Document Validator
Creates test files and demonstrates all three analysis options
"""

import os
import sys
from pathlib import Path

# Create a simple test text file
def create_test_file():
    """Create a simple test file for basic validation"""
    test_file = 'test_document.txt'
    with open(test_file, 'w') as f:
        f.write("This is a test document.\n")
        f.write("It contains sample text for validation.\n")
    return test_file

def main():
    print("Document Validator - Test Suite")
    print("=" * 60)
    
    # Create test file
    test_file = create_test_file()
    print(f"\n✓ Created test file: {test_file}")
    
    # Import the validator
    try:
        from document_validator import BasicValidator, OCRValidator, AzureAIVisionValidator, print_result
    except ImportError as e:
        print(f"\n❌ Error: Could not import document_validator: {e}")
        sys.exit(1)
    
    print("\n" + "=" * 60)
    print("TEST 1: Basic Validation")
    print("=" * 60)
    validator1 = BasicValidator()
    result1 = validator1.analyze(test_file)
    print_result(result1, verbose=True)
    
    print("\n" + "=" * 60)
    print("TEST 2: OCR Text Extraction")
    print("=" * 60)
    print("Note: Requires pytesseract and PIL to be installed")
    validator2 = OCRValidator()
    # OCR won't work on .txt file, but will show error handling
    result2 = validator2.analyze(test_file)
    print_result(result2, verbose=True)
    
    print("\n" + "=" * 60)
    print("TEST 3: Azure AI Vision")
    print("=" * 60)
    print("Note: Requires Azure credentials to be configured")
    validator3 = AzureAIVisionValidator()
    result3 = validator3.analyze(test_file)
    print_result(result3, verbose=True)
    
    # Clean up
    if os.path.exists(test_file):
        os.remove(test_file)
        print(f"✓ Cleaned up test file: {test_file}\n")
    
    print("=" * 60)
    print("TEST SUMMARY")
    print("=" * 60)
    print(f"Test 1 (Basic Validation): {result1['status'].upper()}")
    print(f"Test 2 (OCR): {result2['status'].upper()} (expected to show missing dependencies)")
    print(f"Test 3 (Azure AI Vision): {result3['status'].upper()} (expected to show missing credentials)")
    print("\nTo use OCR and Azure AI Vision, install dependencies:")
    print("  pip install -r requirements.txt")
    print("\nFor Azure AI Vision, also configure credentials:")
    print("  export AZURE_VISION_ENDPOINT='https://your-resource.cognitiveservices.azure.com/'")
    print("  export AZURE_VISION_KEY='your-key-here'")
    print("=" * 60)

if __name__ == '__main__':
    main()
