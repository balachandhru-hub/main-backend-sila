#!/usr/bin/env python3
"""Local document assist for SILA ME Advanced OCR.

.NET remains the business layer. This helper optionally classifies a PDF,
renders pages, and applies a single measured preprocessing profile before
Tesseract. It is skipped when tesseract/pdftoppm are unavailable.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path


def run(command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, check=False, capture_output=True, text=True)


def classify_pdf(path: Path) -> str:
    raw = path.read_bytes()[:200_000]
    textish = sum(32 <= byte < 127 or byte in (9, 10, 13) for byte in raw)
    ratio = textish / max(len(raw), 1)
    if b"/Font" in raw and ratio > 0.55:
        return "DIGITAL_TEXT"
    if b"/Image" in raw and ratio < 0.45:
        return "IMAGE_ONLY"
    return "MIXED"


def preprocess_jpeg(source: Path, dest: Path, profile: str) -> None:
    try:
        from PIL import Image, ImageFilter, ImageOps, ImageStat
    except ImportError:
        shutil.copyfile(source, dest)
        return

    image = Image.open(source).convert("RGB")
    if profile == "ORIGINAL":
        image.save(dest, "JPEG", quality=92)
        return
    gray = ImageOps.grayscale(image)
    if profile == "GRAYSCALE_CONTRAST":
        ImageOps.autocontrast(gray).save(dest, "JPEG", quality=92)
        return
    if profile == "DESKEWED_ENHANCED":
        enhanced = ImageOps.autocontrast(gray.filter(ImageFilter.UnsharpMask(radius=1.4, percent=140)))
        enhanced.save(dest, "JPEG", quality=92)
        return
    # ADAPTIVE_THRESHOLD
    stats = ImageStat.Stat(gray)
    mean = stats.mean[0]
    binary = gray.point(lambda pixel: 255 if pixel > mean else 0)
    binary.save(dest, "JPEG", quality=92)


def score_text(text: str, engine_confidence: float | None = None) -> dict[str, float | int]:
    words = [word for word in text.split() if word]
    keywords = ("invoice", "total", "supplier", "purchase", "trn", "vat", "amount")
    keyword_hits = sum(text.lower().count(word) for word in keywords)
    numeric = sum(ch.isdigit() for ch in text)
    return {
        "characters": len(text),
        "words": len(words),
        "numericDensity": round(numeric / max(len(text), 1), 4),
        "keywordHits": keyword_hits,
        "engineConfidence": engine_confidence or 0,
        "quality": round(min(1.0, (len(words) / 80) + (keyword_hits / 20) + (numeric / 400)), 4),
    }


def ocr_image(image: Path) -> str:
    if not shutil.which("tesseract"):
        return ""
    result = run(["tesseract", str(image), "stdout", "--psm", "6", "-l", "eng"])
    return result.stdout.strip() if result.returncode == 0 else ""


def process(path: Path) -> dict:
    suffix = path.suffix.lower()
    work = Path(tempfile.mkdtemp(prefix="sila-ocr-"))
    images: list[Path] = []
    document_type = "IMAGE"
    if not shutil.which("tesseract"):
        return {"ok": False, "error": "tesseract_missing", "documentType": document_type}
    try:
        if suffix == ".pdf":
            document_type = classify_pdf(path)
            if not shutil.which("pdftoppm"):
                return {"ok": False, "error": "pdftoppm_missing", "documentType": document_type}
            prefix = work / "page"
            rendered = run(["pdftoppm", "-jpeg", "-r", "300", str(path), str(prefix)])
            if rendered.returncode != 0:
                return {"ok": False, "error": "render_failed", "documentType": document_type}
            images = sorted(work.glob("page*.jpg"))
        else:
            images = [path]

        profiles = ["ORIGINAL", "GRAYSCALE_CONTRAST", "ADAPTIVE_THRESHOLD", "DESKEWED_ENHANCED"]
        best_text = ""
        best_profile = "ORIGINAL"
        best_score = -1.0
        page_count = len(images)
        for profile in profiles:
            pages: list[str] = []
            for image in images:
                prepared = work / f"{profile}-{image.name}"
                preprocess_jpeg(image, prepared, profile)
                pages.append(ocr_image(prepared))
            text = "\n\n".join(page for page in pages if page)
            quality = float(score_text(text)["quality"])
            if quality > best_score:
                best_score = quality
                best_text = text
                best_profile = profile
            if quality >= 0.55:
                break
        if not best_text.strip():
            return {
                "ok": False,
                "error": "ocr_empty",
                "documentType": document_type,
                "pageCount": page_count,
                "preprocessingProfile": best_profile,
            }
        return {
            "ok": True,
            "documentType": document_type,
            "pageCount": page_count,
            "preprocessingProfile": best_profile,
            "text": best_text,
            "metrics": score_text(best_text),
        }
    finally:
        shutil.rmtree(work, ignore_errors=True)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("path")
    args = parser.parse_args()
    path = Path(args.path)
    if not path.exists():
        json.dump({"ok": False, "error": "missing_file"}, sys.stdout)
        return 1
    json.dump(process(path), sys.stdout)
    return 0


if __name__ == "__main__":
    sys.exit(main())
