# Manosaba 音效與 BGM 接入流程

新增音訊檔時，除了把檔案放進專案，也要接上播放路由、設定選項與生命週期清理。完成前依照本文檢查。

## 音檔與事件路徑

- BGM 放在 `Manosaba/Manosaba/audio/bgm/`；一般音效放在對應的 `Manosaba/Manosaba/audio/` 子目錄。
- Godot 專案支援 `.ogg`、`.wav`、`.mp3`。提交或複製新檔後，確認 Godot 已建立相應的 `.import` 資料。
- 自訂音訊事件使用 `event:/Manosaba/...`。例如 `Manosaba/Manosaba/audio/bgm/inmu_king.mp3` 對應 `event:/Manosaba/audio/bgm/inmu_king.mp3`。
- `GodotSfxRouter` 會依事件路徑尋找 `res://Manosaba/...` 資源；若事件路徑省略副檔名，路由會依序嘗試 `.ogg`、`.wav`、`.mp3`。

## 播放 BGM

在卡牌或效果觸發位置沿用遊戲的 `SfxCmd.Play`，不要自行建立播放器：

```csharp
private const string BgmEventPath = "event:/Manosaba/audio/bgm/example.mp3";

SfxCmd.Play(BgmEventPath, 0.8f);
```

事件路徑以 `event:/Manosaba/audio/bgm/` 開頭時，會經 `GodotSfxRouter` 播放。路由會暫停原版 BGM，使用模組的「Manosaba SFX 音量」設定調整自訂 BGM 音量；音量設定變動時也會更新正在播放的曲目，並在曲目播完時恢復原版 BGM 音量。`0.8f` 是此播放呼叫的基礎音量倍率。

自訂 BGM 預設播放一次。若曲目需要循環，將完整事件路徑加入 `GodotSfxRouter.LoopingBgmEvents`；不要只依賴音檔本身的 loop metadata。

戰鬥結束、離開戰鬥、放棄冒險、返回主選單或遊戲結束時，由 `Patch_StopCustomBgm` 呼叫 `GodotSfxRouter.StopCustomBgmAndResumeVanilla()` 清理自訂 BGM 並恢復原版 BGM。一般卡牌 BGM 沿用這個集中清理流程。

## 播放一般音效

一般短音效也應使用 `SfxCmd.Play` 和 `event:/Manosaba/...` 事件路徑，例如 `event:/Manosaba/audio/SFX/example.mp3`。路由會將非 BGM 事件當作一次性音效播放，並套用 Manosaba 自訂音量。若是角色選擇語音、cut-in 語音或帶有特殊播放次數控制的音效，先沿用對應角色的 helper 或既有卡牌模式。

## BGM 播放頻率設定

每個由卡牌或能力觸發的新 BGM 都必須提供獨立播放頻率設定，不能因為需求沒有明講設定選項而略過。使用 `ManosabaFxPlayMode`，預設為 `EveryTime`，並提供「每次、每場冒險一次、永不播放」三種選項。只有多個效果確實應共用同一播放頻率時，才使用同一個設定，並在交付摘要說明。

依下列步驟加入設定：

1. 在 `ManosabaConfig` 新增預設為 `EveryTime` 的 `ManosabaFxPlayMode` 設定屬性，並加上 `[ConfigHoverTip]`。
2. 在四語 `Manosaba/Manosaba/localization/{eng,zhs,jpn,kor}/settings_ui.json` 新增 `<設定名稱>.title`、`.hover.title`、`.hover.desc`，說明它控制哪首 BGM，以及三種播放選項。
3. 播放前檢查 `Never` 與 `OncePerRun`，只在設定允許時呼叫 `SfxCmd.Play`。
4. 若使用 `OncePerRun`，在效果類別保存播放狀態、提供重置方法，並從 `Patch_RunState_CreateForNewRun_ResetVfx` 呼叫，讓新冒險重新計次。

音效也可沿用此模式；若只新增一般短音效，依需求決定是否提供獨立頻率設定。

例：

```csharp
ManosabaFxPlayMode playMode = ManosabaConfig.ExampleEffectFrequency;
if (playMode != ManosabaFxPlayMode.Never &&
    (playMode != ManosabaFxPlayMode.OncePerRun || !_bgmPlayedThisRun))
{
    SfxCmd.Play(BgmEventPath, 0.8f);
    if (playMode == ManosabaFxPlayMode.OncePerRun)
        _bgmPlayedThisRun = true;
}
```

全域音量設定由 `ManosabaConfig.ManosabaSfxVolumePercent` 提供。使用 `SfxCmd.Play` 播放 `event:/Manosaba/...` 音訊，才能套用自訂音量和 BGM 暫停／恢復處理。

## 循環與結束清理

- 每首新 BGM 都要確認是否循環。預設播放一次；若曲目要持續循環，將完整事件路徑加入 `GodotSfxRouter.LoopingBgmEvents`。不要只依賴音檔 loop metadata。
- `GodotSfxRouter` 負責曲目播放期間的原版 BGM 暫停、播放結束後恢復，以及 Manosaba 音量套用。`Patch_StopCustomBgm` 負責離開戰鬥、冒險、主選單或遊戲時停止自訂 BGM 並恢復原版 BGM。一般卡牌沿用這個集中清理流程。
- 若有新增音檔，確認 Godot 已產生對應 `.import` 資料，並確認事件路徑可載入該資源。

## 完成檢查表

- [ ] 音檔放在正確的 Godot 資料夾，檔名與事件路徑完全一致。
- [ ] 音檔已匯入，專案資源路徑可解析。
- [ ] 由 `SfxCmd.Play` 觸發，BGM 使用 `event:/Manosaba/audio/bgm/...` 前綴。
- [ ] 若需要循環，事件路徑已登記到 `LoopingBgmEvents`。
- [ ] 若需要單獨播放頻率控制，設定屬性、四語設定文案與新冒險重置都已完成。
- [ ] 確認模組音量設定會套用，且離開戰鬥／冒險後原版 BGM 可恢復。
