"""卡牌效果量化評分腳本。

以原版 Slay the Spire 2 反編譯卡牌為基準，計算每張卡「每點能量的價值」，
並和原版同稀有度的分佈比較，標出可能過強或過弱的模組卡。

用法：
    python tools/card_score/card_score.py [--base <反編譯 Models 目錄>] [--out <輸出目錄>]

輸出：
    cards.csv   每張卡（原版＋模組）的評分明細
    report.md   摘要：原版基準、各角色統計、離群卡清單
"""
from __future__ import annotations

import argparse
import csv
import json
import re
import statistics
from dataclasses import dataclass, field
from pathlib import Path

HERE = Path(__file__).resolve().parent
import sys  # noqa: E402
sys.path.insert(0, str(HERE))
from random_models import MODELS as RANDOM_MODELS, RANDOM_IGNORED  # noqa: E402
import turn_model  # noqa: E402

# 只代表「隨機目標」，不影響期望值
RANDOM_TARGET_ONLY = re.compile(r"RandomEnemy|RandomOpponents|RandomAlly|HittableEnemies|NextItem\((?:enemies|teammates|candidates|noahTeammates)")
RANDOM_HINT = re.compile(r"Rng\.|NextInt|NextFloat|NextItem|\.Random|Random\w*\(")
REPO = HERE.parent.parent
DEFAULT_BASE = REPO.parent / "decompile1118" / "src" / "Core" / "Models"
MOD_CHAR_DIR = REPO / "Manosaba" / "Characters"
MOD_LOC = REPO / "Manosaba" / "Manosaba" / "localization" / "zht" / "cards.json"

SCORED_RARITIES = ["Basic", "Common", "Uncommon", "Rare"]
ORIGINAL_POOLS = ["Ironclad", "Silent", "Defect", "Necrobinder", "Regent", "Colorless"]
BETA_POOLS = ["Fool"]
DEFAULT_BETA = REPO.parent / "decompile_beta1119" / "src" / "Core" / "Models"

# 只代表條件／計算型效果，數值無法直接換算
CONDITIONAL_VARS = {"CalculationBase", "CalculationExtra", "CalculatedDamage", "CalculatedBlock",
                    "Calculated", "ExtraDamage"}
# OnPlay 中出現這些代表有額外邏輯，評分只是部分正確
PARTIAL_HINTS = ["CardSelectCmd", "OrbCmd", "Channel", "if (", "switch", "foreach", "Random", "Rng"]

CLASS_RE = re.compile(r"\bclass\s+(\w+)")
BASE_RE = re.compile(r":\s*base\(([^()]*)\)")
VAR_RE = re.compile(r"new\s+(PowerVar<(\w+)>|(\w*)Var)\s*\(([^()]*)\)")
UPG_VAR_RE = re.compile(r"DynamicVars(?:\.(\w+)|\[\"(\w+)\"\])\.UpgradeValueBy\(\s*(-?[\d.]+)m?\s*\)")
UPG_COST_RE = re.compile(r"EnergyCost\.UpgradeBy\(\s*(-?\d+)\s*\)")
ADD_KW_RE = re.compile(r"AddKeyword\(\s*CardKeyword\.(\w+)\s*\)")
REM_KW_RE = re.compile(r"RemoveKeyword\(\s*CardKeyword\.(\w+)\s*\)")
POOL_ATTR_RE = re.compile(r"\[Pool\(typeof\((\w+)CardPool\)\)\]")
NUM_RE = re.compile(r"^-?\d+(?:\.\d+)?m?$")


@dataclass
class Card:
    source: str            # original / mod
    pool: str
    name: str
    cost: int | None
    x_cost: bool
    type: str
    rarity: str
    target: str
    vars: dict[str, float] = field(default_factory=dict)
    keywords: set[str] = field(default_factory=set)
    upg_vars: dict[str, float] = field(default_factory=dict)
    upg_cost: int = 0
    upg_add_kw: set[str] = field(default_factory=set)
    upg_rem_kw: set[str] = field(default_factory=set)
    body: str = ""
    file: str = ""
    # 計算結果
    value: float = 0.0
    score: float | None = None
    score_upg: float | None = None
    confidence: str = "auto"
    unknown: list[str] = field(default_factory=list)
    percentile: float | None = None
    relative: float | None = None
    var_mult: dict[str, float] = field(default_factory=dict)   # 隨機分支機率
    var_rename: dict[str, str] = field(default_factory=dict)   # 暫時性能力改名
    notes: list[str] = field(default_factory=list)
    random_status: str = ""   # model / branch / target-only / unmodeled
    parent: str = ""
    tags: set[str] = field(default_factory=set)
    mp_constraint: str = ""       # MultiplayerOnly / SingleplayerOnly / ""
    bucket: str = ""              # 比較用稀有度分組（瑪格塔羅 → Normal）
    raw: float | None = None        # 不含關鍵字、不除費用的效果價值
    raw_upg: float | None = None
    raw_multi: float | None = None  # 多人模式效果價值（無 agent 估計時等於 raw）
    raw_multi_upg: float | None = None
    score_solo: float | None = None
    score_multi: float | None = None
    kw_upg: set[str] = field(default_factory=set)
    cost_upg: int | None = None
    verdict: str = ""
    title: str = ""


def norm_var(name: str) -> str:
    return name[:-5] if name.endswith("Power") and name != "Power" else name


def split_classes(text: str):
    """依 class 宣告切段，回傳 (類別名稱, 本段文字, 前置文字)。"""
    matches = list(CLASS_RE.finditer(text))
    for i, m in enumerate(matches):
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        prev_end = matches[i - 1].end() if i > 0 else 0
        yield m.group(1), text[m.start():end], text[prev_end:m.start()]


def resolve_arg(arg: str, body: str, file_text: str) -> str:
    arg = arg.strip()
    if NUM_RE.match(arg) or "." in arg:
        return arg
    m = re.search(rf"const\s+\w+\s+{re.escape(arg)}\s*=\s*([^;]+);", body) or \
        re.search(rf"const\s+\w+\s+{re.escape(arg)}\s*=\s*([^;]+);", file_text)
    return m.group(1).strip() if m else arg


