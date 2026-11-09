# 人生跑酷：心魔、脚本结构与随机关卡升级

> 当前已按要求恢复原始 Chunk 预制体关卡方式：六场景的 Difficulty Levels、Chunks 和生成参数已从原始压缩包还原，Inspector 可直接编辑，运行时实际使用这些预制体。人生事件选卡和独立心魔保留。操作见 `Assets/Documention/原始预制体关卡配置说明.md`。下文关于“清空列表”和程序生成地形的描述属于之前版本；现在程序地形仅在手动关闭 Use Prefab Chunks 后使用，迁移工具不再清空列表。

打开 `Assets/Scenes/GameStart.unity` 运行。六个人生关卡继续使用原场景、角色、背景和图片。新系统自动初始化，无需把心魔拖到 Chunk 或对象池列表里。

## 本次解决的问题

原工程同时保留旧版 `Boss / Bossapper / BossHealth`、分块预制体中的 Boss 触发器、新版心魔控制器，以及路线规划器里的心魔攻击生成逻辑。同一种心魔因此分散在关卡、触发器、生成器、会话和战斗脚本中。

本次把三个系统分开：

1. **人生流程**：`LifeJourneySession` 管理阶段、暂停、死亡、转场和保存。
2. **心魔遭遇**：`JourneyEncounterDirector` 管理出场、战斗或同行、解决和退场；角色与攻击独立于关卡片段。
3. **关卡生成**：`JourneyRoutePlan` 生成带种子的普通地形方案，`LifeCourseGenerator` 流式搭建，`JourneyObstacleSpawner` 只复用普通障碍。

六个正式关卡的旧 `difficultyLevels` 列表已清空；原关卡图片引用先迁移到阶段配置，再解除旧分块引用。原预制体资源保留。正式流程会隔离旧 Boss、旧攻击脚本和旧计时管理器，防止两套逻辑同时运行。

## 心魔系统

### 生命周期与层级

```text
LifeJourneySession（跨场景）
  └─ 保存流程状态，通过 Encounter 引用当前场景的遭遇

Journey Encounters（当前场景根对象）
  ├─ JourneyEncounterDirector
  ├─ Mind Demon（JourneyBossController + SpriteRenderer）
  └─ Telegraphed Attack（JourneyDemonAttack + 独立碰撞体 + 地面预警）

关卡入口（原 ChunkSpawner 所在对象）
  ├─ LifeCourseGenerator
  ├─ JourneyObstacleSpawner
  ├─ Journey segment ...
  └─ Inactive obstacle pool
```

遭遇状态：`Dormant → Approaching → Active → Resolved`。

战斗角色状态：`Arriving → Recovering → Telegraph → Attacking → Recovering ... → Departing`。

出场期间不能攻击心魔；正式交战后可按住 J 射击。攻击具有冷却和飞行过程。每次心魔出招先显示固定在世界坐标中的地面标记，经过预警时间才启用伤害碰撞体，攻击结束立即禁用并回收。暂停会冻结阶段计时与攻击计时；角色死亡、心魔解决或场景结束时清理攻击。

四种战斗心魔保留半血第二阶段，缩短喘息时间，并改变部分攻击范围或数量。路线在心魔区间及其前后缓冲段保留平地；未解决心魔时角色停在战斗区边缘，解决后继续前进。边缘攻击会留在前方，保证停下等待仍然有效。

| 年龄 | 心魔 | 主要表现 | 解决方式 |
| --- | --- | --- | --- |
| 婴儿 | 无 | 学习爬行和等待 | 走完路程 |
| 童年 | 恐惧 | 前方封路预警 | 攻击并避开危险窗口 |
| 少年 | 焦虑 | 标记落点后落物；第二阶段双落点 | 攻击并观察落点 |
| 青年 | 迷茫 | 在已标记的横向范围内移动 | 攻击并看清移动范围 |
| 中年 | 压力 | 交替压向脚下或前方，第二阶段更宽 | 脚下预警时前进，封路时等待 |
| 老年 | 死亡 | 身影由远及近，最后并肩同行 | 走到同行区间末尾 |

### 老年心魔“死亡”

老年在 45% 路程遇见死亡，身影慢慢靠近；到 90% 路程记录接受，随后继续陪伴到结束。

- 无血条、无攻击按钮，J 不会生成子弹。
- 不生成伤害攻击，也不阻止角色继续走。
- 同行开始前一段起，后续路线保持平地。
- 无法通过提前调用战斗胜利入口跳过同行。
- 文案：“你依然舍不得。但这一次，你没有再躲开。”

这段遭遇和四场战斗共用同一套出场、存档和场景生命周期，但使用不同的解决条件。

### 调整心魔

打开 `Assets/Resources/JourneyDemonSettings.asset`：

