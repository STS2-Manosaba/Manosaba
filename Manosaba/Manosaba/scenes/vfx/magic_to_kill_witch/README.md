# 殺死魔女的魔法：1-5 Adv09 演出

櫻羽艾瑪使用 `MagicToKillTheWitch`，且使用時的魔女化達到卡牌 `MajokaThreshold`（100）時播放。100 層以上也符合；其他角色使用同一張卡不觸發。演出在卡牌效果結算前播放並等待完成，原有虛弱、特別的魔法、衍生與結束回合流程保留。測試模式略過原生影音。

原作資料來源：

- `G:/assets_manosaba_WitchTrials/Textures/Naninovel/Backgrounds/Stills/RGB/Still_020_001.png`，4096×2048 的原始整張場景圖，未截取影片介面或字幕。
- 原作 `naninovel-scripts_assets_naninovelscripts-act01_chapter05_advbad.bundle` 中的 `Act01_Chapter05_Adv09`，第 656～671 行，`Stills.20_1`。
- 使用者提供的 `2026-10-27 13-02-00.mp4`，用於校準鏡頭切換時刻。

鏡頭參數直接取自原作：第一段縮放 3、位置 (120,30)→(120,90)，第二段縮放 3、位置 (5,45)→(5,-10)，兩段均為 1.5 秒 EaseOutQuad；第三段置中，2.4 秒從縮放 1.5 拉到 1.15，疊合前一鏡頭並於 0.6 秒內淡出，模糊於 1.8 秒退去。骨翼、光束與光點為原圖的一部分，不是新增的角色骨架動畫。Godot 模糊採取 9 點取樣，與原作後處理不保證逐像素一致。

影片對位後的三個切換點是 1.25、3.77、6.28 秒；總長 11.28 秒。最後 0.6 秒淡回目前戰鬥畫面，避免停留於原作等待閱讀的場景。以真實畫面時間播放，不受快速模式壓縮，等比例填滿螢幕。演出附屬戰鬥房間，結束或離場自動清理。

音樂 `EmA (-狂-).mp3` 的空格為 U+00A0，和場景一起起播，使用既有 `GodotSfxRouter` 的音量及離場還原。配置「殺死魔女的魔法」一起控制畫面和 BGM，預設每次，可選每場冒險每位玩家一次或不播放。新冒險重置次數。

重新轉換原始資料：

```powershell
python Tools/ExtractMagicToKillWitch.py --script-bundle "E:/SteamLibrary/steamapps/common/manosaba_game/manosaba_Data/StreamingAssets/aa/StandaloneWindows64/naninovel-scripts_assets_naninovelscripts-act01_chapter05_advbad.bundle" --image "G:/assets_manosaba_WitchTrials/Textures/Naninovel/Backgrounds/Stills/RGB/Still_020_001.png" --unitypy-path "E:/manosaba/build/unitypy"
```

執行端不需要 Python、UnityPy 或原作檔案；轉換工具只讀原作。`original_scene.json` 保留來源行號、命令及鏡頭參數。

`Tools/MagicToKillWitchVerify.gd` 驗證場景與音樂載入、三段鏡頭、模糊及交疊、反向取樣、畫面縮放、完整播放與離場清理。`-- --capture` 匯出關鍵影格，`-- --record` 匯出 30 FPS 預覽影格，`-- --pack` 驗證 `_mods_out/Manosaba.pck`。可在無 C# 的獨立 Godot 預覽專案中複製相同路徑的資源來執行。
