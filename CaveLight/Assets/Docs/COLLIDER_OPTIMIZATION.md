# 碰撞体优化说明

## 问题

使用 `BoxCollider2D` 时，玩家在 Tilemap 瓦片的边缘容易被卡住，尤其是跳跃后落下时。

## 解决方案

将 `PlayerPlaceholder` 的碰撞体从 `BoxCollider2D` 更换为 `CapsuleCollider2D`。

### 优势

- **圆角设计**：`CapsuleCollider2D` 的顶部和底部为圆形，可以平滑地滑过瓦片的尖角。
- **更好的跳跃体验**：玩家能更自然地在平台之间跳跃，而不会被卡在转角。
- **减少卡顿**：圆角碰撞体可以吸收掉瓦片边缘的碰撞锐角。

### 配置

在 `PlayerPlaceholder` 预制件上应用以下碰撞体设置：

- **Collider Type**：`CapsuleCollider2D`
- **Direction**：`Vertical`
- **Size X**：`0.8`
- **Size Y**：`1.8`
- **Is Trigger**：`false`

### 自动应用

运行菜单命令自动应用此更改：

```
CaveLight > Fix Player Collider (Box -> Capsule)
```

此命令会自动：
1. 移除 `BoxCollider2D`
2. 添加 `CapsuleCollider2D`
3. 保存预制件

## 其他配置保留

- `Rigidbody2D` 保持 `Dynamic` 模式
- `Freeze Rotation Z` 保持开启，防止旋转
- `SpriteRenderer` 和其他组件保持不变

## 测试

运行场景后，观察：
- 玩家跳跃时不再被瓦片边缘卡住
- 落地更平滑，减少抖动
- 在平台之间跳跃时更流畅