| 字段 | 用途 |
| --- | --- |
| `demon / title` | 类型和显示名称；同类型只配置一条 |
| `sprite / tint` | 独立造型与颜色；图片为空时使用总配置中的现有图片 |
| `startProgress` | 出场路程比例，默认 0.45 |
| `arenaEndProgress` | 战斗边界或同行解决位置；战斗默认 0.7，死亡默认 0.9 |
| `arrivalSeconds` | 渐入时长，默认 2 秒 |
| `warningSeconds` | 攻击预警，默认 2 秒 |
| `activeSeconds` | 单次攻击生效时间，默认 1 秒 |
| `recoverySeconds` | 常态喘息，默认 3 秒 |
| `secondPhaseRecovery` | 第二阶段喘息，默认 2 秒 |
| `introduction / resolution` | 遭遇和解决文案 |

各年龄血量仍在 `LifeJourneySettings.asset → stages → bossHealth`；玩家攻击冷却由 `attackCooldown` 控制。当前仍复用现有图片，并以颜色区分心魔，没有生成新美术。为各条目指定 `sprite` 即可替换造型，**不要再把旧 Boss 预制体放入关卡列表**。

新增攻击模式集中在 `JourneyDemonPatterns.Build`；攻击的时间和碰撞生命周期由 `JourneyDemonAttack` 统一执行。路线生成器不再包含“某种心魔生成某种攻击”的分支。

## 随机障碍生成

### 随机与重试规则

开始一段新人生时生成并保存 `routeSeed`。每段使用“存档种子 + 年龄 + 片段索引”的独立随机流，因此：

- 不同人生的障碍类型、位置、台阶高度和沟宽会变化。
- 同一存档死亡重试、退出续玩、分段提前加载或回收，都得到相同路线。
- 不依赖 `UnityEngine.Random` 的全局状态，不会因其他系统调用随机数而改变路线。
- 每段最多一个普通障碍；起终点、检查点、回忆点和战斗区有安全保护。
- 婴儿与老年只允许平地或可等待的封路障碍；低顶必须拥有俯身能力。
- 沟宽按普通速度的单跳距离限制，不强制冲刺或二段跳。

地形片段在玩家前方生成、在后方销毁；普通实体障碍按类型复用。沟壑通过切开地面形成，不进入实体对象池。心魔与心魔攻击完全不进入普通障碍池。当前心魔单次最多生成两处攻击，使用独立创建、禁用、销毁流程。

### 调整权重和密度

打开 `Assets/Resources/JourneyGenerationSettings.asset`：

| 字段 | 用途 |
| --- | --- |
| `startDensity / endDensity` | 起止障碍概率，随路程插值，默认 0.35 → 0.7；安全段优先保持为空 |
| `recoverySeconds` | 按速度把障碍位置向片段中央收拢；中心范围限制在片段的 35%～65%，不是任意秒数的硬性保证 |
| `rules[].obstacle` | 普通障碍：Step、Gap、Gate、Ceiling |
| `weight` | 在当前可用规则中加权抽取；0 表示不参与 |
| `stageMask` | 六个年龄的位掩码，见下表 |
| `fromProgress / untilProgress` | 该规则参与抽取的路程比例 |
| `sprite` | 该类型的可选图片；为空时沿用当前阶段图片 |

| 年龄 | 掩码值 |
| --- | --- |
| 婴儿 | 1 |
| 童年 | 2 |
| 少年 | 4 |
| 青年 | 8 |
| 中年 | 16 |
| 老年 | 32 |

例如童年至中年使用 `2+4+8+16=30`，全部年龄使用 `63`。默认权重为台阶 3、沟壑 2、封路 2、低顶 1。年龄能力过滤始终优先于权重；没有可用规则时回退到安全平地。

`Falling / Moving / Pressure` 是心魔攻击类型，不能配置到普通随机障碍中。使用 Unity 菜单 **Life Journey → Validate Campaign** 检查配置。

## 项目脚本整理

全部 51 个原项目脚本已按职责迁移并保留原 `.meta` / GUID；当前共有 61 个运行时与兼容脚本、5 个编辑器脚本。66 个项目 C# 文件统一使用 Roslyn 格式化，并检查格式化前后语法等价。第三方 TextMesh Pro 示例代码保持原样。

```text
Assets/Script/
  AnswerSystem/Answer/ 原答题 .asset 数据（保留资源路径，无 C# 脚本）
  Item/Inventory/     原物品与 Buff .asset 数据（保留资源路径，无 C# 脚本）
  Runtime/
    Core/           人生流程、阶段能力、总配置
    Encounters/     心魔配置、遭遇管理、角色、模式、攻击与碰撞转发
    Generation/     场景入口、路线规划、地形生成、普通障碍池与封路计时
    Player/         玩家输入和移动
    Combat/         玩家子弹
    Save/           存档
    Collectibles/   回忆、金币、速度拾取
    Items/          背包、拾取和物品数据
    Presentation/   镜头、背景和死亡表现
    Audio/          声音管理与碰撞音效
    UI/             正式流程界面和触屏输入
  Legacy/
    Flow/           原流程入口与正式流程隔离适配
    Combat/         原 Boss、能量和攻击兼容脚本
    Level/          原演示关卡行为和旧 Chunk 流式支持
    UI/             原按钮、答题、血条和暂停兼容入口
    Collectibles/   原跟随式拾取表现
    Data/           原答题数据
Assets/Editor/      配置验证、迁移、关卡入口 Inspector、原批量工具
```

