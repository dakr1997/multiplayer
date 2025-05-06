# Unity Project Summary
Generated on: 2025-05-06 19:43:06
Project: My project
Version: 1.0

## Project Structure
Key project folders:
- Editor
  - Contains 2 scripts
- New_Scripts
  - Contains 33 scripts
  - Core
  - Enemies
  - UI
- Prefabs
  - Core
  - Network
  - UI_Prefabs
- Resources
- Scenes
- ScriptableObjects
  - Contains 3 scripts
- Scripts
  - Contains 17 scripts
  - Camera
  - Enemies
  - Interfaces
  - NetworkHelper
  - Player
  - ...and 3 more subfolder(s)
- Settings
  - Scenes
- Sprites
  - Bars
  - Enemies
  - Player
- TextMesh Pro
  - Fonts
  - Resources
  - Shaders
  - Sprites
- UI

## Code Analysis
Total scripts: 55
- MonoBehaviour scripts: 12
- ScriptableObject scripts: 3
- Interfaces: 2
- Static classes: 2

### Key inheritance hierarchies:
- NetworkBehaviour: 21 implementations
  - PlayerClientHandler
  - PlayerDamage
  - PlayerExperience
  - PlayerMovement
  - ProjectileSpawner
  - ...and 16 more
- MonoBehaviour: 12 implementations
  - CodeSummarizer
  - EnemyManager
  - JoinMenu
  - LobbyUI
  - PlayerCameraFollow_Smooth
  - ...and 7 more
- GameState: 4 implementations
  - BuildingState
  - GameOverState
  - LobbyState
  - WaveState
- ScriptableObject: 3 implementations
  - EnemyData
  - ProjectileData
  - TowerData
- PoolableNetworkObject: 3 implementations
  - ExpBubble
  - Projectile
  - PoolableEnemy

### Most referenced classes:
- TextMeshProUGUI: referenced by 3 classes
- EnemyData: referenced by 3 classes
- TMP_InputField: referenced by 2 classes
- Button: referenced by 2 classes
- LayerMask: referenced by 2 classes
- NetworkObject: referenced by 2 classes
- Slider: referenced by 1 classes
- ProjectileData: referenced by 1 classes
- MainTowerHP: referenced by 1 classes
- TowerData: referenced by 1 classes

## Recent Changes
Files modified in the last 1 days:
- Assets\Scripts\Player\PlayerClientHandler.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Towers\Tower.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Network\ClientPrediction.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Player\Base\PlayerEntity.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Player\Classes\Archer\ArcherComponent.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Player\Classes\Warrior\WarriorComponent.cs (modified 2025-05-05)
- Assets\New_Scripts\Enemies\Base\EnemyEntity.cs (modified 2025-05-05)
- Assets\New_Scripts\Enemies\Base\PoolableEnemy.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Network\NetworkedEntity.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Entities\GameEntity.cs (modified 2025-05-05)
- Assets\Scripts\Tower\Projectile.cs (modified 2025-05-05)
- Assets\Scripts\NetworkHelper\Pools\NetworkObjectPool.cs (modified 2025-05-05)
- Assets\Scripts\NetworkHelper\DamageHelper.cs (modified 2025-05-05)
- Assets\Scripts\NetworkHelper\Pools\PoolableNetworkObject.cs (modified 2025-05-05)
- Assets\New_Scripts\Enemies\EnemyManager.cs (modified 2025-05-05)
- Assets\New_Scripts\Enemies\Base\EnemyAI.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\WaveSystem\WaveManager.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\GameState\BuildingState.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Lobby\GameSceneInitializer.cs (modified 2025-05-05)
- Assets\New_Scripts\Core\Components\HealthComponent.cs (modified 2025-05-05)
- ...and 10 more files

