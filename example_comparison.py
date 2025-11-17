#!/usr/bin/env python3
"""
Example: Compare all three document analysis methods

This example demonstrates the differences between the three analysis options
by running all three on the same file and showing the results.
"""

import sys
import os

# Add current directory to path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from document_validator import BasicValidator, OCRValidator, AzureAIVisionValidator, print_result


def compare_analysis_methods(file_path):
    """Compare all three analysis methods on the same file"""
    
    if not os.path.exists(file_path):
        print(f"Error: File '{file_path}' does not exist")
        return
    
    print("\n" + "="*80)
    print(f"DOCUMENT ANALYSIS COMPARISON")
    print(f"File: {file_path}")
    print("="*80)
    
    # Option 1: Basic Validation
    print("\n" + "-"*80)
    print("OPTION 1: BASIC VALIDATION")
    print("-"*80)
    validator1 = BasicValidator()
    result1 = validator1.analyze(file_path)
    print_result(result1, verbose=False)
    
    # Option 2: OCR Text Extraction
    print("\n" + "-"*80)
    print("OPTION 2: OCR TEXT EXTRACTION")
    print("-"*80)
    validator2 = OCRValidator()
    result2 = validator2.analyze(file_path)
    print_result(result2, verbose=True)
    
    # Option 3: Azure AI Vision
    print("\n" + "-"*80)
    print("OPTION 3: AZURE AI VISION")
    print("-"*80)
    validator3 = AzureAIVisionValidator()
    result3 = validator3.analyze(file_path)
    print_result(result3, verbose=True)
    
    # Summary
    print("\n" + "="*80)
    print("SUMMARY")
    print("="*80)
    print(f"Basic Validation:    {result1['status'].upper():<15} (Always available)")
    print(f"OCR Extraction:      {result2['status'].upper():<15} (Requires pytesseract)")
    print(f"Azure AI Vision:     {result3['status'].upper():<15} (Requires Azure subscription)")
    print("="*80 + "\n")


def main():
    if len(sys.argv) < 2:
        print("Usage: python3 example_comparison.py <file_path>")
        print("\nExample:")
        print("  python3 example_comparison.py sample_document.txt")
        print("  python3 example_comparison.py image.png")
        sys.exit(1)
    
    file_path = sys.argv[1]
    compare_analysis_methods(file_path)


if __name__ == '__main__':
    main()
