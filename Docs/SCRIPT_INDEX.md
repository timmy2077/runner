# 项目脚本索引

项目自有运行时代码按职责归入 `Assets/Script/Runtime`；旧场景和预制体兼容入口归入 `Assets/Script/Legacy`。编辑器工具保留在 `Assets/Editor`。TextMesh Pro 的示例脚本属于第三方资源，保持原目录。

所有移动均保留原 `.meta` 和 GUID。旧类名、序列化字段及按钮调用入口保留；含空格的文件名已改为与类名一致。

| 脚本 | 职责模块 |
| --- | --- |
| [FloatInScreenArea2D](../Assets/Script/Legacy/Collectibles/FloatInScreenArea2D.cs) | Legacy/Collectibles |
| [Boss](../Assets/Script/Legacy/Combat/Boss.cs) | Legacy/Combat |
| [Bossapper](../Assets/Script/Legacy/Combat/Bossapper.cs) | Legacy/Combat |
| [BossHealth](../Assets/Script/Legacy/Combat/BossHealth.cs) | Legacy/Combat |
| [EnergyCollector](../Assets/Script/Legacy/Combat/EnergyCollector.cs) | Legacy/Combat |
| [ForwardMovement](../Assets/Script/Legacy/Combat/ForwardMovement.cs) | Legacy/Combat |
| [ObjectDamage](../Assets/Script/Legacy/Combat/ObjectDamage.cs) | Legacy/Combat |
| [AnswerSO](../Assets/Script/Legacy/Data/AnswerSO.cs) | Legacy/Data |
| [GameManager](../Assets/Script/Legacy/Flow/GameManager.cs) | Legacy/Flow |
| [GameOver](../Assets/Script/Legacy/Flow/GameOver.cs) | Legacy/Flow |
| [JourneyLegacyIsolation](../Assets/Script/Legacy/Flow/JourneyLegacyIsolation.cs) | Legacy/Flow |
| [LoadingScene](../Assets/Script/Legacy/Flow/LoadingScene.cs) | Legacy/Flow |
| [StartGame](../Assets/Script/Legacy/Flow/StartGame.cs) | Legacy/Flow |
| [TheEnd](../Assets/Script/Legacy/Flow/TheEnd.cs) | Legacy/Flow |
| [BarrierStart](../Assets/Script/Legacy/Level/BarrierStart.cs) | Legacy/Level |
| [Customsclearance](../Assets/Script/Legacy/Level/Customsclearance.cs) | Legacy/Level |
| [DetachChild](../Assets/Script/Legacy/Level/DetachChild.cs) | Legacy/Level |
| [Hole](../Assets/Script/Legacy/Level/Hole.cs) | Legacy/Level |
| [LegacyChunkStreamer](../Assets/Script/Legacy/Level/LegacyChunkStreamer.cs) | Legacy/Level |
| [MoveBarrier](../Assets/Script/Legacy/Level/MoveBarrier.cs) | Legacy/Level |
| [Movebleobstacles](../Assets/Script/Legacy/Level/Movebleobstacles.cs) | Legacy/Level |
| [ResetObstaclePosition](../Assets/Script/Legacy/Level/ResetObstaclePosition.cs) | Legacy/Level |
| [Answer](../Assets/Script/Legacy/UI/Answer.cs) | Legacy/UI |
| [GameOverUI](../Assets/Script/Legacy/UI/GameOverUI.cs) | Legacy/UI |
| [HealthBar](../Assets/Script/Legacy/UI/HealthBar.cs) | Legacy/UI |
| [PuaseUI](../Assets/Script/Legacy/UI/PuaseUI.cs) | Legacy/UI |
| [CollisionSound](../Assets/Script/Runtime/Audio/CollisionSound.cs) | Runtime/Audio |
| [SoundManager](../Assets/Script/Runtime/Audio/SoundManager.cs) | Runtime/Audio |
| [Coin](../Assets/Script/Runtime/Collectibles/Coin.cs) | Runtime/Collectibles |
| [JourneyMemory](../Assets/Script/Runtime/Collectibles/JourneyMemory.cs) | Runtime/Collectibles |
| [SpeedUp](../Assets/Script/Runtime/Collectibles/SpeedUp.cs) | Runtime/Collectibles |
| [JourneyProjectile](../Assets/Script/Runtime/Combat/JourneyProjectile.cs) | Runtime/Combat |
| [LifeJourneySession](../Assets/Script/Runtime/Core/LifeJourneySession.cs) | Runtime/Core |
| [LifeJourneySettings](../Assets/Script/Runtime/Core/LifeJourneySettings.cs) | Runtime/Core |
| [LifeStage](../Assets/Script/Runtime/Core/LifeStage.cs) | Runtime/Core |
| [JourneyBossController](../Assets/Script/Runtime/Encounters/JourneyBossController.cs) | Runtime/Encounters |
| [JourneyDemonAttack](../Assets/Script/Runtime/Encounters/JourneyDemonAttack.cs) | Runtime/Encounters |
| [JourneyDemonHitbox](../Assets/Script/Runtime/Encounters/JourneyDemonHitbox.cs) | Runtime/Encounters |
| [JourneyDemonPatterns](../Assets/Script/Runtime/Encounters/JourneyDemonPatterns.cs) | Runtime/Encounters |
| [JourneyDemonSettings](../Assets/Script/Runtime/Encounters/JourneyDemonSettings.cs) | Runtime/Encounters |
| [JourneyEncounterDirector](../Assets/Script/Runtime/Encounters/JourneyEncounterDirector.cs) | Runtime/Encounters |
| [LifeEventDirector](../Assets/Script/Runtime/Events/LifeEventDirector.cs) | Runtime/Events |
| [LifeEventSave](../Assets/Script/Runtime/Events/LifeEventSave.cs) | Runtime/Events |
| [LifeEventSettings](../Assets/Script/Runtime/Events/LifeEventSettings.cs) | Runtime/Events |
| [LifeEventTrigger](../Assets/Script/Runtime/Events/LifeEventTrigger.cs) | Runtime/Events |
| [ChunkSpawner](../Assets/Script/Runtime/Generation/ChunkSpawner.cs) | Runtime/Generation |
| [JourneyGenerationSettings](../Assets/Script/Runtime/Generation/JourneyGenerationSettings.cs) | Runtime/Generation |
| [JourneyHazard](../Assets/Script/Runtime/Generation/JourneyHazard.cs) | Runtime/Generation |
| [JourneyObstacleSpawner](../Assets/Script/Runtime/Generation/JourneyObstacleSpawner.cs) | Runtime/Generation |
| [JourneyObstacleView](../Assets/Script/Runtime/Generation/JourneyObstacleView.cs) | Runtime/Generation |
| [JourneyPrefabPlan](../Assets/Script/Runtime/Generation/JourneyPrefabPlan.cs) | Runtime/Generation |
| [JourneyRoutePlan](../Assets/Script/Runtime/Generation/JourneyRoutePlan.cs) | Runtime/Generation |
| [LifeCourseGenerator](../Assets/Script/Runtime/Generation/LifeCourseGenerator.cs) | Runtime/Generation |
| [BuffSo](../Assets/Script/Runtime/Items/Data/BuffSo.cs) | Runtime/Items/Data |
| [ItemSO](../Assets/Script/Runtime/Items/Data/ItemSO.cs) | Runtime/Items/Data |
| [LootSO](../Assets/Script/Runtime/Items/Data/LootSO.cs) | Runtime/Items/Data |
| [Inventory](../Assets/Script/Runtime/Items/Inventory.cs) | Runtime/Items |
| [InventoryManager](../Assets/Script/Runtime/Items/InventoryManager.cs) | Runtime/Items |
| [Loot](../Assets/Script/Runtime/Items/Loot.cs) | Runtime/Items |
| [PlayerController](../Assets/Script/Runtime/Player/PlayerController.cs) | Runtime/Player |
| [CameraFollow](../Assets/Script/Runtime/Presentation/CameraFollow.cs) | Runtime/Presentation |
| [InfiniteBackground](../Assets/Script/Runtime/Presentation/InfiniteBackground.cs) | Runtime/Presentation |
| [JourneyBackgroundAlignment](../Assets/Script/Runtime/Presentation/JourneyBackgroundAlignment.cs) | Runtime/Presentation |
| [JourneyPresentation](../Assets/Script/Runtime/Presentation/JourneyPresentation.cs) | Runtime/Presentation |
| [JourneySave](../Assets/Script/Runtime/Save/JourneySave.cs) | Runtime/Save |
| [JourneyEventPanel](../Assets/Script/Runtime/UI/JourneyEventPanel.cs) | Runtime/UI |
| [JourneyHoldButton](../Assets/Script/Runtime/UI/JourneyHoldButton.cs) | Runtime/UI |
| [JourneyUI](../Assets/Script/Runtime/UI/JourneyUI.cs) | Runtime/UI |
| [BatchRenameTool](../Assets/Editor/BatchRenameTool.cs) | Editor |
| [BatchTextureSetting](../Assets/Editor/BatchTextureSetting.cs) | Editor |
| [JourneyChunkSpawnerEditor](../Assets/Editor/JourneyChunkSpawnerEditor.cs) | Editor |
| [JourneyPrefabValidation](../Assets/Editor/JourneyPrefabValidation.cs) | Editor |
| [JourneySystemsMigration](../Assets/Editor/JourneySystemsMigration.cs) | Editor |
| [JourneyValidation](../Assets/Editor/JourneyValidation.cs) | Editor |
| [LifeEventTools](../Assets/Editor/LifeEventTools.cs) | Editor |
| [LifeEventValidation](../Assets/Editor/LifeEventValidation.cs) | Editor |

本次索引包含 68 个项目运行时/兼容脚本；与升级前备份比对，51 个原脚本 GUID 全部保留。