## TODO Items
- [CodeSummarizer.cs] items
- [CodeSummarizer.cs] comments
- [CodeSummarizer.cs] */ style comments
            todoMatches = Regex.Matches(content, @"/\*\s*TODO:?\s*(.+?)\
- [GameOverState.cs] Implement UI activation based on your UI system
- [GameOverState.cs] Implement UI deactivation based on your UI system

## Class Diagram
```mermaid
classDiagram
  class AssemblyDefinitionGenerator {
  }
  class CodeSummarizer {
    +MonoBehaviour
  }
  MonoBehaviour <|-- CodeSummarizer
  class EnemyData {
    +ScriptableObject
  }
  ScriptableObject <|-- EnemyData
  class ProjectileData {
    +ScriptableObject
  }
  ScriptableObject <|-- ProjectileData
  class TowerData {
    +ScriptableObject
  }
  ScriptableObject <|-- TowerData
  class EnemyManager {
    +MonoBehaviour
    +RegisterEnemy()
    +UnregisterEnemy()
    +GetActiveEnemyCount()
  }
  MonoBehaviour <|-- EnemyManager
  class JoinMenu {
    +MonoBehaviour
  }
  MonoBehaviour <|-- JoinMenu
  JoinMenu --> TMP_InputField : references
  JoinMenu --> Button : references
  JoinMenu --> TextMeshProUGUI : references
  class LobbyUI {
    +MonoBehaviour
  }
  MonoBehaviour <|-- LobbyUI
  LobbyUI --> Button : references
  LobbyUI --> TextMeshProUGUI : references
  LobbyUI --> TMP_InputField : references
  class PlayerCameraFollow_Smooth {
    +MonoBehaviour
    +SetTarget()
  }
  MonoBehaviour <|-- PlayerCameraFollow_Smooth
  class DamageHelper {
  }
  class ExpBubble {
    +Initialize()
  }
  PoolableNetworkObject <|-- ExpBubble
  class PlayerClientHandler {
    +Initialize()
  }
  NetworkBehaviour <|-- PlayerClientHandler
  class PlayerDamage {
  }
  NetworkBehaviour <|-- PlayerDamage
  PlayerDamage --> LayerMask : references
  class PlayerExperience {
    +AddXP()
  }
  NetworkBehaviour <|-- PlayerExperience
  class PlayerHUDController {
    +MonoBehaviour
    +Initialize()
    +UpdateHealth()
    +UpdateExp()
    +UpdateLevel()
    +UpdateTowerHealth()
  }
  MonoBehaviour <|-- PlayerHUDController
  PlayerHUDController --> Slider : references
  PlayerHUDController --> TextMeshProUGUI : references
  class PlayerMovement {
  }
  NetworkBehaviour <|-- PlayerMovement
  class Aim {
    +AddTarget()
    +RemoveTarget()
    +PredictTargetPosition()
  }
  class Projectile {
    +Initialize()
  }
  PoolableNetworkObject <|-- Projectile
  Projectile --> LayerMask : references
  class ProjectileSpawner {
    +SetShootingPoint()
    +SpawnProjectile()
  }
  NetworkBehaviour <|-- ProjectileSpawner
  ProjectileSpawner --> NetworkObject : references
  ProjectileSpawner --> ProjectileData : references
  class TowerSpawner {
    +MonoBehaviour
    +SpawnTower()
  }
  MonoBehaviour <|-- TowerSpawner
  class HealthComponent {
    +SetHealthMultiplier()
    +TakeDamage()
    +Heal()
  }
  NetworkBehaviour <|-- HealthComponent
  class GameEntity {
  }
  NetworkBehaviour <|-- GameEntity
  class GameInitializer {
    +MonoBehaviour
  }
  MonoBehaviour <|-- GameInitializer
  class GameManager {
    +ConnectWaveManager()
    +ConnectMainTower()
    +StartGame()
    +EndGame()
    +GetCurrentWave()
    +GetGameTime()
    +IsGameActive()
    +IsVictory()
  }
  NetworkBehaviour <|-- GameManager
  GameManager --> MainTowerHP : references
  class GameServices {
  }
  class BuildingState {
    +SkipBuilding()
  }
  GameState <|-- BuildingState
  class GameOverState {
    +SetVictory()
    +RestartGame()
  }
  GameState <|-- GameOverState
  class GameState {
  }
  class GameStateManager {
    +ChangeState()
  }
  NetworkBehaviour <|-- GameStateManager
  class LobbyState {
  }
  GameState <|-- LobbyState
  class WaveState {
  }
  GameState <|-- WaveState
  class IDamageable {
    <<interface>>
    +TakeDamage()
    +Heal()
  }
  class IPoolable {
    <<interface>>
    +SetPool()
    +OnSpawn()
    +OnDespawn()
    +ReturnToPool()
  }
  class GameSceneInitializer {
    +MonoBehaviour
  }
  MonoBehaviour <|-- GameSceneInitializer
  class NetworkLobbyManager {
    +Equals()
    +PlayerConnected()
    +PlayerDisconnected()
    +SetPlayerNameServerRpc()
    +TogglePlayerReadyServerRpc()
    +StartGame()
  }
  NetworkBehaviour <|-- NetworkLobbyManager
  class ClientPrediction {
    +MonoBehaviour
    +ProcessLocalInput()
    +ReconcileWithServer()
    +AddServerState()
    +InterpolateServerStates()
  }
  MonoBehaviour <|-- ClientPrediction
  class NetworkedEntity {
  }
  NetworkBehaviour <|-- NetworkedEntity
  class PersistentObject {
    +MonoBehaviour
  }
  MonoBehaviour <|-- PersistentObject
  class Tower {
  }
  NetworkBehaviour <|-- Tower
  Tower --> TowerData : references
  class WaveManager {
    +StartWave()
    +CompleteCurrentWave()
    +GetCurrentWave()
    +IsWaveActive()
    +GetTotalWaves()
  }
  NetworkBehaviour <|-- WaveManager
  class EnemyAI {
    +SetEnemyData()
    +InitializeForWave()
  }
  NetworkBehaviour <|-- EnemyAI
  EnemyAI --> EnemyData : references
  class EnemyDamage {
    +SetEnemyData()
    +SetDamageMultiplier()
  }
  NetworkBehaviour <|-- EnemyDamage
  EnemyDamage --> EnemyData : references
  class EnemyEntity {
    +InitializeForWave()
  }
  GameEntity <|-- EnemyEntity
  EnemyEntity --> EnemyData : references
  class EnemySpawner {
    +MonoBehaviour
    +OnWaveStateEntered()
    +OnWaveStateExited()
    +ConfigureForWave()
    +SetSpawningEnabled()
    +IsDoneSpawning()
  }
  MonoBehaviour <|-- EnemySpawner
  class PoolableEnemy {
  }
  PoolableNetworkObject <|-- PoolableEnemy
  class XPManager {
    +AwardXP()
    +AwardXPToAll()
  }
  NetworkBehaviour <|-- XPManager
  XPManager --> NetworkObject : references
  class PlayerNetworkAnimator {
  }
  NetworkAnimator <|-- PlayerNetworkAnimator
  class PlayerNetworkTransform {
  }
  NetworkTransform <|-- PlayerNetworkTransform
  class NetworkObjectPool {
    +Get()
    +Release()
    +PrintStats()
  }
  NetworkBehaviour <|-- NetworkObjectPool
  class PoolableNetworkObject {
    +SetPool()
    +ReturnToPool()
  }
  NetworkBehaviour <|-- PoolableNetworkObject
  class PlayerEntity {
  }
  GameEntity <|-- PlayerEntity
  class MainTowerHP {
    +TakeDamage()
    +RequestSetHPServerRpc()
    +Heal()
    +Reset()
  }
  NetworkBehaviour <|-- MainTowerHP
  class ArcherComponent {
  }
  NetworkBehaviour <|-- ArcherComponent
  class WarriorComponent {
  }
  NetworkBehaviour <|-- WarriorComponent

```