def parse_vars(body: str, card: Card):
    m = re.search(r"CanonicalVars\s*=>(.*?);\s*\n", body, re.S)
    seg = m.group(1) if m else ""
    for vm in VAR_RE.finditer(seg):
        power, plain, args = vm.group(2), vm.group(3), vm.group(4)
        parts = [p.strip() for p in args.split(",")]
        name = norm_var(power) if power else (plain or "Dynamic")
        num = None
        for p in parts:
            if p.startswith('"'):
                if not power:
                    name = "custom:" + p.strip('"')
                continue
            if NUM_RE.match(p):
                num = float(p.rstrip("m"))
                break
        if num is None:
            card.unknown.append(name)
            continue
        if name == "Dynamic":
            name = "custom"
        card.vars[name] = card.vars.get(name, 0) + num


def parse_card(source, pool, name, body, file_text, path) -> Card | None:
    args = None
    for bm in BASE_RE.finditer(body):
        a = [resolve_arg(x, body, file_text) for x in bm.group(1).split(",")]
        if len(a) >= 4 and a[1].startswith("CardType."):
            args = a
            break
    if not args and re.match(r"class\s+\w+\s*:\s*HoshoMagoArcanaBase", body):
        # 瑪格大阿爾克那：base(費用 = 1, 類型 = Skill, 目標 = Self)，稀有度固定 Ancient
        am = re.search(r":\s*base\(([^()]*)\)", body)
        parts = [p.strip() for p in (am.group(1).split(",") if am and am.group(1).strip() else [])]
        cost = parts[0] if parts and parts[0].lstrip("-").isdigit() else "1"
        ctype = next((p for p in parts if p.startswith("CardType.")), "CardType.Skill")
        target = next((p for p in parts if p.startswith("TargetType.")), "TargetType.Self")
        args = [cost, ctype, "CardRarity.Ancient", target]
    if not args:
        return None
    cost_s = args[0]
    try:
        cost = int(cost_s)
    except ValueError:
        cost = None
    card = Card(source=source, pool=pool, name=name, cost=cost,
                x_cost="HasEnergyCostX => true" in body,
                type=args[1].split(".")[-1], rarity=args[2].split(".")[-1],
                target=args[3].split(".")[-1], body=body, file=str(path))
    parse_vars(body, card)
    km = re.search(r"CanonicalKeywords\s*=>(.*?);", body, re.S)
    if km:
        card.keywords = set(re.findall(r"(?:CardKeyword|ManosabaKeywords)\.(\w+)", km.group(1)))
    tm = re.search(r"CanonicalTags\s*=>(.*?);", body, re.S)
    if tm:
        card.tags = set(re.findall(r"(?:CardTag|ManosabaCardTags)\.(\w+)", tm.group(1)))
    pm = re.match(r"class\s+\w+\s*(?:\([^)]*\))?\s*:\s*(\w+)", body)
    card.parent = pm.group(1) if pm else ""
    nocomment = re.sub(r"//[^\n]*|/\*.*?\*/", "", body, flags=re.S)   # 被註解掉的限制不算
    mm = re.search(r"MultiplayerConstraint\s*=>\s*CardMultiplayerConstraint\.(\w+)", nocomment)
    card.mp_constraint = mm.group(1) if mm else ""
    um = re.search(r"void\s+OnUpgrade\s*\(\s*\)(.*)", body, re.S)
    if um:
        up = um.group(1)
        for a, b, v in UPG_VAR_RE.findall(up):
            key = norm_var(a or b)
            card.upg_vars[key] = card.upg_vars.get(key, 0) + float(v)
        card.upg_cost = sum(int(x) for x in UPG_COST_RE.findall(up))
        card.upg_add_kw = set(ADD_KW_RE.findall(up))
        card.upg_rem_kw = set(REM_KW_RE.findall(up))
    return card


TEMP_BASE_RE = re.compile(r"class\s+(\w+)\s*:\s*(\w+)")
TEMP_KINDS = {"TemporaryStrengthPower": "Strength", "TemporaryDexterityPower": "Dexterity",
              "TemporaryFocusPower": "Focus", "ManosabaTemporaryStrengthPower": "Strength"}
VAR_REF_RE = re.compile(r"DynamicVars(?:\.(\w+)|\[\"(\w+)\"\])")


def load_temp_powers(dirs: list[Path]) -> dict[str, str]:
    """找出所有暫時性（單回合）力量／敏捷／集中能力。"""
    parent: dict[str, str] = {}
    for d in dirs:
        for f in d.rglob("*Power*.cs"):
            for m in TEMP_BASE_RE.finditer(f.read_text(encoding="utf-8-sig", errors="replace")):
                parent[m.group(1)] = m.group(2)
    out = {}
    for cls in parent:
        cur, seen = cls, set()
        while cur in parent and cur not in seen and cur not in TEMP_KINDS:
            seen.add(cur)
            cur = parent[cur]
        if cur in TEMP_KINDS:
            out[cls] = TEMP_KINDS[cur]
    return out


def var_refs(text: str) -> set[str]:
    return {norm_var(a or b) for a, b in VAR_REF_RE.findall(text)}


