# LightVisual Sorting Notes

Greybox LightVisual display works by drawing a fake, unlit, translucent yellow circle above the player and Tilemap.

Recommended Order in Layer values:

- WallTilemap: `1`
- PlayerPlaceholder: `5`
- ExitPlaceholder: `5`
- LightVisual: `100`

`LightVisual` is controlled by `PlayerLightVisualController` at runtime and must stay above the greybox gameplay sprites so the player and floor visibly receive the yellow overlay.
