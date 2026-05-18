# 占位符资源说明

此文档说明 CaveLight 项目的占位符（Placeholder）资源规格及用途。

## 概述

这些占位符资源仅用于灰盒快速验证玩法机制。后续正式美术可直接替换 Sprite，无需修改 Prefab 名字或项目结构。

## 资源规格

### Tile（瓦片）
- 单格尺寸：1 x 1 Unity Unit
- 像素尺寸：32 x 32 pixels
- 类型：PlaceholderWallTile、PlaceholderGroundTile

### 角色与物体

| 资源 | 尺寸 | 像素尺寸 | 文件名 |
|------|------|---------|--------|
| Player | 1 x 2 Unity Unit | 32 x 64 pixels | placeholder_player.png |
| Monster | 1 x 1 Unity Unit | 32 x 32 pixels | placeholder_monster.png |
| Energy | 0.3 x 0.3 Unity Unit | 16 x 16 pixels | placeholder_energy.png |
| CaveEnergyNode | 1 x 1 Unity Unit | 32 x 32 pixels | placeholder_cave_energy_node.png |
| Exit | 1 x 2 Unity Unit | 32 x 64 pixels | placeholder_exit.png |

### Sprite 导入设置
- Texture Type：Sprite
- Pixels Per Unit：32
- Filter Mode：Point

## Prefab 清单

### Player
- **PlayerPlaceholder**：玩家占位预制件
  - Components：SpriteRenderer、Rigidbody2D (Dynamic, Freeze Rotation Z)、BoxCollider2D

### Monster
- **MonsterPlaceholder**：怪物占位预制件
  - Components：SpriteRenderer、Rigidbody2D (Kinematic)、BoxCollider2D

### Energy
- **EnergyPlaceholder**：能源掉落物占位预制件
  - Components：SpriteRenderer、Rigidbody2D (Dynamic)、CircleCollider2D (Is Trigger)

- **CaveEnergyNodePlaceholder**：洞穴能源节点占位预制件
  - Components：SpriteRenderer、BoxCollider2D

### Level
- **ExitPlaceholder**：出口占位预制件
  - Components：SpriteRenderer、BoxCollider2D (Is Trigger)

## Tile 清单

- **PlaceholderWallTile**：墙壁瓦片（深灰色）
- **PlaceholderGroundTile**：地面瓦片（灰褐色）

## 美术替换指南

1. 用原始 PNG 文件替换 `Assets/Art/Placeholder/` 中的占位符图片
2. 确保 Sprite 导入设置保持一致（PPU = 32）
3. 无需修改任何 Prefab 或脚本
4. Tile 资源会自动使用新的 Sprite

## 注意事项

- 所有物体尺寸以 1 Unity Unit = 32 pixels 标准设计
- Collider 尺寸已根据 Sprite 尺寸配置，无需手动调整
- 占位符使用纯色填充，方便快速识别，正式美术需要替换为详细的像素艺术