def analyze_onplay(card: Card, temp_powers: dict[str, str]):
    """偵測暫時性能力與隨機分支，產生改名與機率倍率。"""
    m = re.search(r"OnPlay\s*\((.*?)(?=void\s+OnUpgrade|\Z)", card.body, re.S)
    onplay = m.group(1) if m else ""
    # 暫時性能力：Apply<TempPower>(... DynamicVars.Strength ...)
    for am in re.finditer(r"Apply<(\w+)>\s*\(([^;]*)", onplay):
        kind = temp_powers.get(am.group(1))
        if kind:
            for ref in var_refs(am.group(2)) or {kind}:
                if ref in card.vars:
                    card.var_rename[ref] = "Temp" + kind
    if card.var_rename:
        card.notes.append("暫時性：" + ",".join(f"{k}→{v}" for k, v in card.var_rename.items()))
    # 有專屬期望值模型（骰子、生成類等）
    model = RANDOM_MODELS.get(card.name) if card.source == "mod" else None
    if model:
        base_vars, upg_vars, note = model(dict(card.vars), dict(card.upg_vars))
        card.upg_vars = {k: upg_vars.get(k, 0) - base_vars.get(k, 0) for k in set(base_vars) | set(upg_vars)}
        card.vars = base_vars
        card.unknown = []
        card.random_status = "model"
        card.notes.append("期望值模型：" + note)
        return
    # 隨機分支：NextInt(N) + if (roll < X) / else if / else
    nm = re.search(r"NextInt\(\s*(\d+)\s*\)", onplay)
    if card.source == "mod" and card.name in RANDOM_IGNORED:
        card.random_status = "ignored"
        card.notes.append("隨機不影響期望值：" + RANDOM_IGNORED[card.name])
    elif RANDOM_HINT.search(onplay):
        card.random_status = "target-only" if not RANDOM_HINT.search(RANDOM_TARGET_ONLY.sub("", onplay)) else "unmodeled"
    if not nm:
        return
    n = int(nm.group(1))
    tail = onplay[nm.end():]
    probs: dict[str, float] = {}
    splits = list(re.finditer(r"(?:else\s+)?if\s*\(\s*\w+\s*<\s*(\d+)\s*\)|\belse\b(?!\s+if)", tail))
    if splits and re.search(r"<\s*\d+", splits[0].group(0)):
        prev = 0
        for i, sm in enumerate(splits):
            seg = tail[sm.end(): splits[i + 1].start() if i + 1 < len(splits) else len(tail)]
            if sm.group(1):
                th = int(sm.group(1)); p = (th - prev) / n; prev = th
            else:
                p = (n - prev) / n
            for r in var_refs(seg):
                probs[r] = probs.get(r, 0) + p
    else:
        cases = list(re.finditer(r"case\s+\d+\s*:", tail))
        for i, cm in enumerate(cases):
            seg = tail[cm.end(): cases[i + 1].start() if i + 1 < len(cases) else len(tail)]
            for r in var_refs(seg):
                probs[r] = probs.get(r, 0) + 1 / n
    probs = {k: min(v, 1.0) for k, v in probs.items() if k in card.vars}
    if probs:
        card.random_status = "branch"
        card.var_mult = probs
        card.notes.append("隨機期望值：" + ",".join(f"{k}×{v:.0%}" for k, v in probs.items()))


def apply_overrides(card: Card, ov: dict, w: dict):
    """card_overrides.json：手動指定選項（mode=max 玩家自選；expected 依機率）。"""
    o = ov.get(card.name)
    if not o:
        return None
    if "vars" in o:
        card.vars = {k: float(v) for k, v in o["vars"].items()}
        card.notes.append("手動數值")
    if "options" in o:
        vals = [(opt.get("p", 1.0), compute_value(card, w, {k: float(v) for k, v in opt["vars"].items()}, set()))
                for opt in o["options"]]
        if o.get("mode", "max") == "max":
            card.notes.append("自選取最大")
            return max(v for _, v in vals)
        card.notes.append("手動期望值")
        return sum(p * v for p, v in vals)
    return None


def load_original(base: Path, pools: list[str] = ORIGINAL_POOLS, source: str = "original") -> list[Card]:
    pool_of: dict[str, str] = {}
    for p in pools:
        f = base / "CardPools" / f"{p}CardPool.cs"
        if f.exists():
            for c in re.findall(r"ModelDb\.Card<(\w+)>", f.read_text(encoding="utf-8", errors="replace")):
                pool_of.setdefault(c, p)
    cards = []
    for f in sorted((base / "Cards").glob("*.cs")):
        text = f.read_text(encoding="utf-8", errors="replace")
        for name, body, _ in split_classes(text):
            if name not in pool_of:
                continue
            c = parse_card(source, pool_of[name], name, body, text, f)
            if c:
                cards.append(c)
    return cards


def load_mod(char_dir: Path) -> list[Card]:
    cards = []
    for f in sorted(char_dir.rglob("*.cs")):
        raw = f.read_bytes()
        try:
            text = raw.decode("utf-8-sig")
        except UnicodeDecodeError:
            text = raw.decode("cp950", errors="replace")
        for name, body, pre in split_classes(text):
            pm = POOL_ATTR_RE.findall(pre[-400:])
            if not pm:
                continue
            # 類別內若有巢狀 class，延伸到下一個帶 [Pool] 的類別為止
            start = text.find(body)
            nxt = POOL_ATTR_RE.search(text, start + len(name) + 6)
            full_body = text[start: nxt.start() if nxt else len(text)]
            c = parse_card("mod", pm[-1], name, full_body, text, f.relative_to(REPO))
            if c:
                cards.append(c)
    return cards


GEN_RESOLVER = None  # 第二階段設定：解析 gen:<位置>:<池> 的價值


def compute_value(card: Card, w: dict, vars_: dict[str, float], keywords: set[str]) -> float:
    eff, pw = w["effects"], w["powers"]
    gen_total = 0.0
    for k, v in vars_.items():
        if k.startswith("gen:") and GEN_RESOLVER:
            gen_total += v * GEN_RESOLVER(k)
    vars_ = {k: v for k, v in vars_.items() if not k.startswith("gen:")}
    repeat = vars_.get("Repeat", 1) or 1
    mult = w["target_multiplier"].get(card.target, 1.0)
    total = 0.0
    for k0, v in vars_.items():
        v = v * card.var_mult.get(k0, 1.0)
        k = card.var_rename.get(k0, k0)
        if k in ("Repeat",) or k in CONDITIONAL_VARS:
            continue
        if k == "Damage":
            total += v * repeat * eff["Damage"] * mult
        elif k in eff and eff[k] is not None:
            total += v * eff[k]
        elif k in pw and pw[k] is not None:
            val = v * pw[k]
            if k in ("Vulnerable", "Weak", "Poison", "Doom", "Burn"):
                val *= mult
            if card.type == "Power":
                val *= w["power_card_turns"]
            total += val
    for kw in keywords:
        total += w["keywords"].get(kw, 0)
    return total + gen_total


LOC_MULT = {"Hand": 1.0, "Draw": 0.7, "Discard": 0.5}
GEN_BASE = 0.4   # 多一張牌本身的價值（約等於抽 1 張的 80%）


