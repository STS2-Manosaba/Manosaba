"""把 output/data.json 嵌進 report_template.html，產生可直接分享的單檔靜態報告。

用法：python tools/card_score/build_report.py（需先執行 card_score.py）
輸出：tools/card_score/output/Manosaba_card_strength.html
"""
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
data = json.loads((HERE / "output" / "data.json").read_text(encoding="utf-8"))
up = HERE / "output" / "uncertainty.json"   # uncertainty.py 的蒙地卡羅結果（可選）
if up.exists():
    data["uncertainty"] = json.loads(up.read_text(encoding="utf-8"))
payload = json.dumps(data, ensure_ascii=False, separators=(",", ":")).replace("</", "<\\/")
tpl = (HERE / "report_template.html").read_text(encoding="utf-8")
fp = HERE / "findings.html"   # 人工撰寫的重點結論（HTML 片段）
tpl = tpl.replace("__FINDINGS__", fp.read_text(encoding="utf-8") if fp.exists() else "<p class=\"note\">（尚未撰寫）</p>")
# 範本省略了 <html>/<head>，單檔分享時補上完整文件骨架
head, sep, rest = tpl.partition("<style>")
html = ("<!doctype html>\n<html lang=\"zh-Hant\">\n<head>\n<meta charset=\"utf-8\">\n"
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\">\n"
        + head + sep + rest.replace("</style>", "</style>\n</head>\n<body>", 1)
        .replace("__REPORT_DATA__", payload) + "\n</body>\n</html>\n")
out = HERE / "output" / "Manosaba_card_strength.html"
out.write_text(html, encoding="utf-8")
sys.stdout.reconfigure(encoding="utf-8")
print(f"{out}（{out.stat().st_size / 1024:.0f} KB，{len(data['cards'])} 張卡）")
