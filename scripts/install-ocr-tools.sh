#!/usr/bin/env bash
# Install Tesseract and Poppler so backend Advanced OCR can read scanned invoices.
set -euo pipefail

if command -v tesseract >/dev/null 2>&1 && command -v pdftoppm >/dev/null 2>&1; then
  echo "OCR tools already available: tesseract $(tesseract --version 2>&1 | head -n 1) and pdftoppm."
  exit 0
fi

if command -v brew >/dev/null 2>&1; then
  brew install tesseract poppler
elif command -v apt-get >/dev/null 2>&1; then
  sudo apt-get update
  sudo apt-get install -y tesseract-ocr tesseract-ocr-eng poppler-utils python3-pil
else
  echo "Install tesseract and poppler (pdftoppm) with your package manager, then restart the API."
  exit 1
fi

command -v tesseract >/dev/null
command -v pdftoppm >/dev/null
echo "OCR tools installed. Restart the SILA ME API so Advanced OCR can use them."