def make_gen_resolver(all_cards: list[Card], cal: dict):
    """gen:<Hand|Draw|Discard>:<spec>
    spec：cls:A|B（指定類別平均）、filter:k=v,...（parent/kw/tag/type/norare/notoken/exclude=A|B）、
          best3:filter:...（3 選 1 取最佳的期望值）、generic（未知卡）。"""
    by_name = {c.name: c for c in all_cards}

    def surplus(c: Card) -> float:
        if c.raw is None or c.cost is None or c.cost < 0:
            return 0.0
        return max(0.0, c.raw - cal["ec"](c.cost))

    def select(spec: str) -> list[Card]:
        if spec.startswith("cls:"):
            return [by_name[n] for n in spec[4:].split("|") if n in by_name]
        cs = [c for c in all_cards if c.source == "mod"]
        for cond in spec[7:].split(","):
            k, _, v = cond.partition("=")
            if k == "parent":
                cs = [c for c in cs if c.parent == v]
            elif k == "kw":
                cs = [c for c in cs if v in c.keywords]
            elif k == "tag":
                cs = [c for c in cs if v in c.tags]
            elif k == "type":
                cs = [c for c in cs if c.type == v]
            elif k == "norare":
                cs = [c for c in cs if c.rarity != "Rare"]
            elif k == "notoken":
                cs = [c for c in cs if c.rarity != "Token"]
            elif k == "exclude":
                cs = [c for c in cs if c.name not in v.split("|")]
        return cs

    cache: dict[str, float] = {}

    def resolve(key: str) -> float:
        if key in cache:
            return cache[key]
        _, loc, spec = key.split(":", 2)
        if spec == "generic":
            agg = 0.2
        else:
            best3 = spec.startswith("best3:")
            cs = select(spec[6:] if best3 else spec)
            vals = sorted(surplus(c) for c in cs)
            if not vals:
                agg = 0.2
            elif best3:
                n = len(vals)  # 3 次獨立抽取的最大值期望（可重複）
                agg = sum(v * (((i + 1) / n) ** 3 - (i / n) ** 3) for i, v in enumerate(vals))
            else:
                agg = sum(vals) / len(vals)
        cache[key] = (GEN_BASE + agg) * LOC_MULT.get(loc, 1.0)
        return cache[key]

    return resolve


def classify(card: Card, w: dict):
    pw = w["powers"]
    reasons = list(card.unknown)
    for k in card.vars:
        if k in card.var_rename or k.startswith("gen:"):
            continue
        if k in CONDITIONAL_VARS or (k.startswith("custom") and w["effects"].get(k) is None):
            reasons.append(k)
        elif k not in w["effects"] and k != "Repeat" and pw.get(k) is None:
            reasons.append(k)
    card.unknown = sorted(set(reasons))
    if card.x_cost or card.cost is None or card.cost < 0 or "Unplayable" in card.keywords:
        card.confidence = "manual"
    elif card.unknown or not card.vars:
        card.confidence = "manual"
    elif card.type == "Power":
        card.confidence = "partial"
    else:
        onplay = card.body.split("OnUpgrade")[0]
        card.confidence = "partial" if any(h in onplay for h in PARTIAL_HINTS) else "auto"


def pool_weights(w: dict, pool: str) -> dict:
    """套用該卡池的覆寫權重（反映遺物／機制體系）。"""
    ov = w.get("pool_overrides", {}).get(pool)
    if not ov:
        return w
    merged = dict(w)
    for sec in ("effects", "powers", "keywords", "target_multiplier"):
        if sec in ov:
            merged[sec] = {**w[sec], **ov[sec]}
    return merged


AGENT_DIR = HERE / "agent_estimates"
AGENT_DIR_V2 = HERE / "agent_estimates_v2"


def load_agent_estimates() -> dict[str, dict]:
    """讀取 subagent 對各角色卡牌的估計（agent_estimates/*.json）。"""
    out = {}
    files = []
    for d in (AGENT_DIR, AGENT_DIR_V2, HERE / "agent_estimates_v3_heal", HERE / "agent_estimates_v3_ally", HERE / "agent_estimates_v4"):
        # v2（以錨點卡重估）覆蓋 v1；v3（補血重估、特定隊友機率）再覆蓋；ally 最後套用
        if d.exists():
            files += sorted(d.glob("*.json"))
    if files:
        for f in files:
            try:
                data = json.loads(f.read_text(encoding="utf-8"))
            except json.JSONDecodeError as e:
                print(f"略過無法解析的 {f.name}: {e}")
                continue
            src = data.get("source", "mod")
            src = "original" if src.startswith("original") or f.name.startswith("Original_") else "mod"
            for name, est in data.get("cards", {}).items():
                if not isinstance(est, dict):
                    continue
                num = lambda x: isinstance(x, (int, float))
                if num(est.get("value")) or (est.get("value") is None and num(est.get("value_multi"))):
                    out[(src, name)] = {**est, "_file": f.name}
    return out


AGENT_EST: dict[tuple, dict] = {}

PER_TURN_RE = re.compile(r"每回合|每\s*\d\s*回合|回合(開始|末|結束)|[×x]\s*\d+(\.\d+)?\s*回合|約\s*\d+(\.\d+)?\s*(~|-|～)?\s*\d*\s*回合")
ASSUMED_RE = re.compile(r"(\d+(?:\.\d+)?)\s*回合")


def apply_turn_model(card: Card):
    """持續型能力牌：依戰鬥長度模擬（turn_model）縮放價值，取代固定約 4~4.5 回合的假設。"""
    if card.type != "Power" or card.raw is None:
        return
    if card.confidence.startswith("agent"):
        est = AGENT_EST.get(("mod" if card.source == "mod" else "original", card.name), {})
        text = str(est.get("reasoning", ""))
        if not PER_TURN_RE.search(text):
            return
        nums = [float(x) for x in ASSUMED_RE.findall(text) if 3 <= float(x) <= 6]
        assumed = nums[0] if nums else turn_model.OLD_TURNS
    else:
        assumed = turn_model.OLD_TURNS   # 腳本自動評分的能力（力量等）也隱含整場價值
    f = turn_model.factors()
    base = f["normal"]["expected_turns"]
    inn = f["innate"]["expected_turns"]
    solo_turns = inn if "Innate" in card.keywords else base
    upg_turns = inn if "Innate" in (card.kw_upg or card.keywords) else base
    card.raw *= solo_turns / assumed
    if card.raw_upg is not None:
        card.raw_upg *= upg_turns / assumed
    if card.raw_multi is not None:
        card.raw_multi *= solo_turns / assumed
    if getattr(card, "raw_multi_upg", None) is not None:
        card.raw_multi_upg *= upg_turns / assumed
    card.notes.append(f"回合模型：{assumed:g}→{solo_turns:g} 回合")


