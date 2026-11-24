"""隨機效果卡的期望值模型。

每個模型讀取卡牌的原始數值（card.vars，含升級前數值），回傳：
    (vars, upgraded_vars, note)
其中 vars／upgraded_vars 是換算成期望值後、可直接套用權重的數值。
模型只描述「隨機部分」；非隨機的效果照原本數值保留。

新增隨機卡時：在 MODELS 加入 "C#類別名稱": 函式。
"""
from __future__ import annotations

from itertools import product

D6 = range(1, 7)


def _mean(xs):
    xs = list(xs)
    return sum(xs) / len(xs)


def angels_dice(v, up):
    mult = v.get("custom:BlockMultiplier", 2)
    umult = mult + up.get("custom:BlockMultiplier", 1)
    e = _mean(D6)
    return {"Block": e * mult}, {"Block": e * umult}, f"1d6 期望 {e} × 倍率 {mult:g}"


def devils_dice(v, up):
    e = _mean(D6)
    return ({"custom:EnemyTempStrengthDown": e}, {"custom:EnemyTempStrengthDown": e},
            f"1d6 期望 {e} 點敵人暫時減力量（升級的額外效果未計）")


def ascension_elevator(v, up):
    def ev(base_mult):
        elev_mult = v.get("custom:ElevatorMultiplier", 3)
        dbl_elev = v.get("custom:DoubleElevatorDamage", 36)
        dbl_eel = v.get("custom:DoubleEelDamage", 10)
        total = 0.0
        for a, b in product(D6, D6):
            if a == 6 and b == 6:
                total += dbl_eel
            elif a == 5 and b == 5:
                total += dbl_elev
            else:
                num = (a if a <= 4 else 0) + (b if b <= 4 else 0)
                dmg = num * base_mult
                if 5 in (a, b):
                    dmg *= elev_mult
                total += dmg
        return total / 36

    base = v.get("custom:BaseMultiplier", 2)
    ubase = base + up.get("custom:BaseMultiplier", 1)
    d, ud = ev(base), ev(ubase)
    return {"Damage": d}, {"Damage": ud}, f"2d6 列舉期望傷害 {d:.1f}（升級 {ud:.1f}）"


def give_hanna_a_beer(v, up):
    low = 3 * _mean(D6)          # 3d6
    high = 3 * _mean(range(4, 7))  # 魔女化 > 50：3d(4~6)
    p_high, p_high_up = 0.3, 0.45  # 暫定：魔女化超過門檻（50／升級 30）的機率
    d = low * (1 - p_high) + high * p_high
    ud = low * (1 - p_high_up) + high * p_high_up
    return ({"Damage": d}, {"Damage": ud},
            f"3d6={low} / 高骰={high}，假設 {p_high:.0%} 機率達魔女化門檻（升級 {p_high_up:.0%}）")


def risk_dice(v, up, uses_per_fight=3, faces=20):
    def ev(dmg, self_mult):
        # 狀態：(好運率, 自上次不幸後的使用次數, 機率)
        states = [(95, 0, 1.0)]
        e_dmg = e_self = 0.0
        for _ in range(uses_per_fight):
            nxt = []
            for good, used, p in states:
                used += 1
                p_good = int(good * faces / 100) / faces
                e_dmg += p * p_good * dmg
                e_self += p * (1 - p_good) * used * self_mult
                nxt.append((max(0, good - 5), used, p * p_good))
                nxt.append((95, 0, p * (1 - p_good)))
            states = nxt
        return e_dmg / uses_per_fight, e_self / uses_per_fight

    dmg = v.get("Damage", 3)
    sm = v.get("custom:SelfDamageMultiplier", 3)
    d, s = ev(dmg, sm)
    ud, us = ev(dmg + up.get("Damage", 1), sm + up.get("custom:SelfDamageMultiplier", 1))
    return ({"Damage": d, "HpLoss": s}, {"Damage": ud, "HpLoss": us},
            f"d20 模擬每場 {uses_per_fight} 次：平均傷害 {d:.2f}、自傷 {s:.2f}")


def future_of_potions(v, up):
    dmg = v.get("Damage", 20)
    udmg = dmg + up.get("Damage", 10)
    wrong = 0.05  # 沒有梅露露時拿錯藥水的機率
    heal = 0.2   # 回復造成傷害的 20%

    def ev(d):
        return {"Damage": d, "Heal": (1 - wrong) * d * heal, "HpLoss": wrong * d}
    return ev(dmg), ev(udmg), "5% 拿錯藥水自傷、否則回復 20% 傷害（假設沒有梅露露）"


