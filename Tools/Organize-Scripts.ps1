$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$scriptRoot = Join-Path $projectRoot 'Assets/Script'
$moves = @{
    'Journey/LifeJourneySession.cs' = 'Runtime/Core/LifeJourneySession.cs'
    'Journey/LifeJourneySettings.cs' = 'Runtime/Core/LifeJourneySettings.cs'
    'Journey/LifeStage.cs' = 'Runtime/Core/LifeStage.cs'
    'Journey/JourneySave.cs' = 'Runtime/Save/JourneySave.cs'
    'Journey/LifeCourseGenerator.cs' = 'Runtime/Generation/LifeCourseGenerator.cs'
    'Journey/JourneyRoutePlan.cs' = 'Runtime/Generation/JourneyRoutePlan.cs'
    'Journey/JourneyHazard.cs' = 'Runtime/Generation/JourneyHazard.cs'
    'Journey/JourneyBossController.cs' = 'Runtime/Encounters/JourneyBossController.cs'
    'Journey/JourneyProjectile.cs' = 'Runtime/Combat/JourneyProjectile.cs'
    'Journey/JourneyMemory.cs' = 'Runtime/Collectibles/JourneyMemory.cs'
    'Journey/JourneyPresentation.cs' = 'Runtime/Presentation/JourneyPresentation.cs'
    'Journey/JourneyUI.cs' = 'Runtime/UI/JourneyUI.cs'
    'Journey/JourneyHoldButton.cs' = 'Runtime/UI/JourneyHoldButton.cs'
    'Player/PlayerController.cs' = 'Runtime/Player/PlayerController.cs'
    'Player/Coin.cs' = 'Runtime/Collectibles/Coin.cs'
    'Player/ObjectDamage.cs' = 'Legacy/Combat/ObjectDamage.cs'
    'Player/EnergyCollector.cs' = 'Legacy/Combat/EnergyCollector.cs'
    'CameraFollow.cs' = 'Runtime/Presentation/CameraFollow.cs'
    'InfiniteBackground.cs' = 'Runtime/Presentation/InfiniteBackground.cs'
    'CollisionSound.cs' = 'Runtime/Audio/CollisionSound.cs'
    'Manager/SoundManager.cs' = 'Runtime/Audio/SoundManager.cs'
    'Manager/GameManager.cs' = 'Legacy/Flow/GameManager.cs'
    'ChunkSpawner.cs' = 'Runtime/Generation/ChunkSpawner.cs'
    'Customs clearance.cs' = 'Legacy/Level/Customsclearance.cs'
    'The End.cs' = 'Legacy/Flow/TheEnd.cs'
    'DetachChild.cs' = 'Legacy/Level/DetachChild.cs'
    'ForwardMovement.cs' = 'Legacy/Combat/ForwardMovement.cs'
    'FloatInScreenArea2D.cs' = 'Legacy/Collectibles/FloatInScreenArea2D.cs'
    'Barrier/Hole.cs' = 'Legacy/Level/Hole.cs'
    'Barrier/MoveBarrier.cs' = 'Legacy/Level/MoveBarrier.cs'
    'Barrier/Moveble obstacles.cs' = 'Legacy/Level/Movebleobstacles.cs'
    'Barrier/ResetObstaclePosition.cs' = 'Legacy/Level/ResetObstaclePosition.cs'
    'Boss/Boss.cs' = 'Legacy/Combat/Boss.cs'
    'Boss/Boss apper.cs' = 'Legacy/Combat/Bossapper.cs'
    'Boss/BossHealth.cs' = 'Legacy/Combat/BossHealth.cs'
    'Boss/HealthBar.cs' = 'Legacy/UI/HealthBar.cs'
    'UI/GameOverUI.cs' = 'Legacy/UI/GameOverUI.cs'
    'UI/PuaseUI.cs' = 'Legacy/UI/PuaseUI.cs'
    'Game/StartGame.cs' = 'Legacy/Flow/StartGame.cs'
    'Game/LoadingScene.cs' = 'Legacy/Flow/LoadingScene.cs'
    'Game/GameOver.cs' = 'Legacy/Flow/GameOver.cs'
    'Prop/BarrierStart.cs' = 'Legacy/Level/BarrierStart.cs'
    'Prop/SpeedUp.cs' = 'Runtime/Collectibles/SpeedUp.cs'
    'AnswerSystem/Answer.cs' = 'Legacy/UI/Answer.cs'
    'AnswerSystem/AnswerSO.cs' = 'Legacy/Data/AnswerSO.cs'
    'Item/Inventory/LootSO.cs' = 'Runtime/Items/Data/LootSO.cs'
    'Item/Inventory/ItemSO.cs' = 'Runtime/Items/Data/ItemSO.cs'
    'Item/Inventory/BuffSo.cs' = 'Runtime/Items/Data/BuffSo.cs'
    'Item/Inventory/Loot.cs' = 'Runtime/Items/Loot.cs'
    'Item/Inventory/Inventory.cs' = 'Runtime/Items/Inventory.cs'
    'Item/Inventory/InventoryManager.cs' = 'Runtime/Items/InventoryManager.cs'
}
foreach ($entry in $moves.GetEnumerator()) {
    $source = [IO.Path]::GetFullPath((Join-Path $scriptRoot $entry.Key))
    $destination = [IO.Path]::GetFullPath((Join-Path $scriptRoot $entry.Value))
    if (-not $source.StartsWith($scriptRoot + [IO.Path]::DirectorySeparatorChar) -or
        -not $destination.StartsWith($scriptRoot + [IO.Path]::DirectorySeparatorChar)) { throw 'Move outside Assets/Script rejected.' }
    if (-not (Test-Path -LiteralPath $source)) { continue }
    if (Test-Path -LiteralPath $destination) { throw "Destination already exists: $destination" }
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Move-Item -LiteralPath $source -Destination $destination
    if (Test-Path -LiteralPath ($source + '.meta')) { Move-Item -LiteralPath ($source + '.meta') -Destination ($destination + '.meta') }
}
Write-Output 'Organized project scripts with their original Unity GUIDs.'
Get-ChildItem -LiteralPath $scriptRoot -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
    if ($_.FullName.StartsWith($scriptRoot + [IO.Path]::DirectorySeparatorChar) -and
        @(Get-ChildItem -LiteralPath $_.FullName -Force).Count -eq 0) {
        Remove-Item -LiteralPath $_.FullName
        if (Test-Path -LiteralPath ($_.FullName + '.meta')) { Remove-Item -LiteralPath ($_.FullName + '.meta') }
    }
}