def score(card: Card, w: dict, overrides: dict | None = None):
    w = pool_weights(w, card.pool)
    override_value = apply_overrides(card, overrides or {}, w)
    classify(card, w)
    est = AGENT_EST.get(("mod" if card.source == "mod" else "original", card.name))
    if est and override_value is None and card.confidence in ("manual", "partial") \
            and card.cost is not None and not card.x_cost and card.cost >= 0:
        card.confidence = f"agent-{est.get('confidence', 'medium')}"
        card.unknown = []
        card.kw_upg = (card.keywords | card.upg_add_kw) - card.upg_rem_kw
        card.cost_upg = max(card.cost + card.upg_cost, 0)
        solo = est.get("value")
        if solo is None:  # 多人限定卡：單人不存在，以多人值暫代（finalize 會排除單人分數）
            card.mp_constraint = "MultiplayerOnly"
            solo = est["value_multi"]
            card.raw_upg = float(est.get("value_multi_upgraded", solo))
        else:
            card.raw_upg = float(est.get("value_upgraded", solo) if est.get("value_upgraded") is not None else solo)
        card.raw = float(solo)
        if isinstance(est.get("value_multi"), (int, float)):
            card.raw_multi = float(est["value_multi"])
            card.raw_multi_upg = float(est.get("value_multi_upgraded", est["value_multi"]))
        if est.get("reasoning"):
            card.notes.append("agent：" + str(est["reasoning"])[:160])
        return
    if override_value is not None:
        card.confidence = "manual-set"
        card.unknown = []
    if card.cost is None or card.x_cost or card.cost < 0:
        return
    card.kw_upg = (card.keywords | card.upg_add_kw) - card.upg_rem_kw
    card.cost_upg = max(card.cost + card.upg_cost, 0)
    if override_value is not None:
        card.raw = card.raw_upg = override_value
        return
    card.raw = compute_value(card, w, card.vars, set())
    # 主要效果沒被數值捕捉（只剩 Repeat 或關鍵字）→ 改為手動評估
    if card.raw <= 0 and not any(k.startswith("gen:") for k in card.vars):
        card.confidence = "manual"
        card.unknown = sorted(set(card.unknown) | {"主要效果未數值化"})
    uv = dict(card.vars)
    for k, v in card.upg_vars.items():
        uv[k] = uv.get(k, 0) + v
    card.raw_upg = compute_value(card, w, uv, set())


CALIB_KEYWORDS = ["Exhaust", "Ethereal", "Retain", "Innate"]


def calibrate(orig: list[Card], w: dict) -> dict:
    """從原版卡反推費用曲線與關鍵字係數（假設原版多數卡強度相近）。"""
    # 只用腳本自動計算的卡校準，避免 agent 估值影響費用曲線與關鍵字係數
    pool = [c for c in orig if c.raw and c.raw > 0 and c.confidence == "auto" and c.type != "Power"
            and c.rarity in SCORED_RARITIES]
    plain = [c for c in pool if not (c.keywords & set(CALIB_KEYWORDS))]
    base1 = statistics.median([c.raw for c in plain if c.cost == 1])
    eff_cost, samples = {}, {}
    for cost in (0, 1, 2, 3):
        vals = [c.raw for c in plain if (c.cost == cost if cost < 3 else c.cost >= 3)]
        samples[cost] = len(vals)
        eff_cost[cost] = statistics.median(vals) / base1 if len(vals) >= 5 else max(cost, w["zero_cost_denominator"])
    eff_cost[1] = 1.0

    def ec(cost):
        return eff_cost[min(cost, 3)]

    plain_per = statistics.median([c.raw / ec(c.cost) for c in plain])
    kw_mult, kw_n = {}, {}
    fixed = w.get("keyword_fixed", {})
    for kw in CALIB_KEYWORDS:
        vals = [c.raw / ec(c.cost) for c in pool if kw in c.keywords]
        kw_n[kw] = len(vals)
        if kw in fixed:
            kw_mult[kw] = fixed[kw]
        elif len(vals) >= 8:
            # 帶關鍵字的卡效果較強 → 代表關鍵字是代價；係數 = 一般卡 ÷ 關鍵字卡
            kw_mult[kw] = min(1.5, max(0.5, plain_per / statistics.median(vals)))
        else:
            kw_mult[kw] = 1.0 + w["keywords"].get(kw, 0)
    mw = w.get("mode_weights", {"solo": 0.6, "multi": 0.4})
    total = mw["solo"] + mw["multi"]
    return {"eff_cost": eff_cost, "samples": samples, "kw_mult": kw_mult, "kw_n": kw_n, "ec": ec,
            "mode_weights": (mw["solo"] / total, mw["multi"] / total)}


def finalize(card: Card, cal: dict):
    if card.raw is None:
        return

    def mult(kws):
        m = 1.0
        for k in kws:
            if k == "Exhaust" and card.type == "Power":
                continue
            m *= cal["kw_mult"].get(k, 1.0)
        return m

    ws, wm = cal["mode_weights"]
    rm = card.raw if card.raw_multi is None else card.raw_multi
    rmu = card.raw_upg if card.raw_multi_upg is None else card.raw_multi_upg
    k, ku = mult(card.keywords), mult(card.kw_upg)
    ec, ecu = cal["ec"](card.cost), cal["ec"](card.cost_upg)
    card.value = card.raw * k
    card.score_solo = card.value / ec
    card.score_multi = rm * k / ec
    if card.mp_constraint == "MultiplayerOnly":
        # 單人模式卡池會移除此卡（CardPoolModel.RemoveAll），不計單人分數
        card.score_solo = None
        card.score = card.score_multi
        card.score_upg = rmu * ku / ecu
    elif card.mp_constraint == "SingleplayerOnly":
        card.score_multi = None
        card.score = card.score_solo
        card.score_upg = card.raw_upg * ku / ecu
    else:
        card.score = ws * card.score_solo + wm * card.score_multi
        card.score_upg = (ws * card.raw_upg + wm * rmu) * ku / ecu


REVIEW_DIR = HERE / "reviews"
BIAS_SHRINK = 4   # 收斂強度：係數 = 偏差^(n/(n+4))，樣本少時往 1 靠


