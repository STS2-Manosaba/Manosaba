import json, os
base = r'e:\slay_the_spire\Manosaba\tools\card_score'
v2 = json.load(open(os.path.join(base, 'agent_estimates_v2', 'SakurabaEma.json'), encoding='utf-8'))
v3a = json.load(open(os.path.join(base, 'agent_estimates_v3_ally', 'SakurabaEma.json'), encoding='utf-8'))['cards']
v3h = json.load(open(os.path.join(base, 'agent_estimates_v3_heal', 'SakurabaEma.json'), encoding='utf-8'))['cards']

def C(v, vu, vm, vmu, conf, why):
    return dict(value=v, value_upgraded=vu, value_multi=vm, value_multi_upgraded=vmu, confidence=conf, reasoning="v5模擬:" + why)

K = "（進度≥10於打出時55%、整場達10為90%；≥20打出時20%、整場49%）"
cards = {
 "PresentEvidence": C(1.9, 2.0, 1.9, 2.0, "medium", "提出證據每次平均1.96（圖鑑空7.4%；有證言時幾乎都選證言=抽1+罪證2，罪證每層0.8）；升級+1議論，但議論達10僅2.6%場次，約+0.1"),
 "Question": C(3.0, 3.5, 3.0, 3.5, "medium", "抽1＋自動打出證言（抽1+罪證2≈2.1，艾瑪/守衛證言額外≈+0.15）＋證言留在圖鑑使之後提出證據可重複選它（約+0.3）＋裁判進度+1；升級三選一可挑守衛（力量2）/艾瑪（發言），約+0.5"),
 "EnoughRedTape": C(4.1, 4.1, 4.1, 4.1, "medium", "0費3次提出證據；打出時平均回合2、圖鑑僅2.8張（空8%），每次約1.5→4.1；升級去消耗不改單次價值"),
 "RawTellOwk": C(2.2, 2.7, 2.2, 2.7, "medium", "牌組內第二張生效時+10進度：C型牌組打出時進度≥10比例80%（無此卡A/B約40~48%），約+0.35就緒率×每場約4張進度效果牌×每次就緒加成≈1.5；升級固有提早約+0.5"),
 "Rebuttal": C(5.0, 6.3, 5.0, 6.3, "medium", "9/12傷(1.5/2.0)+提出證據1.9；就緒時+2/+3傷×圖鑑張數，E[張數×就緒]=4.36→+1.45/+2.2；罪證加成屬環境不計" + K),
 "Sophistry": C(1.85, 2.45, 1.85, 2.45, "medium", "9/12傷；打兩次需就緒且自身有嫌疑：B型44%，A/C約6~9%，加權24%→+0.36/+0.48（v3按10%）"),
 "ExhaustiveMethod": C(4.7, 7.7, 4.7, 7.7, "medium", "20/30傷(3.33/5.0)；就緒時+進度×1/2傷，E[進度×就緒]≈8.2（2費牌較晚打）→+1.37/+2.73"),
 "SuspiciousIWasntInvited": C(3.15, 4.4, 3.65, 4.9, "medium", "單人對自己：10/13格擋+抽1−嫌疑1/2；就緒55%時加 3/4×嫌疑（含本次+1/+2，E≈1.15/1.7）格擋→+0.69/+1.36；多人沿用v3+0.5"),
 "WitchHammer": C(4.6, 5.45, 4.6, 5.45, "medium", "13/18傷+提出證據1.9；就緒時依目標減益數各+13傷，E[減益數×就緒]=0.28→+0.6（模型減益只計易傷/虛弱/灼燒/流血，缺魔女化）"),
 "AccUhEartsIndictment": C(4.3, 5.3, 4.3, 5.3, "medium", "5/7傷×段數+提出證據1.9；段數=就緒時min(5,1+圖鑑)，加權期望2.96段（C型4.1段）→2.47/3.45"),
 "AccUhEartsRevelation": C(4.7, 5.8, 4.7, 5.8, "low", "同起訴版但上限10段，期望約3.4段；13場後才轉化，時點偏後期圖鑑更大，未單獨模擬"),
 "DArime": C(0.45, 0.75, 0.45, 0.75, "medium", "需進度≥20：打出時20%、整場49%；升級保留可等到20；連抽裁判牌期望約0.7段×13傷並消耗之"),
 "InformationNetwork": C(5.0, 5.0, 5.0, 5.0, "medium", "每次提出證據隨機敵6傷；每場提出證據11.2次，第1輪打出後剩約60%≈6.5次×6傷×0.85溢傷折扣；升級僅降費"),
 "JudgmentHour": C(5.0, 6.2, 5.0, 6.2, "low", "三選一裁判牌≈2.5（可抽到反駁約5）；抽牌堆5/7張附加裁判：每張後續打出+1進度，附加裁判的攻擊吃罪證（打出時平均罪證≈18.6，折半計）；未直接模擬"),
 "LaVIBavellabion": C(2.6, 2.6, 2.6, 2.6, "medium", "每回合罪證+1、議論+1：剩約4回合，罪證每層邊際值隨時間遞減平均≈0.5→2.0；議論+0.4；裁判攻擊吃力量（力量來源少）+0.2；升級降費"),
 "SearchSakurabaEma": C(1.2, 1.7, 1.3, 1.8, "medium", "抽1/2+隨機證物：證物平均單次值0.56，但圖鑑已有證言時很少被選，邊際≈0.7（含反駁/控訴的圖鑑張數加成）"),
 "EvidenceStrike": C(4.15, 4.65, 4.15, 4.65, "medium", "6/9傷+提出證據1.9+活體書頁1.3"),
 "EndlessEvidence": C(2.8, 3.3, 3.3, 3.8, "medium", "每回合隨機證物×剩約4回合×邊際0.7≈2.8；升級固有+1回合；多人沿用v2差額"),
 "JointInvestigation": C(2.2, 2.7, 6.7, 8.0, "medium", "抽2/3+2證物（邊際0.6×2）；多人沿用v3（2.5人隊，隊友1.5名）結構，只下修自身證物部分"),
 "TraumaSakurabaEma": C(0.5, 0.75, 0.5, 0.75, "medium", "你做得很好層數<5時才是此版本（前4場），魔女化倍率均≈70%→7/10.5層×0.07"),
 "TraumaTrueSakurabaEma": C(1.7, 2.55, 1.7, 2.55, "medium", "層數≥5後轉真版；擊殺約1.5隻/場，第9場起層數13，倍率均≈170%→34/51層；魔女化≥100於43%場次出現，飽和後每層取0.05"),
 "Fever": C(1.4, 2.1, 1.4, 2.1, "medium", "20/30魔女化×全程平均倍率≈150%→30/45層×0.05，扣自身虛弱約0.1"),
 "MagicToKillTheWitch": C(2.3, 2.6, 2.3, 2.3 + 0.3, "low", "層數≥5自動入牌組；全體虛弱1+魔女化/25；魔女化≥100於43%場次出現（v2按20%），持牌等待可觸發特別的魔法；特別的魔法效果未模擬"),
 "RequestRetrial": C(0.8, 1.0, 0.8, 1.0, "medium", "需10嫌疑：模擬中嫌疑來源（證言/證物/可疑）每次+1，打出時嫌疑平均<1.5，≥10幾乎不發生；需專門堆嫌疑牌組"),
 "InformationSharing": C(None, None, 6.8, 8.2, "medium", "多人限定；就緒55%（v2按30%）時雙方各得1/2張虛無提出證據，+0.5/+0.8"),
}
# 無法模擬：保留前版數值
for name, src in [("SillyLittleDog", v3a), ("ValentineGift", v2['cards']), ("Gluttony", v2['cards']),
                  ("PsychicPhotograph", v2['cards']), ("WitchIslandTreasureMap", v2['cards'])]:
    o = src[name]
    cards[name] = C(o['value'], o['value_upgraded'], o['value_multi'], o['value_multi_upgraded'], o['confidence'],
                    "模擬未涵蓋此卡主要機制（%s），沿用前版數值" % {
                        "SillyLittleDog": "受傷減免／希羅艾瑪狗；保留v3_ally特定隊友0.088", "ValentineGift": "流血門檻爆發",
                        "Gluttony": "每回合消耗手牌換屬性", "PsychicPhotograph": "魔女化100與結束回合的取捨",
                        "WitchIslandTreasureMap": "壞結局技能"}[name])