def palette(v, up):
    b = v.get("Block", 4)
    return ({"Block": b, "custom:PaintOrb": 1}, {"Block": b + up.get("Block", 2), "custom:PaintOrb": 1},
            "權重表隨機 1 顆顏料球（以顏料球平均價值計）")


def palette_gap(v, up):
    b, r = v.get("Block", 4), v.get("Repeat", 1)
    return ({"Block": b, "custom:PaintOrb": r},
            {"Block": b + up.get("Block", 0), "custom:PaintOrb": r + up.get("Repeat", 0)},
            "權重表隨機顏料球（以顏料球平均價值計）")


def confusion(v, up):
    r = v.get("Repeat", 2)
    return ({"custom:PaintOrb": r}, {"custom:PaintOrb": r + up.get("Repeat", 0)}, "隨機顏料球（以平均價值計）")


def drug_dealer(v, up):
    return ({"custom:Potion": 1}, {"custom:Potion": 1, "custom:Catalyst": 0.5},
            "隨機基礎藥水 1 瓶（單人）；升級 50% 催化劑")


def collect_potions(v, up):
    n = v.get("Cards", 2)
    return ({"custom:Potion": n}, {"custom:Potion": n + up.get("Cards", 0)}, "隨機基礎藥水（以平均價值計）")


def three_sixty_no_scope(v, up):
    keep = {k: x for k, x in v.items()}
    ukeep = {k: x + up.get(k, 0) for k, x in v.items()}
    return keep, ukeep, "0.1% 秒殺，影響可忽略"


# ---------- 第二批：隨機生成卡牌 ----------
# gen:<Hand|Draw|Discard>:<spec> 由主程式依「被生成卡」的價值解析，見 card_score.make_gen_resolver。

def _keep(v, up, extra=None, uextra=None):
    b = dict(v)
    u = {k: x + up.get(k, 0) for k, x in v.items()}
    for k, x in up.items():
        u.setdefault(k, x)
    b.update(extra or {})
    u.update(uextra if uextra is not None else (extra or {}))
    return b, u


def linear(v, up):
    b, u = _keep(v, up, {"gen:Hand:filter:kw=SwordTechnique,notoken": 1})
    return b, u, "隨機 1 張劍技卡到手牌（劍技池平均）"


def stardust_mirage(v, up, sword_ratio=0.3):
    d = v.get("Damage", 2)
    hits = 5 * sword_ratio * 3
    return ({"Damage": d * hits}, {"Damage": (d + up.get("Damage", 1)) * hits},
            f"翻開 5 張，假設 {sword_ratio:.0%} 為劍技，每張 3 段")


def dream_interpretation(v, up):
    k = "gen:Hand:best3:filter:tag=Tarot,exclude=TheWorld"
    return {k: 1}, {k: 1}, "隨機 3 張塔羅 3 選 1（取最佳期望）"


def color_blast(v, up, orbs=3):
    return ({"custom:PaintOrbAny": orbs * 2}, {"custom:PaintOrbAny": orbs * 2},
            f"假設場上 {orbs} 顆：激發 {orbs} 顆＋重新生成 {orbs} 顆")


def zuma(v, up):
    n = v.get("Cards", 7)
    b, u = _keep({k: x for k, x in v.items() if k != "Cards"}, {})
    b["gen:Draw:cls:PaletteGap"] = n
    u["gen:Draw:cls:PaletteGap"] = n
    return b, u, f"{n:g} 張縫隙調色洗入抽牌堆（祖瑪能力另計）"


def emergency_block(v, up):
    b, u = _keep(v, up, {"custom:Retrieve": 1})
    return b, u, "棄牌堆隨機 1 張攻擊回手"


def exchange(v, up):
    b, u = _keep(v, up, {"gen:Hand:generic": 1})
    return b, u, "複製牌組隨機 1 張（以一般生成卡計）"


def mind_shard(v, up):
    k = "gen:Hand:best3:filter:type=Attack,norare,notoken"
    return {k: 1}, {k: 1.15}, "各角色非稀有攻擊 3 選 1（升級後再升級該卡，約 +15%）"


def movie_viewing(v, up):
    n = v.get("Cards", 2)
    k = "gen:Hand:filter:parent=MovieBase,exclude=CassetteShapedRock|SmallPaper"
    return {k: n}, {k: n + up.get("Cards", 1)}, "隨機電影牌到手牌（電影池平均）"


def question(v, up):
    b, u = _keep(v, up, {"gen:Hand:generic": 1})
    return b, u, "3 選 1（以一般生成卡計）"