def apply_pool_bias(cards: list[Card]) -> dict:
    """交叉審核修正：同一位審核員統一評分各卡池抽樣卡，
    以「審核值 ÷ 原估值」的幾何中位數估計各卡池 agent 的鬆緊偏差，並以原版正式版為尺度基準。"""
    import math
    by_key = {f"{c.source}|{c.pool}|{c.name}": c for c in cards}
    ratios: dict[tuple, list[float]] = {}
    if not REVIEW_DIR.exists():
        return {}
    for f in sorted(REVIEW_DIR.glob("review_*.json")):
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
        except json.JSONDecodeError as e:
            print(f"略過無法解析的 {f.name}: {e}")
            continue
        for key, est in data.get("cards", {}).items():
            c = by_key.get(key)
            v = est.get("value") if isinstance(est, dict) else None
            if v is None and isinstance(est, dict):
                v = est.get("value_multi")
            base = c.raw if c and c.mp_constraint != "MultiplayerOnly" else (c.raw_multi if c else None)
            if c and isinstance(v, (int, float)) and v > 0.05 and base and base > 0.05:
                ratios.setdefault((c.source, c.pool), []).append(math.log(v / base))
    if not ratios:
        return {}
    raw_bias = {k: statistics.median(v) for k, v in ratios.items()}
    ref = [b for (src, pool), b in raw_bias.items() if src == "original" and pool != "Colorless"]
    norm = statistics.mean(ref) if ref else 0.0      # 讓原版正式版的平均偏差 = 1
    out = {}
    for (src, pool), b in raw_bias.items():
        n = len(ratios[(src, pool)])
        factor = math.exp((b - norm) * n / (n + BIAS_SHRINK))
        out[f"{src}|{pool}"] = {"factor": round(factor, 3), "raw": round(math.exp(b - norm), 3), "n": n}
        for c in cards:
            if c.source == src and c.pool == pool and c.confidence.startswith("agent"):
                for attr in ("raw", "raw_upg", "raw_multi", "raw_multi_upg"):
                    if getattr(c, attr) is not None:
                        setattr(c, attr, getattr(c, attr) * factor)
    return out


def build_baseline(orig: list[Card]) -> dict[str, list[float]]:
    """原版正式版各稀有度分數分佈；Normal＝普通＋罕見＋稀有合併（瑪格塔羅用）。"""
    baseline: dict[str, list[float]] = {}
    for c in orig:
        if c.source == "original" and c.score is not None and c.confidence != "manual":
            baseline.setdefault(c.rarity, []).append(c.score)
            if c.rarity in ("Common", "Uncommon", "Rare"):
                baseline.setdefault("Normal", []).append(c.score)
    for r in baseline:
        baseline[r].sort()
    return baseline


