from collections import deque
from pathlib import Path
import colorsys

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
INPUT_PATH = ROOT / "Assets" / "Art" / "Energy" / "source_energy_crystal.png"
OUTPUT_PATH = ROOT / "Assets" / "Art" / "Energy" / "energy_crystal_transparent.png"
PREVIEW_PATH = ROOT / "Assets" / "Art" / "Energy" / "energy_crystal_preview_on_dark.png"

PADDING = 12


def is_background_pixel(pixel):
    r, g, b, a = pixel
    if a == 0:
        return True

    rf, gf, bf = r / 255.0, g / 255.0, b / 255.0
    h, s, v = colorsys.rgb_to_hsv(rf, gf, bf)
    return v >= 0.82 and s <= 0.20 and min(r, g, b) >= 190


def flood_background(image):
    width, height = image.size
    pixels = image.load()
    visited = bytearray(width * height)
    background = bytearray(width * height)
    queue = deque()

    def add_if_background(x, y):
        index = y * width + x
        if visited[index]:
            return

        visited[index] = 1
        if is_background_pixel(pixels[x, y]):
            background[index] = 1
            queue.append((x, y))

    for x in range(width):
        add_if_background(x, 0)
        add_if_background(x, height - 1)

    for y in range(height):
        add_if_background(0, y)
        add_if_background(width - 1, y)

    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if nx < 0 or ny < 0 or nx >= width or ny >= height:
                continue

            index = ny * width + nx
            if visited[index]:
                continue

            visited[index] = 1
            if is_background_pixel(pixels[nx, ny]):
                background[index] = 1
                queue.append((nx, ny))

    return background


def apply_alpha(image, background):
    width, height = image.size
    alpha = Image.new("L", image.size, 255)
    alpha_pixels = alpha.load()

    for y in range(height):
        for x in range(width):
            if background[y * width + x]:
                alpha_pixels[x, y] = 0

    soft_alpha = alpha.filter(ImageFilter.GaussianBlur(radius=0.55))
    result = image.copy()
    result.putalpha(soft_alpha)
    return result


def crop_with_padding(image):
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        return image

    left = max(0, bbox[0] - PADDING)
    top = max(0, bbox[1] - PADDING)
    right = min(image.width, bbox[2] + PADDING)
    bottom = min(image.height, bbox[3] + PADDING)
    return image.crop((left, top, right, bottom))


def make_preview(image):
    background = Image.new("RGBA", image.size, (18, 20, 26, 255))
    background.alpha_composite(image)
    return background.convert("RGB")


def main():
    if not INPUT_PATH.exists():
        raise FileNotFoundError(f"Input image not found: {INPUT_PATH}")

    source = Image.open(INPUT_PATH).convert("RGBA")
    background = flood_background(source)
    transparent = crop_with_padding(apply_alpha(source, background))
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    transparent.save(OUTPUT_PATH)
    make_preview(transparent).save(PREVIEW_PATH)

    alpha = transparent.getchannel("A")
    alpha_values = list(alpha.getdata())
    transparent_pixels = sum(1 for value in alpha_values if value == 0)
    transparent_ratio = transparent_pixels / len(alpha_values)
    has_alpha_zero = transparent_pixels > 0

    print(f"Input image: {INPUT_PATH}")
    print(f"Output image: {OUTPUT_PATH}")
    print(f"Preview image: {PREVIEW_PATH}")
    print(f"Output mode: {transparent.mode}")
    print(f"Transparent pixel ratio: {transparent_ratio:.4f}")
    print(f"Has alpha=0 pixels: {has_alpha_zero}")

    if transparent_ratio < 0.10:
        print("Warning: transparent pixel ratio is low. Consider loosening is_background_pixel thresholds.")


if __name__ == "__main__":
    main()
