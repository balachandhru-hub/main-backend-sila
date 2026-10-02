from ocr_preprocess import score_text, process
from pathlib import Path


def test_quality_rewards_invoice_keywords():
    weak = score_text("hello world")
    strong = score_text("Invoice Supplier TRN Purchase Order Grand Total 997.50 AED")
    assert strong["quality"] > weak["quality"]
    assert strong["keywordHits"] >= 4


def test_process_reports_missing_tesseract(monkeypatch, tmp_path):
    monkeypatch.setattr("ocr_preprocess.shutil.which", lambda name: None)
    result = process(tmp_path / "scan.png")
    assert result["ok"] is False
    assert result["error"] == "tesseract_missing"


def test_quality_rewards_invoice_keywords():
    weak = score_text("hello world")
    strong = score_text("Invoice Supplier TRN Purchase Order Grand Total 997.50 AED")
    assert strong["quality"] > weak["quality"]
    assert strong["keywordHits"] >= 4