def percentile(sorted_vals: list[float], x: float) -> float:
    if not sorted_vals:
        return float("nan")
    below = sum(1 for v in sorted_vals if v < x)
    equal = sum(1 for v in sorted_vals if v == x)
    return 100.0 * (below + 0.5 * equal) / len(sorted_vals)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", type=Path, default=DEFAULT_BASE)
    ap.add_argument("--out", type=Path, default=HERE / "output")
    ap.add_argument("--weights", type=Path, default=HERE / "weights.json")
    ap.add_argument("--beta", type=Path, default=DEFAULT_BETA)
    args = ap.parse_args()
    w = json.loads(args.weights.read_text(encoding="utf-8"))
    AGENT_EST.update(load_agent_estimates())

    keep = SCORED_RARITIES + ["Ancient"]
    orig = [c for c in load_original(args.base) if c.rarity in keep]
    # beta 版新角色（愚者）：列入比較，但不納入原版基準
    if args.beta.exists():
        orig += [c for c in load_original(args.beta, BETA_POOLS, "original-beta") if c.rarity in keep]
    all_mod = load_mod(MOD_CHAR_DIR)  # 含 Token，供生成卡查價值
    # 精神創傷（Trauma*）為各角色共通的起始魔女化卡，刻意設計為負面，不列入比較
    mod = [c for c in all_mod if c.rarity in keep and not c.name.startswith("Trauma")]
    for c in orig + mod:
        c.bucket = c.rarity
        # 瑪格的大阿爾克那等同一般卡（由塔羅牌組取代卡牌獎勵）；只有集齊後的「世界」才是真正的 Ancient
        if c.pool == "HoshoMago" and c.rarity == "Ancient" and c.name != "TheWorld":
            c.bucket = "Normal"
    titles = {}
    if MOD_LOC.exists():
        titles = json.loads(MOD_LOC.read_text(encoding="utf-8"))
    temp_powers = load_temp_powers([args.base / "Powers", MOD_CHAR_DIR])
    ov_path = HERE / "card_overrides.json"
    overrides = json.loads(ov_path.read_text(encoding="utf-8")) if ov_path.exists() else {}
    overrides = {k: v for k, v in overrides.items() if not k.startswith("_")}
    for c in orig + all_mod:
        analyze_onplay(c, temp_powers)
        score(c, w, overrides)
        apply_turn_model(c)
    cal = calibrate(orig, w)
    cal["turn_model"] = turn_model.factors()
    cal["pool_bias"] = apply_pool_bias(orig + all_mod)
    for c in orig + all_mod:
        finalize(c, cal)
    # 第二階段：生成卡牌的價值依被生成卡計算（先算被生成卡，再重算生成者）
    global GEN_RESOLVER
    GEN_RESOLVER = make_gen_resolver(all_mod, cal)
    for c in all_mod:
        if any(k.startswith("gen:") for k in c.vars) and c.raw is not None:
            score(c, w, overrides)
            apply_turn_model(c)
            finalize(c, cal)
        if c.source == "mod":
            key = "MANOSABA-" + re.sub(r"(?<!^)(?=[A-Z])", "_", c.name).upper() + ".title"
            c.title = titles.get(key, "")
    # 原版卡名：取官方繁中本地化（beta 卡取 beta 版）
    for src, base in (("original", args.base), ("original-beta", args.beta)):
        loc = base.parents[2] / "localization" / "zht" / "cards.json"
        if loc.exists():
            otitles = json.loads(loc.read_text(encoding="utf-8"))
            for c in orig:
                if c.source == src:
                    c.title = otitles.get(re.sub(r"(?<!^)(?=[A-Z])", "_", c.name).upper() + ".title", "")

    # 原版基準（只用可評分且非 manual 的卡）
    baseline = build_baseline(orig)
    # 類型校準：假設原版攻擊／技能／能力三類平均強度相同，以原版各類型中位數反推係數，套用到所有卡
    type_factor = {}
    calib = [c for c in orig if c.source == "original" and c.pool != "Colorless" and c.bucket in SCORED_RARITIES
             and c.score is not None and c.confidence != "manual" and baseline.get(c.bucket)]
    for t in ("Attack", "Skill", "Power"):
        rels = [c.score / statistics.median(baseline[c.bucket]) for c in calib if c.type == t]
        type_factor[t] = 1 / statistics.median(rels) if len(rels) >= 20 else 1.0
    cal["type_factor"] = type_factor
    for c in orig + all_mod:
        f = type_factor.get(c.type, 1.0)
        for attr in ("score", "score_solo", "score_multi", "score_upg"):
            if getattr(c, attr) is not None:
                setattr(c, attr, getattr(c, attr) * f)
    baseline = build_baseline(orig)
    th = w["outlier_threshold"]
    for c in orig + mod:
        b = baseline.get(c.bucket) or (baseline.get("Rare") if c.bucket == "Ancient" else None)
        if c.score is None or not b or c.confidence == "manual":
            c.verdict = "需手動評估"
            continue
        c.percentile = percentile(b, c.score)
        # 相對強度：以原版同稀有度中位數為 1.0，等於「該稀有度下 1 能量應有的價值」
        c.relative = c.score / statistics.median(b)
        if c.bucket == "Ancient":
            c.verdict = "特殊（Ancient，不列入平均）"
        elif c.relative > 1 + th and c.percentile >= 90:
            c.verdict = "可能過強"
        elif c.relative < 1 - th and c.percentile <= 10:
            c.verdict = "可能過弱"
        else:
            c.verdict = "正常"

    args.out.mkdir(parents=True, exist_ok=True)
    cols = ["source", "pool", "name", "title", "rarity", "type", "cost", "target", "score", "score_solo", "score_multi", "score_upg",
            "relative", "percentile", "verdict", "confidence", "notes", "vars", "upgrade", "keywords", "unknown", "file"]
    with open(args.out / "cards.csv", "w", encoding="utf-8-sig", newline="") as fh:
        wr = csv.writer(fh)
        wr.writerow(cols)
        for c in sorted(orig + mod, key=lambda c: (c.source, c.pool, keep.index(c.rarity), c.name)):
            wr.writerow([c.source, c.pool, c.name, c.title, c.rarity, c.type,
                         "X" if c.x_cost else c.cost, c.target,
                         "" if c.score is None else round(c.score, 2),
                         "" if c.score_solo is None else round(c.score_solo, 2),
                         "" if c.score_multi is None else round(c.score_multi, 2),
                         "" if c.score_upg is None else round(c.score_upg, 2),
                         "" if c.relative is None else round(c.relative, 2),
                         "" if c.percentile is None else round(c.percentile),
                         c.verdict, c.confidence, " / ".join(c.notes),
                         ";".join(f"{k}={v:g}" for k, v in c.vars.items()),
                         ";".join([f"{k}+{v:g}" for k, v in c.upg_vars.items()] +
                                  ([f"cost{c.upg_cost:+d}"] if c.upg_cost else []) +
                                  [f"+{k}" for k in sorted(c.upg_add_kw)] + [f"-{k}" for k in sorted(c.upg_rem_kw)]),
                         ";".join(sorted(c.keywords)), ";".join(c.unknown), c.file])

    write_report(args.out / "report.md", orig, mod, baseline, cal)

    # HTML 報告用的資料
    r2 = lambda x: None if x is None else round(x, 3)
    relics = load_starting_relics()
    data = {
        "baseline": {k: {"n": len(v), "median": r2(statistics.median(v)),
                         "p10": r2(v[int(0.1 * len(v))]), "p90": r2(v[min(len(v) - 1, int(0.9 * len(v)))])}
                     for k, v in baseline.items() if v},
        "calibration": {"eff_cost": cal["eff_cost"], "kw_mult": cal["kw_mult"],
                        "mode_weights": cal["mode_weights"], "type_factor": cal.get("type_factor", {}),
                        "pool_bias": cal.get("pool_bias", {})},
        "relics": relics,
        "cards": [{
            "source": c.source, "pool": c.pool, "name": c.name, "title": c.title, "rarity": c.rarity,
            "bucket": c.bucket, "type": c.type, "cost": "X" if c.x_cost else c.cost,
            "score": r2(c.score), "solo": r2(c.score_solo), "multi": r2(c.score_multi), "upg": r2(c.score_upg),
            "relative": r2(c.relative),
            "rel_solo": r2(c.score_solo / statistics.median(baseline[c.bucket])) if c.score_solo is not None and c.relative is not None and baseline.get(c.bucket) else None,
            "rel_multi": r2(c.score_multi / statistics.median(baseline[c.bucket])) if c.score_multi is not None and c.relative is not None and baseline.get(c.bucket) else None,
            "percentile": r2(c.percentile), "verdict": c.verdict, "confidence": c.confidence,
            "raw": r2(c.raw), "raw_upg": r2(c.raw_upg), "raw_multi": r2(c.raw_multi),
            "mp": c.mp_constraint, "keywords": sorted(c.keywords), "notes": " / ".join(c.notes),
        } for c in orig + mod],
    }
    (args.out / "data.json").write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
    print(f"原版 {len(orig)} 張、模組 {len(mod)} 張 → {args.out}")


def load_starting_relics() -> dict[str, str]:
    """讀取各模組角色的起始遺物，供報告參考機制體系。"""
    out = {}
    for f in MOD_CHAR_DIR.glob("*/*.cs"):
        text = f.read_text(encoding="utf-8-sig", errors="replace")
        m = re.search(r"StartingRelics\s*=>\s*\[(.*?)\]", text, re.S)
        if m:
            out[f.parent.name] = ", ".join(re.findall(r"Relic<(\w+)>", m.group(1)))
    return out


def fmt(x):
    return "-" if x is None else f"{x:.2f}"


