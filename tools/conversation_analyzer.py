#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
conversation_analyzer.py
=========================
分析 Antigravity AI 對話記錄，整理成結構化摘要：
  1. 起初怎麼提問
  2. 中間如何引導與調整
  3. 最終是如何得出這個成果的
  4. 可重現結果的提示詞

使用方式:
  python conversation_analyzer.py                         # 互動式選單
  python conversation_analyzer.py <conversation_id>       # 直接指定對話 ID
  python conversation_analyzer.py --list                  # 列出所有對話
  python conversation_analyzer.py --api-key <KEY>         # 使用 Gemini API 進行 AI 分析
"""

import os
import sys
import json
import re
import argparse
import textwrap
from pathlib import Path
from datetime import datetime
from typing import Optional

# ─────────────────────────────────────────────
# 設定
# ─────────────────────────────────────────────

# Antigravity 對話記錄根目錄（自動偵測）
BRAIN_ROOT = Path(os.environ.get("APPDATA", "")) / "Gemini" / "antigravity" / "brain"
# 備用路徑（若上述不存在）
BRAIN_ROOT_ALT = Path.home() / ".gemini" / "antigravity" / "brain"

# 輸出報告的預設目錄
OUTPUT_DIR = Path(__file__).parent / "conversation_reports"

# ─────────────────────────────────────────────
# 工具函式
# ─────────────────────────────────────────────

def get_brain_root() -> Path:
    """自動找到 Antigravity brain 目錄"""
    candidates = [
        BRAIN_ROOT,
        BRAIN_ROOT_ALT,
        # Windows 特殊路徑
        Path(os.environ.get("USERPROFILE", "")) / ".gemini" / "antigravity" / "brain",
        Path("C:/Users") / os.environ.get("USERNAME", "") / ".gemini" / "antigravity" / "brain",
    ]
    for p in candidates:
        if p.exists():
            return p
    # 讓使用者手動輸入
    print("⚠️  找不到 Antigravity brain 目錄，請手動輸入路徑：")
    manual = input("路徑: ").strip()
    return Path(manual)


def list_conversations(brain_root: Path) -> list[dict]:
    """列出所有對話，並嘗試讀取摘要"""
    convs = []
    for d in brain_root.iterdir():
        if not d.is_dir() or d.name.startswith("."):
            continue
        # UUID 格式檢查
        if not re.match(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", d.name):
            continue

        meta = {"id": d.name, "title": "（無標題）", "created": "", "artifacts": []}

        # 嘗試讀取 metadata.json
        meta_file = d / "metadata.json"
        if meta_file.exists():
            try:
                with open(meta_file, encoding="utf-8") as f:
                    data = json.load(f)
                    meta["title"] = data.get("title", meta["title"])
                    meta["created"] = data.get("created", "")
            except Exception:
                pass

        # 收集 artifact 清單（.md 檔）
        for f in d.iterdir():
            if f.suffix == ".md" and not f.name.startswith("."):
                meta["artifacts"].append(f.name)

        # 目錄修改時間作為備用時間
        if not meta["created"]:
            mtime = d.stat().st_mtime
            meta["created"] = datetime.fromtimestamp(mtime).strftime("%Y-%m-%d %H:%M")

        convs.append(meta)

    convs.sort(key=lambda x: x["created"], reverse=True)
    return convs


def read_artifact(path: Path) -> str:
    """讀取 artifact 內容"""
    try:
        with open(path, encoding="utf-8") as f:
            return f.read()
    except Exception as e:
        return f"（讀取失敗: {e}）"


def collect_conversation_content(conv_dir: Path) -> dict:
    """收集對話目錄中的所有可用內容"""
    content = {
        "id": conv_dir.name,
        "artifacts": {},
        "metadata_files": {},
    }

    for f in conv_dir.iterdir():
        if f.is_file():
            if f.suffix == ".md" and not f.name.endswith(".resolved") and "." not in f.stem:
                content["artifacts"][f.name] = read_artifact(f)
            elif f.suffix == ".json":
                try:
                    with open(f, encoding="utf-8") as fp:
                        content["metadata_files"][f.name] = json.load(fp)
                except Exception:
                    pass

    return content


# ─────────────────────────────────────────────
# 規則式分析（不需要 API）
# ─────────────────────────────────────────────

def rule_based_analysis(content: dict, conv_meta: dict) -> dict:
    """
    用規則式方法從 artifact 內容中萃取對話結構。
    回傳結構化分析結果。
    """
    artifacts_text = "\n\n".join(
        f"=== {name} ===\n{body}"
        for name, body in content["artifacts"].items()
    )

    title = conv_meta.get("title", "（無標題）")
    created = conv_meta.get("created", "")

    # ── 第 1 節：起初怎麼提問 ──────────────────
    # 嘗試從 artifact 標題和開頭段落推斷
    first_section_lines = []
    for name, body in content["artifacts"].items():
        lines = body.strip().splitlines()
        # 取前幾段（跳過標題行）
        intro_lines = [l for l in lines[:30] if l.strip() and not l.startswith("#")][:5]
        if intro_lines:
            first_section_lines.extend(intro_lines)
            break

    initial_question = (
        "\n".join(first_section_lines[:3])
        if first_section_lines
        else f"（根據對話標題推測）使用者詢問關於「{title}」的相關問題。"
    )

    # ── 第 2 節：中間引導與調整 ────────────────
    # 從各 artifact 的標題/章節結構推斷迭代過程
    iteration_steps = []
    for name, body in content["artifacts"].items():
        headers = re.findall(r"^#{1,3}\s+(.+)$", body, re.MULTILINE)
        if headers:
            iteration_steps.append(f"📄 **{name}** 涵蓋章節：")
            for h in headers[:6]:
                iteration_steps.append(f"   - {h}")

    # ── 第 3 節：最終成果 ──────────────────────
    outcomes = []
    for name, body in content["artifacts"].items():
        # 抓最後幾個標題或段落作為成果摘要
        lines = body.strip().splitlines()
        last_headers = re.findall(r"^#{1,3}\s+(.+)$", body, re.MULTILINE)
        word_count = len(body)
        outcomes.append(f"- **{name}**（{word_count:,} 字元）：{last_headers[-1] if last_headers else '完整文件'}")

    # ── 第 4 節：可重現提示詞 ──────────────────
    reproducible_prompt = generate_reproducible_prompt(title, content, initial_question)

    return {
        "title": title,
        "created": created,
        "initial_question": initial_question,
        "iteration_steps": iteration_steps,
        "outcomes": outcomes,
        "reproducible_prompt": reproducible_prompt,
        "artifacts": list(content["artifacts"].keys()),
    }


def generate_reproducible_prompt(title: str, content: dict, initial_question: str) -> str:
    """根據對話內容生成可重現的提示詞"""
    artifact_names = list(content["artifacts"].keys())

    # 從 artifacts 推斷目的
    purposes = []
    for name in artifact_names:
        body = content["artifacts"].get(name, "")
        if "資料庫" in name or "schema" in name.lower() or "database" in name.lower():
            purposes.append("資料庫結構分析與設計")
        if "架構" in name or "architecture" in name.lower():
            purposes.append("系統架構設計")
        if "設計" in name or "design" in name.lower():
            purposes.append("功能設計與規劃")
        if "分析" in name or "analysis" in name.lower():
            purposes.append("現況分析與評估")

    purpose_str = "、".join(set(purposes)) if purposes else "分析與設計"

    prompt = f"""# 可重現結果的提示詞

