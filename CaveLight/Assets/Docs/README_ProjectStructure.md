# CaveLight 项目结构说明

此文档说明 Unity 项目 `Assets` 下主要文件夹用途，帮助团队快速理解目录分布。

## Scenes
存放游戏关卡场景文件，如主场景、战斗场景、商人房等。

## Scripts
存放游戏代码。
- Core：游戏核心系统、通用游戏管理、状态机、时间/流程控制等。
- Player：玩家相关逻辑，如移动、能源消耗、交互接口等。
- LevelGeneration：关卡构建、洞穴地图生成、Tilemap 组合等（暂不写复杂生成逻辑）。
- Energy：能源系统、能量补给、能源显示、能源消耗等。
- Monster：怪物行为、敌人属性、攻击与死亡处理等。
- Shop：商人房与商品系统、购买逻辑、商店界面等。
- UI：用户界面、HUD、提示、结算屏等。
- Common：通用工具类、扩展、数据定义、辅助函数等。

## Prefabs
存放可复用预制件。
- Player：玩家预制件。
- Energy：能源物品和能源节点预制件。
- Monster：怪物预制件。
- Level：关卡、出口、Tilemap 组合等预制件。
- UI：UI 界面相关预制件。
- Shop：商人房与商品展示预制件。

## Art
存放美术资源。
- Placeholder：占位图资源，用于快速验证玩法。
- Tiles：地图瓦片素材。
- Player：玩家精灵与动画素材。
- Monster：怪物精灵素材。
- Energy：能源与能量节点素材。
- UI：界面图标、按钮、装饰等素材。
- Background：背景图与环境装饰素材。

## Tiles
存放 Tilemap 相关 Tile 资源。

## TilePalettes
存放 TilePalette 资源，用于 Tilemap 编辑。

## ScriptableObjects
存放可配置数据资产。
- Player：玩家配置数据。
- Level：关卡与生成配置。
- Monster：怪物属性配置。
- Shop：商店商品与价格配置。

## Materials
存放材质资源。

## Settings
存放项目自定义设置资产。

## Docs
存放项目文档、需求说明、任务分解等文档文件。
