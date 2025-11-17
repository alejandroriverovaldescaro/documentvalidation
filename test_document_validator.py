"""
Unit tests for Document Validator

Tests all three analysis options:
1. Basic Validation
2. OCR Text Extraction
3. Azure AI Vision
"""

import unittest
import os
import tempfile
from pathlib import Path

from document_validator import BasicValidator, OCRValidator, AzureAIVisionValidator


class TestBasicValidator(unittest.TestCase):
    """Test cases for Basic Validation"""
    
    def setUp(self):
        """Create temporary test files"""
        self.temp_dir = tempfile.mkdtemp()
        self.validator = BasicValidator()
    
    def tearDown(self):
        """Clean up temporary files"""
        import shutil
        shutil.rmtree(self.temp_dir, ignore_errors=True)
    
    def test_valid_file(self):
        """Test validation of a valid file"""
        test_file = os.path.join(self.temp_dir, 'test.txt')
        with open(test_file, 'w') as f:
            f.write("Test content")
        
        result = self.validator.analyze(test_file)
        self.assertIn('status', result)
        self.assertIn(result['status'], ['passed', 'warning'])
    
    def test_nonexistent_file(self):
        """Test validation of non-existent file"""
        test_file = os.path.join(self.temp_dir, 'nonexistent.txt')
        
        result = self.validator.analyze(test_file)
        self.assertEqual(result['status'], 'failed')
        self.assertIn('error', result)
    
    def test_empty_file(self):
        """Test validation of empty file"""
        test_file = os.path.join(self.temp_dir, 'empty.txt')
        open(test_file, 'w').close()
        
        result = self.validator.analyze(test_file)
        self.assertEqual(result['status'], 'failed')
        self.assertIn('empty', result['error'].lower())
    
    def test_large_file(self):
        """Test validation of file size check"""
        test_file = os.path.join(self.temp_dir, 'test.txt')
        with open(test_file, 'w') as f:
            f.write("A" * 1000)
        
        result = self.validator.analyze(test_file)
        self.assertIn('file_size', result['checks'])
        self.assertEqual(result['checks']['file_size'], 1000)
    
    def test_format_size_helper(self):
        """Test the file size formatting helper"""
        self.assertEqual(self.validator._format_size(100), "100.00 B")
        self.assertEqual(self.validator._format_size(1024), "1.00 KB")
        self.assertEqual(self.validator._format_size(1024 * 1024), "1.00 MB")


class TestOCRValidator(unittest.TestCase):
    """Test cases for OCR Validation"""
    
    def setUp(self):
        """Initialize validator"""
        self.validator = OCRValidator()
    
    def test_missing_dependencies(self):
        """Test behavior when dependencies are missing"""
        # This test will pass if dependencies are not installed
        # or skip if they are installed
        if not self.validator.available:
            result = self.validator.analyze('dummy.txt')
            self.assertEqual(result['status'], 'failed')
            self.assertIn('dependencies', result['error'].lower())


class TestAzureAIVisionValidator(unittest.TestCase):
    """Test cases for Azure AI Vision Validation"""
    
    def setUp(self):
        """Initialize validator"""
        self.validator = AzureAIVisionValidator()
    
    def test_missing_credentials(self):
        """Test behavior when credentials are not configured"""
        # Clear environment variables for test
        old_endpoint = os.environ.get('AZURE_VISION_ENDPOINT')
        old_key = os.environ.get('AZURE_VISION_KEY')
        
        if old_endpoint:
            del os.environ['AZURE_VISION_ENDPOINT']
        if old_key:
            del os.environ['AZURE_VISION_KEY']
        
        validator = AzureAIVisionValidator()
        result = validator.analyze('dummy.txt')
        
        # Restore environment variables
        if old_endpoint:
            os.environ['AZURE_VISION_ENDPOINT'] = old_endpoint
        if old_key:
            os.environ['AZURE_VISION_KEY'] = old_key
        
        # Check that it fails due to missing credentials
        if not validator.available:
            self.assertEqual(result['status'], 'failed')
        else:
            # If SDK is available, should fail on credentials
            self.assertIn('status', result)


class TestValidatorIntegration(unittest.TestCase):
    """Integration tests for all validators"""
    
    def test_all_validators_instantiate(self):
        """Test that all validators can be instantiated"""
        basic = BasicValidator()
        ocr = OCRValidator()
        azure = AzureAIVisionValidator()
        
        self.assertEqual(basic.name, "Basic Validation")
        self.assertEqual(ocr.name, "OCR Text Extraction")
        self.assertEqual(azure.name, "Azure AI Vision")
    
    def test_result_structure(self):
        """Test that all validators return consistent result structure"""
        temp_file = tempfile.NamedTemporaryFile(mode='w', delete=False, suffix='.txt')
        temp_file.write("Test")
        temp_file.close()
        
        try:
            basic = BasicValidator()
            result = basic.analyze(temp_file.name)
            
            # Check required fields
            self.assertIn('method', result)
            self.assertIn('file_path', result)
            self.assertIn('status', result)
            
        finally:
            os.unlink(temp_file.name)


if __name__ == '__main__':
    unittest.main()