## 情境設定提示詞

```
你是一個專業的 ERP 系統顧問與軟體架構師。
請協助我完成「{title}」相關的工作。

【背景說明】
- 系統類型：企業資源規劃（ERP）系統
- 工作目標：{purpose_str}
- 輸出格式：Markdown 格式的結構化文件

【任務說明】
{initial_question}

【期望輸出】
請提供：
1. 詳細的分析與規劃文件
2. 具體的實施步驟與建議
3. 相關的程式碼或 SQL 範例（如適用）
4. 潛在問題與解決方案

【輸出要求】
- 使用繁體中文
- 結構清晰，使用標題層級組織內容
- 提供具體可執行的內容，避免過於抽象的建議
- 若涉及資料庫，請提供完整的 Schema 定義
```

## 迭代優化提示詞

```
在上述基礎上，請進一步：
1. 補充細節：針對 [具體章節] 提供更深入的說明
2. 調整方向：將重點放在 [特定需求] 上
3. 新增內容：加入 [缺少的部分]
4. 格式優化：改善文件的可讀性與實用性
```

## 產生物件清單（本次對話的輸出）
{chr(10).join(f'- {name}' for name in artifact_names)}
"""
    return prompt


# ─────────────────────────────────────────────
# Gemini API 分析（需要 API Key）
# ─────────────────────────────────────────────

def gemini_analysis(content: dict, conv_meta: dict, api_key: str) -> Optional[dict]:
    """使用 Gemini API 進行深度分析（需要安裝 google-generativeai）"""
    try:
        import google.generativeai as genai
    except ImportError:
        print("⚠️  未安裝 google-generativeai，改用規則式分析。")
        print("   安裝指令：pip install google-generativeai")
        return None

    genai.configure(api_key=api_key)
    model = genai.GenerativeModel("gemini-2.0-flash")

    artifacts_text = "\n\n".join(
        f"=== ARTIFACT: {name} ===\n{body[:3000]}..."  # 限制長度
        for name, body in content["artifacts"].items()
    )

    title_str = conv_meta.get("title", "（未知）")
    prompt = f"""你是一個對話分析專家。以下是一個 AI 助理對話的「產出物（Artifacts）」，
