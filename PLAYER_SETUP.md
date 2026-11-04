# 玩家素材匯入方式

這個公開儲存庫只保存玩家控制、預製物件、動畫控制器與設定工具。`Bodyguards` 模型及 Mixamo 動畫的原始檔由每位組員自行取得，不放入公開 GitHub。

1. 用 Unity **2021.3.5f1** 開啟專案。在 Unity Asset Store 取得免費的 [Bodyguards](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/bodyguards-31711)，從 Package Manager 匯入專案。匯入後應有 `Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx`。
2. 用 Adobe 帳號登入 [Mixamo](https://www.mixamo.com/)。以 **X Bot** 下載以下三段動畫，格式均選 **FBX for Unity**、**Without Skin**、**30 FPS**：

   | Mixamo 動畫 | 下載時設定 | 存入專案的檔名 |
   | --- | --- | --- |
   | Unarmed Idle | 預設 | `Assets/Player/Animations/UnarmedIdle.fbx` |
   | Unarmed Run Forward | 勾選 In Place | `Assets/Player/Animations/UnarmedRun.fbx` |
   | Unarmed Jump | 勾選 In Place | `Assets/Player/Animations/UnarmedJump.fbx` |

3. 回到 Unity，等資源匯入完成後，選 **Tools → HW2 → Rebind Player Assets**。這會將三段動畫重新連到控制器，並把 Bodyguards 材質轉為專案使用的 URP 材質。
4. 開啟 `Assets/Scenes/Stage 1.unity` 測試：**WASD** 移動、**滑鼠**轉鏡頭、**空白鍵**跳躍。玩家會依狀態播放待機、跑步、跳躍動畫。

如果在完成第 3 步之前看到缺少模型或動畫的提示，先確認上述四個素材檔案都已匯入到指定路徑，再執行設定工具。不要把 `Assets/BodyGuards` 或三個動畫 FBX 加進公開 Git 提交。

## 進階功能分支

`advanced` 分支包含彈藥管理、場景淡入淡出、存讀檔、可破壞補給木箱，以及原創投擲與補充彈藥骨架動作。完成上述素材匯入及 Rebind 後即可使用，不需要另外下載投擲或換彈動畫。左鍵投擲、R 補充彈藥、F5 存檔、Esc 暫停；三關移動速度均為 6。完整展示流程見 [ADVANCED_FEATURES.md](ADVANCED_FEATURES.md)。

敵人的 Goblins 素材也需依組員原本的方式各自匯入。原創動作程式可以公開推送，Bodyguards、Mixamo 與 Goblins 的原始素材仍不包含在此分支。
