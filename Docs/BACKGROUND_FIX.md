# 原预制体位置、镜头与底部绿块修复

## 原因

之前的修补有两处问题：

1. `LifeCourseGenerator` 将相机高度强制设成 `GroundTop + orthographicSize * 0.4`。相机向下移动，让关卡看起来整体向上偏移；这并不是原 Chunk 资源的内部坐标被一起改动。
2. `JourneyBackgroundAlignment` 在原背景下面生成了 `Ground continuation` 网格，用单个绿色像素填充，所以底部出现一整块绿。

## 当前修改

- 删除补色网格生成逻辑，不再添加绿块、重复风景或不透明底栏。
- 移除强制将地面放到屏幕 30% 高度的相机公式。
- 只读取实际 Chunk 背景的渲染边界，将镜头底边贴在原图底边内侧 0.01 个世界单位。镜头可视范围高于原图时才缩小 orthographicSize，避免露出背景之外的空白。
- 原预制体资源、背景子节点、道具、楼梯、碰撞体均保持原始坐标、旋转和缩放。Chunk 根节点继续使用 ChunkSpawner 的 Initial Spawn Position。
- 镜头取景只在关卡初始化时计算，正常奔跑仍由 CameraFollow 横向跟随，不会逐帧上下跳动。

## 验证

- `Tools/Validate-Journey.ps1`：运行时及编辑器编译、69,914 项逻辑断言。
- `Tools/Run-JourneySmoke.ps1 -RenderScreenshots`：隔离副本中验证六关，逐级比较 Chunk 子节点与源预制体的位置、旋转、缩放和子节点数量；检查镜头在原背景范围内，并检查没有补色网格。保留物理地面、事件恢复、心魔、回收和结局验证。
- 截图：`Logs/JourneyValidation/SystemQA/Logs/PrefabRestore/screenshots/stage-0.png` 至 `stage-5.png`。

Unity 中停止 Play 后重新运行，让新生成的 Chunk 和相机使用这版逻辑。

## 跳跃后持续空屏排查（尚未确认根因）

用户反馈：跳跃后角色和关卡一起消失，仅剩相机背景色与 HUD；落地后也不恢复。

当前验证结果不能说明该异常已修好：隔离副本中的短篇模式原地跳、跨 Chunk 跳，以及标准模式原地跳、二段跳、落地、跨 Chunk 跳均通过。验证还检查镜头高度和深度没有随跳跃漂移、原背景在相机视锥和渲染层内。实际截图没有出现用户的持续空屏。

验证命令：

```powershell
./Tools/Validate-Journey.ps1
./Tools/Run-JourneySmoke.ps1 -RenderScreenshots
./Tools/Run-JourneySmoke.ps1 -RenderScreenshots -StandardJourney
```

已增加仅编辑器使用的 `Assets/Editor/JourneyViewDiagnostics.cs` 自动记录器，不调整相机、角色、场景或存档。在普通编辑器 Play 中，前五次起跳会记录起跳与稍后画面，同时每 0.1 秒记录角色位置/速度、游戏状态、关卡数量、相机位置/旋转/尺寸/裁剪距离/渲染层/目标纹理、可见关卡 Sprite 数量和脚本错误。最多保留最近 100 条记录；批处理测试不启用记录器。

现场文件保存在 `Logs/JourneyValidation/LiveView/latest.txt` 和同目录的 `jump-*-takeoff.png`、`jump-*-after.png`。也可通过 `Life Journey → 诊断 → 记录当前游戏画面` 手动保存截图。

下一步需要在用户编辑器中编译完成后重新进入 Play、跳跃并保留空屏现场，从上述文件核对真实故障。未抓到现场前，不再根据单张截图推断根因或调整预制体坐标。