這些是對話中 AI 針對使用者需求所產生的文件。

請根據這些產出物，推斷並整理這場對話的結構：

## 對話標題
{title_str}

## 對話產出物內容
{artifacts_text}

---

請用繁體中文，以 JSON 格式回傳以下結構分析：

{{
  "initial_question": "推斷使用者最初的提問是什麼（1-3 句話）",
  "iteration_steps": [
    "第一輪：使用者的初始需求描述",
    "第二輪：如何引導或調整需求",
    "第三輪：深化或補充的部分",
    ...
  ],
  "final_outcome": "最終達成了什麼成果（2-4 句話）",
  "key_decisions": [
    "過程中的關鍵決策或轉折點1",
    "過程中的關鍵決策或轉折點2"
  ],
  "reproducible_prompt": "一段完整的提示詞，讓其他人可以用這段文字重現類似的對話成果"
}}

只回傳 JSON，不要有其他說明文字。"""

    try:
        response = model.generate_content(prompt)
        text = response.text.strip()
        # 清除可能的 markdown code block 包裝
        text = re.sub(r"^```(?:json)?\n?", "", text)
        text = re.sub(r"\n?```$", "", text)
        result = json.loads(text)
        result["title"] = conv_meta.get("title", "（未知）")
        result["created"] = conv_meta.get("created", "")
        result["artifacts"] = list(content["artifacts"].keys())
        return result
    except Exception as e:
        print(f"⚠️  Gemini API 分析失敗：{e}")
        return None


# ─────────────────────────────────────────────
# 報告生成
# ─────────────────────────────────────────────

def format_report(analysis: dict, use_api: bool = False) -> str:
    """將分析結果格式化為 Markdown 報告"""
    title = analysis.get("title", "未知對話")
    created = analysis.get("created", "")
    artifacts = analysis.get("artifacts", [])

    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    lines = [
        f"# 對話分析報告：{title}",
        f"\n> 🕐 對話時間：{created}　　📊 分析時間：{now}",
        f"> 📎 包含產出物：{len(artifacts)} 個",
        "",
        "---",
        "",
    ]

    # ── 第 1 節 ──────────────────────────────
    lines.append("## 1️⃣  起初怎麼提問")
    lines.append("")

    if use_api:
        lines.append(analysis.get("initial_question", "（無法推斷）"))
    else:
        lines.append(analysis.get("initial_question", "（無法推斷）"))

    lines.append("")
    lines.append("---")
    lines.append("")

    # ── 第 2 節 ──────────────────────────────
    lines.append("## 2️⃣  中間如何引導與調整")
    lines.append("")

    if use_api:
        steps = analysis.get("iteration_steps", [])
        if steps:
            for i, step in enumerate(steps, 1):
                lines.append(f"**第 {i} 輪**：{step}")
                lines.append("")
        else:
            lines.append("（無迭代記錄）")
    else:
        steps = analysis.get("iteration_steps", [])
        if steps:
            lines.extend(steps)
        else:
            lines.append("（無法從現有資料推斷迭代過程）")

    lines.append("")

    if use_api and analysis.get("key_decisions"):
        lines.append("### 🔑 關鍵決策點")
        lines.append("")
        for d in analysis["key_decisions"]:
            lines.append(f"- {d}")
        lines.append("")

    lines.append("---")
    lines.append("")

    # ── 第 3 節 ──────────────────────────────
    lines.append("## 3️⃣  最終如何得出成果")
    lines.append("")

    if use_api:
        lines.append(analysis.get("final_outcome", "（無法推斷）"))
        lines.append("")
    else:
        outcomes = analysis.get("outcomes", [])
        if outcomes:
            lines.append("### 📦 產出物清單")
            lines.append("")
            lines.extend(outcomes)
        else:
            lines.append("（無產出物記錄）")
    lines.append("")
    lines.append("---")
    lines.append("")

    # ── 第 4 節：可重現提示詞 ─────────────────
    lines.append("## 4️⃣  可重現結果的提示詞")
    lines.append("")

    if use_api:
        repro = analysis.get("reproducible_prompt", "")
        if repro:
            lines.append("```")
            lines.append(repro)
            lines.append("```")
    else:
        lines.append(analysis.get("reproducible_prompt", "（無法生成）"))

    lines.append("")
    lines.append("---")
    lines.append("")
    lines.append("*本報告由 `conversation_analyzer.py` 自動生成*")

    return "\n".join(lines)


# ─────────────────────────────────────────────
# 互動選單
# ─────────────────────────────────────────────

def interactive_menu(brain_root: Path, api_key: Optional[str] = None):
    """互動式選單"""
    print("\n" + "═" * 60)
    print("  🔍  Antigravity 對話分析工具")
    print("═" * 60)

    convs = list_conversations(brain_root)
    if not convs:
        print("❌ 找不到任何對話記錄")
        return

    print(f"\n找到 {len(convs)} 個對話記錄：\n")
    for i, c in enumerate(convs, 1):
        artifact_info = f"[{len(c['artifacts'])} 個產出物]" if c["artifacts"] else "[無產出物]"
        print(f"  {i:2}. {c['title']}")
        print(f"      ID: {c['id'][:8]}...  時間: {c['created']}  {artifact_info}")
        print()

    print("  0. 退出")
    print()

    while True:
        try:
            choice = input("請輸入編號（或對話 ID）：").strip()
            if choice == "0":
                return
            if choice.isdigit() and 1 <= int(choice) <= len(convs):
                selected = convs[int(choice) - 1]
            else:
                # 當作 ID 或 ID 前綴
                matches = [c for c in convs if c["id"].startswith(choice)]
                if not matches:
                    print("❌ 找不到對應的對話，請重新輸入")
                    continue
                selected = matches[0]
            break
        except KeyboardInterrupt:
            print("\n已取消")
            return

    run_analysis(brain_root, selected["id"], api_key)


def run_analysis(brain_root: Path, conv_id: str, api_key: Optional[str] = None):
    """執行單一對話的分析"""
    conv_dir = brain_root / conv_id
    if not conv_dir.exists():
        # 嘗試前綴匹配
        matches = [d for d in brain_root.iterdir() if d.name.startswith(conv_id)]
        if not matches:
            print(f"❌ 找不到對話目錄：{conv_id}")
            return
        conv_dir = matches[0]

    print(f"\n🔄 正在分析對話：{conv_dir.name}")

    convs = list_conversations(brain_root)
    conv_meta = next((c for c in convs if c["id"] == conv_dir.name), {"id": conv_dir.name, "title": "（未知）", "created": ""})

    content = collect_conversation_content(conv_dir)

    if not content["artifacts"]:
        print("⚠️  此對話沒有 Markdown 產出物，將生成基礎報告")

    # 選擇分析方式
    analysis = None
    use_api = False

    if api_key:
        print("🤖 使用 Gemini API 進行深度分析...")
        analysis = gemini_analysis(content, conv_meta, api_key)
        if analysis:
            use_api = True

    if not analysis:
        print("📝 使用規則式分析...")
        analysis = rule_based_analysis(content, conv_meta)

    # 生成報告
    report = format_report(analysis, use_api)

    # 輸出到終端
    print("\n" + "═" * 60)
    print(report)
    print("═" * 60)

    # 儲存到檔案
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    safe_title = re.sub(r'[\\/:*?"<>|]', "_", conv_meta.get("title", conv_dir.name))[:50]
    out_file = OUTPUT_DIR / f"{conv_dir.name[:8]}_{safe_title}.md"

    with open(out_file, "w", encoding="utf-8") as f:
        f.write(report)

    print(f"\n✅ 報告已儲存至：{out_file}")


# ─────────────────────────────────────────────
# 主程式
# ─────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(
        description="分析 Antigravity AI 對話並整理成結構化摘要",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=textwrap.dedent("""
        使用範例：
          python conversation_analyzer.py                   # 互動式選單
          python conversation_analyzer.py --list            # 列出所有對話
          python conversation_analyzer.py 1a481da1          # 分析指定對話
          python conversation_analyzer.py --api-key AIza... # 使用 Gemini API
        """),
    )
    parser.add_argument("conv_id", nargs="?", help="對話 ID 或 ID 前綴")
    parser.add_argument("--list", "-l", action="store_true", help="列出所有對話")
    parser.add_argument("--api-key", "-k", help="Gemini API Key（用於 AI 深度分析）")
    parser.add_argument("--brain-root", help="自訂 brain 目錄路徑")

    args = parser.parse_args()

    # 取得 brain 目錄
    if args.brain_root:
        brain_root = Path(args.brain_root)
    else:
        brain_root = get_brain_root()

    if not brain_root.exists():
        print(f"❌ Brain 目錄不存在：{brain_root}")
        sys.exit(1)

    # API Key（也可從環境變數讀取）
    api_key = args.api_key or os.environ.get("GEMINI_API_KEY")

    if args.list:
        # 列出所有對話
        convs = list_conversations(brain_root)
        print(f"\n共找到 {len(convs)} 個對話：\n")
        for c in convs:
            print(f"  {c['id']}")
            print(f"  標題：{c['title']}")
            print(f"  時間：{c['created']}")
            print(f"  產出物：{', '.join(c['artifacts']) if c['artifacts'] else '（無）'}")
            print()
    elif args.conv_id:
        # 直接分析指定對話
        run_analysis(brain_root, args.conv_id, api_key)
    else:
        # 互動式選單
        interactive_menu(brain_root, api_key)


if __name__ == "__main__":
    main()