def search_ema(v, up):
    k = "gen:Hand:filter:parent=SearchEvidenceCard"
    b, u = _keep(v, up, {k: 1})
    return b, u, "隨機證據（證據池平均）"


def joint_investigation(v, up):
    k = "gen:Hand:filter:parent=SearchEvidenceCard"
    n = v.get("custom:Evidence", 2)
    b = {"Cards": v.get("Cards", 2), k: n}
    u = {"Cards": v.get("Cards", 2) + up.get("Cards", 0), k: n + up.get("custom:Evidence", 0)}
    return b, u, "抽牌＋隨機證據（單人計）"


def i_want_to_play(v, up):
    b = {k: x for k, x in v.items() if k != "Cards"}
    b["gen:Draw:cls:PlayWhatGame"] = v.get("Cards", 3)
    return b, dict(b), "3 張玩什麼遊戲洗入抽牌堆（能力另計）"


def hide_and_seek(v, up):
    b, u = _keep(v, up, {"gen:Draw:cls:HideInWardrobe|FoundYou": 1})
    return b, u, "隨機躲進衣櫃／找到你了 洗入抽牌堆"


def crossbow(v, up):
    b, u = _keep(v, up, {"gen:Hand:cls:BrokenCrossbow": 1})
    return b, u, "生成壞掉的十字弩到手牌"


def ice_cube(v, up):
    n = v.get("Cards", 1)
    return ({"Strength": 2, "gen:Hand:cls:IceBall": n},
            {"Strength": 2, "gen:Hand:cls:IceBall": n + up.get("Cards", 1)}, "生成冰球＋2 力量")


def master_key(v, up):
    b, u = _keep(v, up, {"Strength": 1, "gen:Hand:cls:BrokenLock": 1})
    return b, u, "生成壞掉的鎖＋1 力量"


def miria_puppet(v, up):
    n = v.get("Cards", 2)
    return ({"custom:Transform": n}, {"custom:Transform": n * 1.3}, "變化手牌為隨機人偶（收藏能力另計）")


def noah_puppet(v, up):
    b = {"Block": v.get("Block", 7), "Weak": 1}
    u = {"Block": b["Block"] + up.get("Block", 2), "Weak": 2, "custom:RandomExhaustHand": 1}
    return b, u, "升級後隨機消耗 1 張手牌"


MODELS_GEN = {
    "Linear": linear, "StardustMirage": stardust_mirage, "DreamInterpretation": dream_interpretation,
    "ColorBlast": color_blast, "Zuma": zuma, "EmergencyBlock": emergency_block, "Exchange": exchange,
    "MindShard": mind_shard, "MovieViewing": movie_viewing, "Question": question,
    "SearchSakurabaEma": search_ema, "JointInvestigation": joint_investigation,
    "IWantToPlayAGame": i_want_to_play, "LetsPlayHideAndSeek": hide_and_seek,
    "Crossbow": crossbow, "IceCube": ice_cube, "MasterKey": master_key,
    "MiriaPuppet": miria_puppet, "NoahPuppet": noah_puppet,
}

# 隨機只影響目標／位置，或是多人限定，不影響單人期望值
RANDOM_IGNORED = {
    "Electrocution": "隨機目標", "ShotsFired": "隨機奈葉香隊友", "SeveringSpin": "隨機架式",
    "MindOverload": "隨機隊友目標", "LucidDream": "隨機隊友目標", "BerserkerSoul": "隨機目標",
    "JudgeJogasakiNoah": "多人：隨機隊友", "NovelCreation": "需諾亞隊友，單人無生成",
    "ForgeSpear": "只隨機插入位置", "TrialStart": "多人限定", "NanokaPuzzle": "只隨機插入位置",
    "Kokoro": "只隨機插入位置（能力另計）", "ShowYouTheUnseen": "只隨機插入位置（X 費另計）",
    "FingerSupportLine": "X 費另計", "ExhaustiveMethod": "非隨機", "RequestRetrial": "非隨機",
}

MODELS = {
    "AngelsDice": angels_dice,
    "DevilsDice": devils_dice,
    "AscensionElevator": ascension_elevator,
    "GiveHannaABeer": give_hanna_a_beer,
    "RiskDice": risk_dice,
    "FutureOfPotions": future_of_potions,
    "Palette": palette,
    "PaletteGap": palette_gap,
    "Confusion": confusion,
    "DrugDealer": drug_dealer,
    "CollectPotions": collect_potions,
    "ThreeSixtyNoScope": three_sixty_no_scope,
    **MODELS_GEN,
}
