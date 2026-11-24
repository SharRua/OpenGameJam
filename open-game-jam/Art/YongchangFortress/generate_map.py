"""Build the game-scale Yongchangpu-inspired map atlas from source artwork.

Requires Pillow. The complete composition is drawn first and then used as an
11 x 17 atlas, so neighbouring 256 px atlas regions share exact boundaries.
"""

from __future__ import annotations

import json
import random
from pathlib import Path

from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
TERRAIN = HERE.parent / "Terrain" / "Terrain_Atlas.png"
TILE = 256
COLS, ROWS = 11, 17
WIDTH, HEIGHT = COLS * TILE, ROWS * TILE
RNG = random.Random(1558)


def repeat_into(target: Image.Image, texture: Image.Image, box: tuple[int, int, int, int]) -> None:
    left, top, right, bottom = box
    for y in range(top, bottom, texture.height):
        for x in range(left, right, texture.width):
            patch = texture.crop((0, 0, min(texture.width, right - x), min(texture.height, bottom - y)))
            target.paste(patch, (x, y))


def pattern(texture: Image.Image) -> Image.Image:
    output = Image.new("RGBA", (WIDTH, HEIGHT))
    repeat_into(output, texture, (0, 0, WIDTH, HEIGHT))
    return output


def seamless_stone() -> Image.Image:
    image = Image.open(HERE / "Stone_Source.png").convert("RGB")
    image = image.crop((126, 126, 1126, 1126)).resize((TILE, TILE), Image.Resampling.LANCZOS)
    pixels = image.load()
    for y in range(TILE):
        for edge in range(28):
            a = pixels[edge, y]
            b = pixels[TILE - 1 - edge, y]
            center = tuple((a[c] + b[c]) // 2 for c in range(3))
            weight = (28 - edge) / 28
            pixels[edge, y] = tuple(round(a[c] * (1 - weight) + center[c] * weight) for c in range(3))
            pixels[TILE - 1 - edge, y] = tuple(round(b[c] * (1 - weight) + center[c] * weight) for c in range(3))
    for x in range(TILE):
        for edge in range(28):
            a = pixels[x, edge]
            b = pixels[x, TILE - 1 - edge]
            center = tuple((a[c] + b[c]) // 2 for c in range(3))
            weight = (28 - edge) / 28
            pixels[x, edge] = tuple(round(a[c] * (1 - weight) + center[c] * weight) for c in range(3))
            pixels[x, TILE - 1 - edge] = tuple(round(b[c] * (1 - weight) + center[c] * weight) for c in range(3))
    image.save(HERE / "Stone_Seamless.png")
    return image.convert("RGBA")


def mask_rectangle(box: tuple[int, int, int, int]) -> Image.Image:
    mask = Image.new("L", (WIDTH, HEIGHT))
    ImageDraw.Draw(mask).rectangle(box, fill=255)
    return mask


def house(draw: ImageDraw.ImageDraw, x: int, y: int, width: int = 192, height: int = 132) -> None:
    left, top = x - width // 2, y - height // 2
    right, bottom = x + width // 2, y + height // 2
    draw.rounded_rectangle((left + 9, top + 14, right + 9, bottom + 14), radius=9, fill="#514940")
    draw.rectangle((left, top + 14, right, bottom), fill="#75675a", outline="#3b3936", width=5)
    draw.polygon([(left - 12, top + 18), (x, top - 19), (right + 12, top + 18),
                  (right, bottom - 2), (x, bottom + 20), (left, bottom - 2)],
                 fill="#747779", outline="#4c5153")
    for offset in range(-width // 2 + 20, width // 2, 24):
        draw.line((x + offset, top + 14, x + offset // 2, bottom + 8), fill="#51585a", width=3)
    draw.line((left - 8, top + 18, x, top - 15, right + 8, top + 18), fill="#a3a4a1", width=4)
    draw.line((left, bottom - 1, right, bottom - 1), fill="#7d3930", width=6)


def gatehouse(image: Image.Image, x: int, y: int, degrees: int, size: int) -> None:
    source = Image.open(HERE / "Gatehouse_Source.png").convert("RGBA")
    bbox = source.getchannel("A").getbbox()
    if bbox is None:
        return
    source = source.crop(bbox)
    source.thumbnail((size, size), Image.Resampling.LANCZOS)
    if degrees:
        source = source.rotate(degrees, expand=True, resample=Image.Resampling.BICUBIC)
    image.alpha_composite(source, (x - source.width // 2, y - source.height // 2))


def build() -> None:
    terrain = Image.open(TERRAIN).convert("RGBA")
    grass = [terrain.crop((x * 128, y * 128, (x + 1) * 128, (y + 1) * 128))
             for x, y in [(0, 0), (1, 0), (0, 2)]]
    water = terrain.crop((2 * 128, 4 * 128, 3 * 128, 5 * 128))
    earth = terrain.crop((5 * 128, 0, 6 * 128, 128))
    stone = seamless_stone()
    image = Image.new("RGBA", (WIDTH, HEIGHT), "#82913b")
    for y in range(0, HEIGHT, 128):
        for x in range(0, WIDTH, 128):
            selector = RNG.randrange(16)
            image.alpha_composite(grass[2 if selector == 0 else 1 if selector < 5 else 0], (x, y))

    # A water ring represents the moat; the four land-gate approaches bridge it.
    for box in [(0, 0, WIDTH, TILE), (0, HEIGHT - TILE, WIDTH, HEIGHT),
                (0, 0, TILE, HEIGHT), (WIDTH - TILE, 0, WIDTH, HEIGHT)]:
        repeat_into(image, water, box)
    draw = ImageDraw.Draw(image)
    draw.rectangle((TILE - 14, TILE - 14, WIDTH - TILE + 14, HEIGHT - TILE + 14),
                   outline="#394442", width=24)
    draw.rectangle((TILE - 2, TILE - 2, WIDTH - TILE + 2, HEIGHT - TILE + 2),
                   outline="#9e946e", width=14)

    earth_pattern = pattern(earth)
    road_mask = Image.new("L", (WIDTH, HEIGHT))
    road_draw = ImageDraw.Draw(road_mask)
    road_draw.line((WIDTH // 2, TILE, WIDTH // 2, HEIGHT - TILE), fill=255, width=174)
    road_draw.line((TILE, HEIGHT // 2, WIDTH - TILE, HEIGHT // 2), fill=255, width=174)
    image.paste(earth_pattern, (0, 0), road_mask)
    draw = ImageDraw.Draw(image)
    draw.rectangle((TILE * 2, TILE * 2, WIDTH - TILE * 2, HEIGHT - TILE * 2),
                   outline="#9a8057", width=8)

    # The two north-south waterways follow the documented inner-canal layout.
    canal_centers = (TILE * 3 + TILE // 2, TILE * 7 + TILE // 2)
    water_pattern = pattern(water)
    canal_mask = Image.new("L", (WIDTH, HEIGHT))
    canal_draw = ImageDraw.Draw(canal_mask)
    for center in canal_centers:
        canal_draw.line((center, TILE * 2, center, HEIGHT - TILE * 2), fill=255, width=94)
    image.paste(water_pattern, (0, 0), canal_mask)
    draw = ImageDraw.Draw(image)
    for center in canal_centers:
        for bank_x in (center - 55, center + 55):
            draw.line((bank_x, TILE * 2, bank_x, HEIGHT - TILE * 2), fill="#5e6050", width=12)
            draw.line((bank_x + (5 if bank_x < center else -5), TILE * 2,
                       bank_x + (5 if bank_x < center else -5), HEIGHT - TILE * 2),
                      fill="#b5aa80", width=5)

    stone_pattern = pattern(stone)
    # Three cross streets use short stone bridges over both canals.
    for center in canal_centers:
        for bridge_y in (TILE * 4 + TILE // 2, HEIGHT // 2, TILE * 12 + TILE // 2):
            box = (center - 80, bridge_y - 70, center + 80, bridge_y + 70)
            image.paste(stone_pattern.crop(box), box[:2])
            draw = ImageDraw.Draw(image)
            draw.rectangle(box, outline="#4f4d47", width=10)
            draw.line((box[0], box[1] + 17, box[2], box[1] + 17), fill="#c8bb99", width=7)
            draw.line((box[0], box[3] - 17, box[2], box[3] - 17), fill="#6f6a5d", width=7)

    # Small lanes, dwellings and courtyard gardens sit inside the ramparts.
    draw = ImageDraw.Draw(image)
    for lane_y in (TILE * 4 + TILE // 2, TILE * 12 + TILE // 2):
        draw.line((TILE * 2, lane_y, WIDTH - TILE * 2, lane_y), fill="#a18a62", width=35)
    for x, y in [(665, 920), (1120, 910), (1700, 920), (2140, 920),
                 (665, 1430), (1130, 1480), (1690, 1480), (2140, 1430),
                 (665, 2870), (1120, 2860), (1700, 2860), (2140, 2870),
                 (665, 3390), (1130, 3370), (1690, 3370), (2140, 3390)]:
        house(draw, x, y, 155 if x in (665, 2140) else 198)
    for x, y in [(1180, 1790), (1660, 2580), (1160, 3310), (1650, 1120)]:
        draw.ellipse((x - 38, y - 38, x + 38, y + 38), fill="#807f5a", outline="#485e48", width=8)
        draw.ellipse((x - 20, y - 20, x + 20, y + 20), fill="#445948")

    # The raised, one-cell-wide stone platform is assembled as a closed ring.
    wall_mask = Image.new("L", (WIDTH, HEIGHT))
    wall_draw = ImageDraw.Draw(wall_mask)
    wall_draw.rectangle((TILE, TILE, WIDTH - TILE - 1, HEIGHT - TILE - 1), fill=255)
    wall_draw.rectangle((TILE * 2, TILE * 2, WIDTH - TILE * 2 - 1, HEIGHT - TILE * 2 - 1), fill=0)
    shadow = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.rectangle((TILE + 20, TILE + 28, WIDTH - TILE + 24, HEIGHT - TILE + 25),
                          outline="#242e2d", width=58)
    image.alpha_composite(shadow)
    image.paste(stone_pattern, (0, 0), wall_mask)
    draw = ImageDraw.Draw(image)
    outer = (TILE, TILE, WIDTH - TILE, HEIGHT - TILE)
    inner = (TILE * 2, TILE * 2, WIDTH - TILE * 2, HEIGHT - TILE * 2)
    draw.rectangle(outer, outline="#393c3d", width=24)
    draw.rectangle((outer[0] + 19, outer[1] + 19, outer[2] - 19, outer[3] - 19),
                   outline="#c9c0af", width=8)
    draw.rectangle(inner, outline="#4f4a45", width=17)
    draw.rectangle((inner[0] + 18, inner[1] + 18, inner[2] - 18, inner[3] - 18),
                   outline="#8c867b", width=7)

    # Merlons are evenly spaced across the outer parapet, leaving gate openings.
    gate_span_x = range(TILE * 5 - 112, TILE * 5 + 113)
    gate_span_y = range(TILE * 8 + TILE // 2 - 112, TILE * 8 + TILE // 2 + 113)
    for x in range(TILE + 25, WIDTH - TILE - 25, 66):
        if x in gate_span_x:
            continue
        for y in (TILE - 20, HEIGHT - TILE - 17):
            draw.rounded_rectangle((x - 22, y - 17, x + 22, y + 19), radius=5,
                                   fill="#bcb6a9", outline="#45484a", width=5)
    for y in range(TILE + 25, HEIGHT - TILE - 25, 66):
        if y in gate_span_y:
            continue
        for x in (TILE - 20, WIDTH - TILE - 17):
            draw.rounded_rectangle((x - 17, y - 22, x + 19, y + 22), radius=5,
                                   fill="#bcb6a9", outline="#45484a", width=5)

    # Twelve guard platforms: four corners and two additional ones on each side.
    tower_cells = [(1, 1), (9, 1), (1, 15), (9, 15),
                   (3, 1), (7, 1), (3, 15), (7, 15),
                   (1, 5), (1, 11), (9, 5), (9, 11)]
    for x, y in tower_cells:
        cx, cy = x * TILE + TILE // 2, y * TILE + TILE // 2
        draw.ellipse((cx - 97, cy - 97, cx + 97, cy + 97),
                     fill="#81817c", outline="#373b3d", width=11)
        draw.ellipse((cx - 78, cy - 78, cx + 78, cy + 78),
                     fill="#aba99e", outline="#d2cab8", width=5)
        draw.ellipse((cx - 57, cy - 57, cx + 57, cy + 57),
                     fill="#898981", outline="#62635f", width=6)

    # The east gate projects into a compact barbican beyond the main wall.
    barbican = (WIDTH - TILE - 16, HEIGHT // 2 - 320, WIDTH - 17, HEIGHT // 2 + 320)
    draw.rounded_rectangle(barbican, radius=76, fill="#595a54", outline="#262d30", width=17)
    draw.rounded_rectangle((barbican[0] + 24, barbican[1] + 24,
                            barbican[2] - 24, barbican[3] - 24),
                           radius=55, fill="#9d988a", outline="#d1c5ae", width=9)
    draw.line((barbican[0] + 30, HEIGHT // 2, WIDTH, HEIGHT // 2),
              fill="#b2a58d", width=108)
    for y in range(barbican[1] + 35, barbican[3] - 30, 76):
        draw.rectangle((barbican[2] - 29, y - 19, barbican[2] + 5, y + 19),
                       fill="#c5bdad", outline="#444646", width=4)

    # Bridges lead to the four land gates through the moat.
    north_bridge = (WIDTH // 2 - 110, 0, WIDTH // 2 + 110, TILE + 55)
    south_bridge = (WIDTH // 2 - 110, HEIGHT - TILE - 55, WIDTH // 2 + 110, HEIGHT)
    west_bridge = (0, HEIGHT // 2 - 110, TILE + 55, HEIGHT // 2 + 110)
    for box in (north_bridge, south_bridge, west_bridge):
        image.paste(stone_pattern.crop(box), box[:2])
        draw = ImageDraw.Draw(image)
        draw.rectangle(box, outline="#353b3c", width=13)

    for x, y, rotation, size in [
        (WIDTH // 2, TILE + TILE // 2, 180, 350),
        (WIDTH // 2, HEIGHT - TILE - TILE // 2, 0, 350),
        (TILE + TILE // 2, HEIGHT // 2, 90, 350),
        (WIDTH - TILE - TILE // 2, HEIGHT // 2, -90, 400),
    ]:
        gatehouse(image, x, y, rotation, size)

    # Small stone circles make deployment slots visible without obstructing units.
    gate_cells = {(5, 1), (5, 15), (1, 8), (9, 8)}
    deployable = []
    draw = ImageDraw.Draw(image)
    for y in range(1, 16):
        for x in range(1, 10):
            wall = x in (1, 9) or y in (1, 15)
            if not wall or (x, y) in gate_cells:
                continue
            deployable.append({"x": x - 5, "y": y - 8})
            if (x, y) in tower_cells:
                continue
            cx, cy = x * TILE + TILE // 2, y * TILE + TILE // 2
            draw.ellipse((cx - 28, cy - 28, cx + 28, cy + 28),
                         outline="#c6b891", width=5)
            draw.ellipse((cx - 12, cy - 12, cx + 12, cy + 12), fill="#847a61")

    image.convert("RGB").save(HERE / "YongchangFortress_Atlas.png", optimize=True)
    image.resize((704, 1088), Image.Resampling.LANCZOS).convert("RGB").save(
        HERE / "YongchangFortress_Preview.png", optimize=True
    )
    metadata = {
        "inspiration": "Yongchangpu / 永昌堡, Wenzhou; stylized game map, not a measured reconstruction",
        "tile_size": TILE,
        "atlas_columns": COLS,
        "atlas_rows": ROWS,
        "map_cell_origin": [-5, -8],
        "deployment_cells": deployable,
        "gate_cells": [{"x": x - 5, "y": y - 8} for x, y in sorted(gate_cells)],
    }
    (HERE / "YongchangFortress_MapData.json").write_text(
        json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"atlas={WIDTH}x{HEIGHT}, cells={COLS * ROWS}, soldier_slots={len(deployable)}")


if __name__ == "__main__":
    build()
