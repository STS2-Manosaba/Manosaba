# 原版錨點卡（估值共同尺標）

value 欄是「打出一次的效果價值」（能量單位，不扣費用、不計消耗等關鍵字），與你要輸出的 value 同一尺度。
這些卡的相對強度接近原版同稀有度中位數（≈1.0），代表「設計上剛好合理」的強度；另附偏強與偏弱的例子。
卡牌程式碼在 e:\slay_the_spire\decompile1118\src\Core\Models\Cards\<名稱>.cs，可對照效果。

## Basic

| 卡 | 角色 | 類型 | 費用 | value | 升級後 | 相對強度 |
|---|---|---|---|---|---|---|
| Bash | Ironclad | Attack | 2 | 2.03 | 2.72 | 0.98 |
| StrikeDefect | Defect | Attack | 1 | 1.00 | 1.50 | 0.86 |
| DefendDefect | Defect | Skill | 1 | 1.00 | 1.60 | 1.00 |
| DefendIronclad | Ironclad | Skill | 1 | 1.00 | 1.60 | 1.00 |

## Common

| 卡 | 角色 | 類型 | 費用 | value | 升級後 | 相對強度 |
|---|---|---|---|---|---|---|
| AstralPulse | Regent | Attack | 0 | 1.40 | 1.87 | 0.99 |
| CelestialMight | Regent | Attack | 2 | 3.00 | 4.00 | 0.99 |
| SculptingStrike | Necrobinder | Attack | 1 | 1.70 | 2.20 | 1.01 |
| BoostAway | Defect | Skill | 0 | 1.20 | 1.80 | 0.98 |
| Armaments | Ironclad | Skill | 1 | 1.40 | 2.00 | 0.96 |
| CosmicIndifference | Regent | Skill | 1 | 1.40 | 2.00 | 0.96 |
| GuidingStar | Regent | Attack | 1 | 3.00 | 3.70 | 1.78 |
| BallLightning | Defect | Attack | 1 | 2.92 | 3.42 | 1.73 |
| KnowThyPlace | Regent | Skill | 0 | 0.65 | 0.65 | 0.45 |
| Untouchable | Silent | Skill | 2 | 1.20 | 1.80 | 0.46 |

## Uncommon

| 卡 | 角色 | 類型 | 費用 | value | 升級後 | 相對強度 |
|---|---|---|---|---|---|---|
| Radiate | Regent | Attack | 0 | 1.60 | 2.10 | 0.99 |
| Bludgeon | Ironclad | Attack | 3 | 5.33 | 7.00 | 1.02 |
| Spite | Ironclad | Attack | 0 | 1.67 | 2.50 | 1.03 |
| Quasar | Regent | Skill | 0 | 1.40 | 1.80 | 1.00 |
| Prophesize | Regent | Skill | 2 | 3.00 | 4.50 | 1.00 |
| LegionOfBone | Necrobinder | Skill | 2 | 3.00 | 3.80 | 1.00 |
| InfiniteBlades | Silent | Power | 1 | 2.40 | 2.40 | 1.00 |
| Inflame | Ironclad | Power | 1 | 2.40 | 3.60 | 1.00 |
| Loop | Defect | Power | 1 | 2.50 | 5.00 | 1.04 |
| EnergySurge | Defect | Skill | 1 | 8.00 | 12.00 | 4.08 |
| Devastate | Regent | Attack | 1 | 5.83 | 7.50 | 3.03 |
| Reflex | Silent | Skill | 3 | 1.00 | 1.50 | 0.22 |
| Tactician | Silent | Skill | 3 | 1.00 | 2.00 | 0.22 |

## Rare

| 卡 | 角色 | 類型 | 費用 | value | 升級後 | 相對強度 |
|---|---|---|---|---|---|---|
| Hang | Necrobinder | Attack | 1 | 2.00 | 2.80 | 1.00 |
| Squeeze | Necrobinder | Attack | 3 | 5.60 | 6.80 | 1.04 |
| FiendFire | Ironclad | Attack | 2 | 4.50 | 5.50 | 1.06 |
| Undeath | Necrobinder | Skill | 0 | 1.40 | 1.80 | 0.96 |
| Burst | Silent | Skill | 1 | 1.80 | 2.60 | 1.04 |
| Flanking | Silent | Skill | 2 | 3.50 | 4.00 | 1.12 |
| Pyre | Ironclad | Power | 2 | 4.50 | 5.00 | 1.00 |
| ToolsOfTheTrade | Silent | Power | 1 | 2.50 | 2.50 | 1.00 |
| Demesne | Necrobinder | Power | 3 | 8.00 | 8.00 | 1.01 |
| Comet | Regent | Attack | 0 | 7.30 | 9.10 | 4.35 |
| SevenStars | Regent | Attack | 2 | 11.44 | 11.44 | 3.18 |
| Oblivion | Necrobinder | Skill | 0 | 0.45 | 0.60 | 0.31 |
| NotYet | Ironclad | Skill | 2 | 1.20 | 1.56 | 0.33 |

## 校準結論（請在估值時留意）

- 類型係數（腳本會自動套用，**不要**自己再修正）：{'Attack': 1.0, 'Skill': 1.15, 'Power': 0.8}
- 原因：以同一套方法估原版時，技能牌的過牌、檢索、保留等操作價值常被低估，能力牌的整場價值常被高估。
- 估值時請照實估計，只要和上方錨點卡保持一致的尺度即可。