逐文件索引见 [Docs/SCRIPT_INDEX.md](Docs/SCRIPT_INDEX.md)。含空格的脚本文件名已与原类名对齐，例如 `Boss apper.cs → Bossapper.cs`；旧类名和序列化按钮方法保留，避免迁移后出现 Missing Script 或按钮失效。

整理时同时修复：背包 Buff 查找被第一个无关槽位提前中止、拾取重复触发和空文本引用、对象池初始姿态在首次启用之后才记录、跟随拾取缺失锚点后无法再拾取，以及声音管理器缺少 AudioSource 和单例销毁清理的问题。

旧演示场景仍使用 `LegacyChunkStreamer`，并排除含旧 Boss 的分块。若某个旧难度组只有 Boss 分块，会停止生成并给出提示；正式六关不受该兼容路径影响。

## 存档与迁移

继续使用原存档键与版本，新增 `routeSeed` 字段；旧存档缺少此字段时按种子 0 读取。原阶段进度、回忆、死亡次数和四个心魔胜利位继续保留。由于路线算法升级，旧存档首次进入时地形会按新规则重建；之后同一存档重试保持一致。

`demonMask` 表示“已解决遭遇”，四场战斗使用原有位，老年死亡使用第 5 位；新完成整个人生时为 62。已经完成的旧人生仍可直接回看结局，不要求补走死亡遭遇。

迁移前备份位于 `Logs/RefactorBackup/before-systems-upgrade.zip`，包括原脚本、场景、资源配置、测试、工具和说明。项目当前没有 Git 仓库，不能使用 Git 回退本次修改。恢复时需同时恢复脚本与对应 `.meta`，避免 GUID 不匹配。

## 验证与维护命令

```powershell
# 编译运行时与编辑器代码，并执行路线、年龄能力、攻击模式与存档逻辑断言
./Tools/Validate-Journey.ps1

# 比对原脚本 GUID、检查重复 GUID 和类名，生成逐文件 Markdown 索引
./Tools/Audit-ProjectScripts.ps1

# 在 Logs 下的独立项目副本运行真实 Unity 流程，不操作当前已打开的编辑器
./Tools/Run-JourneySmoke.ps1

# 同时生成关键界面、攻击预警和死亡同行截图
./Tools/Run-JourneySmoke.ps1 -RenderScreenshots
```

验证使用 Unity 2022.3.62f1。可通过 `-UnityEditorPath` 指定其他安装目录。编译脚本需要项目已在 Unity 打开过以取得已解析的程序集；真实流程测试需要本机 Unity 授权服务可用。

独立测试使用专门存档键，完成后清理；不会覆盖玩家进度。成功后仅把已验证的六个场景与阶段配置迁移结果复制回主项目。日志为 `Logs/JourneyValidation/systems-smoke.log`，截图为 `Logs/JourneyValidation/SystemQA/Logs/JourneyScreenshots/`。

编辑器还提供：

- **Life Journey → Validate Campaign**：场景、配置与存档校验。
- **Life Journey → Audit Script And Scene Bindings**：脚本 GUID 和六关旧分块解绑检查。
- **Life Journey → Migrate Legacy Chunk Bindings**：可重复执行的旧分块美术迁移。仅在停止播放且已保存场景时运行。
- 选中正式关卡的 `ChunkSpawner`，Inspector 中可直接跳到阶段、心魔、随机障碍配置。

自动测试会加速定位到关键位置；完整关卡的手感、叙事时长与连续游玩难度仍需人工试玩调优。

### 本次验证结果（2026-11-09）

- 运行时与编辑器 C# 编译通过。
- **69,812 条逻辑断言通过**：六阶段能力、两种长度、32 组种子、确定性重试、普通障碍过滤、检查点与回忆安全、战斗区保护、跳跃可达性、边界攻击安全、死亡无攻击、存档规则。
- **Unity 真实流程回归通过**：六关流转、旧 Boss 隔离、普通障碍池复用与重置、攻击预警／伤害窗口／清理、预警中暂停、未解决心魔死亡后满血重建且保持种子、四场战斗、死亡同行、出口约束和结局续读。
- 51 个原脚本 GUID 全部保留，当前 61 个运行时／兼容脚本无重复 GUID；六个正式场景均已解除旧分块列表绑定。
- 渲染了 14 张关键界面与遭遇截图，并检查了攻击预警及老年同行画面。当前心魔造型仍复用原图片，专门美术可在配置中替换。

参考：[流程日志](Logs/JourneyValidation/systems-smoke.log)、[死亡同行截图](Logs/JourneyValidation/SystemQA/Logs/JourneyScreenshots/death-companion.png)。