cards["MagicToKillTheWitch"]["value_multi_upgraded"] = 2.6

assumptions = [
    "單人、蒙地卡羅 3 種牌組原型（A早期16張 0.3／B裁判22張 0.4／C證物+牌組內RawTellOwk 22張 0.3）各3萬場，每回合抽5、能量3，貪婪出牌（0費與RawTellOwk優先、其餘按價值/費用）",
    "戰鬥：一般/菁英/首領回合數依 turn_model.py 的 Gamma 抽樣，敵人血量=回合數×24/30/36×幕數係數(1/1.6/2.2)，可提早擊殺（平均約4.3/5.7/7.2回合）；重要性權重1/2/3",
    "腿環等級按一局3幕×(6一般+2菁英+1首領)經驗推進：Lv1~2僅前6場，多數場次Lv3~4，最後5場Lv5；搜查需打8/6/4/2張（自動打出也計），Lv3起審問開始給反駁，Lv5開場給RawTellOwk並罪證加成×2",
    "你做得很好：每場約擊殺1.5隻非爪牙，第5場起≥5層（創傷轉真版、殺死魔女的魔法入牌組），第9場起13層；魔女化倍率=(50+10×層數)%",
    "裁判進度：審問開始後每張裁判牌（含提出證據、證言不是）+1；打出當下≥10的比例55%（A40%/B48%/C80%），整場達10為90%；≥20 打出時20%、整場49%；審問開始回合1為88%",
    "證物進手即收入圖鑑，提出證據自動打出最佳一張並放回；證言(抽1+罪證2)留在圖鑑可被重複提出，使罪證每場累積到約25層；每層罪證後續平均吃到7.8段裁判攻擊（含Lv5×2）→每層約0.8能量（含溢傷折扣）",
    "裁判攻擊本身的罪證加成視為環境（同力量不計入攻擊卡），只計入罪證來源卡；多人數值沿用 v3_ally（2.5人隊、特定隊友0.088）與 v3_heal（回血0.28/點）",
]
mech = dict(v2['mechanic_values'])
mech.update({
    "提出證據": "模擬每次≈1.96（圖鑑空7.4%）",
    "證物卡": "隨機證物單次平均0.56；加入圖鑑的邊際≈0.7",
    "證言": "抽1+罪證2≈2.1，可重複提出",
    "罪證": "每層≈0.8（後續7.8段裁判攻擊×溢傷折扣）",
    "裁判進度": "打出時≥10 55%／≥20 20%；整場≥10 90%／≥20 49%",
    "議論": "達10層僅2.6%場次，每層≈0.1",
    "魔女化": "倍率隨你做得很好層數，前4場≈70%，之後≈150~180%；≥100於43%場次出現",
    "每場提出證據次數": "11.2",
})
out = dict(character=v2['character'], source=v2['source'], assumptions=assumptions, mechanic_values=mech, cards=cards)
d = os.path.join(base, 'agent_estimates_v5_sim'); os.makedirs(d, exist_ok=True)
with open(os.path.join(d, 'SakurabaEma.json'), 'w', encoding='utf-8', newline='\n') as f:
    json.dump(out, f, ensure_ascii=False, indent=2)
# 對照表
for n, c in cards.items():
    old = v3a.get(n) or v3h.get(n) or v2['cards'].get(n)
    print(f"{n}: {old['value']}/{old['value_upgraded']}/{old['value_multi']}/{old['value_multi_upgraded']} {old['confidence']} -> {c['value']}/{c['value_upgraded']}/{c['value_multi']}/{c['value_multi_upgraded']} {c['confidence']}")