def trimmed_mean(xs: list[float], cut: float = 0.1) -> float | None:
    """去掉頭尾各 cut 比例後的平均；卡牌強度右偏，平均值會被少數強卡拉高。"""
    xs = sorted(xs)
    if not xs:
        return None
    k = int(len(xs) * cut)
    core = xs[k:len(xs) - k] or xs
    return sum(core) / len(core)


def pool_summary(cards: list, attr: str = "relative") -> dict:
    xs = [getattr(c, attr) for c in cards if getattr(c, attr) is not None and c.bucket != "Ancient"]
    return {"n": len(xs), "median": statistics.median(xs) if xs else None,
            "trimmed": trimmed_mean(xs), "mean": statistics.mean(xs) if xs else None}


def write_report(path: Path, orig, mod, baseline, cal):
    L = ["# 卡牌量化評分報告", "",
         "分數 = 效果價值（能量單位）× 關鍵字係數 ÷ 有效費用。百分位 = 在原版同稀有度卡中的位置。",
         "confidence：auto 純數值；partial 有額外邏輯（分數可能低估）；manual 無法自動評分。", "",
         "## 從原版校準的係數", "",
         "假設原版多數卡強度相近，反推費用曲線與關鍵字代價（非能力牌、無上述關鍵字的卡）。", "",
         "| 費用 | 有效費用 | 樣本數 |", "|---|---|---|"]
    for k in (0, 1, 2, 3):
        L.append(f"| {k if k < 3 else '3+'} | {cal['eff_cost'][k]:.2f} | {cal['samples'][k]} |")
    L += ["", "| 關鍵字 | 價值係數 | 原版樣本數 |", "|---|---|---|"]
    for k, m in cal["kw_mult"].items():
        L.append(f"| {k} | ×{m:.2f} | {cal['kw_n'][k]} |")
    L += ["", "係數 < 1 表示原版會給帶該關鍵字的卡更強的效果作為補償，因此它是代價；能力牌不套用消耗係數。", "",
         "## 原版基準（auto＋partial）", "", "| 稀有度 | 張數 | P10 | 中位數 | P90 |", "|---|---|---|---|---|"]
    for r in SCORED_RARITIES + ["Normal", "Ancient"]:
        b = baseline.get(r, [])
        if b:
            q = lambda p: b[min(len(b) - 1, int(p * len(b)))]
            L.append(f"| {r} | {len(b)} | {q(0.1):.2f} | {statistics.median(b):.2f} | {q(0.9):.2f} |")
    L += ["", "中位數即「該稀有度下每 1 能量的期望價值」；相對強度 = 分數 ÷ 該中位數（1.00 = 原版平均）。"]
    L += ["", "## 各角色／卡池統計", "",
          "| 來源 | 卡池 | 起始遺物 | 卡數 | 可評分 | 中位數（合併） | 截尾平均 | 平均 | 單人 | 多人 | 平均百分位 | 過強 | 過弱 | 需手動 |",
          "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    groups: dict[tuple, list] = {}
    for c in orig + mod:
        if c.bucket != "Ancient":  # Ancient 為特殊卡，不列入角色平均
            groups.setdefault((c.source, c.pool), []).append(c)
    relics = load_starting_relics()
    for (src, pool), cs in sorted(groups.items()):
        rel = [c.relative for c in cs if c.relative is not None]
        pc = [c.percentile for c in cs if c.percentile is not None]

        def mode_rel(attr):
            xs = [getattr(c, attr) / statistics.median(baseline[c.bucket]) for c in cs
                  if c.relative is not None and getattr(c, attr) is not None and baseline.get(c.bucket)]
            return fmt(statistics.median(xs) if xs else None)
        ps = pool_summary(cs)
        L.append(f"| {src} | {pool} | {relics.get(pool, '')} | {len(cs)} | {len(rel)} | "
                 f"{fmt(ps['median'])} | {fmt(ps['trimmed'])} | {fmt(ps['mean'])} | {mode_rel('score_solo')} | {mode_rel('score_multi')} | "
                 f"{fmt(statistics.mean(pc) if pc else None)} | "
                 f"{sum(c.verdict == '可能過強' for c in cs)} | {sum(c.verdict == '可能過弱' for c in cs)} | "
                 f"{sum(c.verdict == '需手動評估' for c in cs)} |")
    L += ["", "## 尚未定義權重的機制（依出現次數）", "",
          "在 weights.json 的 powers 或 pool_overrides 補上權重後，這些卡就能自動評分。", "",
          "| 卡池 | 機制（次數） |", "|---|---|"]
    for (src, pool), cs in sorted(groups.items()):
        if src != "mod":
            continue
        cnt: dict[str, int] = {}
        for c in cs:
            for u in c.unknown:
                cnt[u] = cnt.get(u, 0) + 1
        if cnt:
            top = sorted(cnt.items(), key=lambda kv: -kv[1])[:8]
            L.append(f"| {pool} | {', '.join(f'{k}({v})' for k, v in top)} |")
    L += ["", "## 隨機效果處理狀態（模組卡）", "",
          "model＝專屬期望值模型（random_models.py）；branch＝自動解析機率分支；",
          "unmodeled＝含隨機但尚未建模（分數未反映隨機部分），可在 random_models.py 補上。", "",
          "| 狀態 | 卡（卡池） |", "|---|---|"]
    for st in ["model", "branch", "unmodeled"]:
        cs = [c for c in mod if c.random_status == st]
        if cs:
            L.append(f"| {st}（{len(cs)}） | {', '.join(f'{c.title or c.name}（{c.pool}）' for c in cs)} |")
    for label in ["可能過強", "可能過弱"]:
        L += ["", f"## 模組卡：{label}", "",
              "| 卡池 | 卡名 | 稀有度 | 費用 | 分數 | 相對強度 | 升級後 | 百分位 | confidence | 數值 | 備註 |",
              "|---|---|---|---|---|---|---|---|---|---|---|"]
        for c in sorted((c for c in mod if c.verdict == label), key=lambda c: -(c.relative or 0)):
            L.append(f"| {c.pool} | {c.title or c.name} | {c.rarity} | {c.cost} | {fmt(c.score)} | {fmt(c.relative)} | "
                     f"{fmt(c.score_upg)} | {c.percentile:.0f} | {c.confidence} | "
                     f"{', '.join(f'{k}={v:g}' for k, v in c.vars.items())} | {' / '.join(c.notes)} |")
    path.write_text("\n".join(L) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
