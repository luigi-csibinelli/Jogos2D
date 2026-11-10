"""Gera toda a pixel art do jogo em Assets/Resources/Art (paleta inspirada no NES).

Tudo é desenhado aqui, em código: tiles, personagens, armas, interface e a
fonte 5x7. Nenhum asset externo. Rode: python3 Tools/gerar_sprites.py
"""
import math
import os
import random

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', 'Assets', 'Resources', 'Art')
os.makedirs(OUT, exist_ok=True)
random.seed(1945)

# ------------------------------------------------------------------ paleta
C = {
    'k': (0, 0, 0, 255), 'K': (24, 20, 28, 255), 'd': (52, 48, 56, 255), 'D': (80, 76, 84, 255),
    'g': (124, 124, 124, 255), 'G': (188, 188, 188, 255), 'w': (248, 248, 248, 255), 'W': (236, 230, 214, 255),
    'r': (168, 16, 0, 255), 'R': (228, 40, 24, 255), 'o': (228, 92, 16, 255), 'O': (252, 160, 68, 255),
    'y': (248, 184, 0, 255), 'Y': (248, 216, 120, 255), 'b': (136, 72, 24, 255), 'B': (172, 112, 48, 255),
    'n': (88, 52, 20, 255), 'N': (60, 36, 16, 255), 'e': (0, 120, 0, 255), 'E': (0, 184, 0, 255),
    'l': (184, 248, 24, 255), 'L': (88, 216, 84, 255), 'v': (80, 96, 40, 255), 'V': (112, 128, 56, 255),
    'u': (0, 88, 248, 255), 'U': (60, 188, 252, 255), 'i': (164, 228, 252, 255), 'p': (104, 68, 252, 255),
    'P': (152, 120, 248, 255), 's': (172, 196, 160, 255), 'S': (140, 164, 128, 255), 'h': (96, 112, 88, 255),
    'c': (40, 64, 88, 255), 'C': (0, 136, 136, 255), 'm': (148, 0, 132, 255), 'M': (248, 88, 152, 255),
    '.': (0, 0, 0, 0),
}


def new(w, h):
    return Image.new('RGBA', (w, h), (0, 0, 0, 0))


def save(img, name):
    img.save(os.path.join(OUT, name + '.png'))


def grid(rows, pal=C):
    h = len(rows)
    w = max(len(r) for r in rows)
    img = new(w, h)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != '.' and ch != ' ':
                img.putpixel((x, y), pal[ch])
    return img


def px(img, x, y, col):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), C[col] if isinstance(col, str) else col)


def rect(img, x0, y0, x1, y1, col):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px(img, x, y, col)


def noise_fill(img, base, alts, chance=0.12):
    for y in range(img.height):
        for x in range(img.width):
            px(img, x, y, random.choice(alts) if random.random() < chance else base)


def outline(img, col='k'):
    """Contorno de 1 px em volta dos pixels opacos (legibilidade no escuro)."""
    src = img.copy()
    for y in range(img.height):
        for x in range(img.width):
            if src.getpixel((x, y))[3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.width and 0 <= yy < img.height and src.getpixel((xx, yy))[3] > 0:
                    img.putpixel((x, y), C[col])
                    break
    return img


# ================================================================== tiles
def tiles():
    # ladrilho de enfermaria (xadrez claro, rejunte)
    for name, a, b in (('FloorTileA', 'G', 'g'), ('FloorTileB', 'W', 'G')):
        img = new(16, 16)
        rect(img, 0, 0, 15, 15, a)
        for i in range(16):
            px(img, i, 15, 'g'); px(img, 15, i, 'g')
        for _ in range(6):
            px(img, random.randrange(15), random.randrange(15), b)
        # manchas de uso
        if name == 'FloorTileA':
            px(img, 4, 9, 'g'); px(img, 5, 9, 'g')
        save(img, name)

    for name, base in (('FloorWoodA', 'b'), ('FloorWoodB', 'B')):
        img = new(16, 16)
        for y in range(16):
            for x in range(16):
                px(img, x, y, base if (y // 4) % 2 == 0 else ('B' if base == 'b' else 'b'))
        for y in (3, 7, 11, 15):
            for x in range(16):
                px(img, x, y, 'n')
        for y, x in ((1, 5), (5, 12), (9, 2), (13, 9)):
            px(img, x, y, 'N')
            px(img, x, y + 1, 'n')
        save(img, name)

    for name, base, alt in (('FloorCarpetA', 'r', 'n'), ('FloorCarpetB', 'r', 'b')):
        img = new(16, 16)
        noise_fill(img, base, [alt, 'N'], 0.18)
        for i in range(16):
            px(img, i, 0, 'n')
        if name == 'FloorCarpetA':
            for i in range(2, 14, 4):
                px(img, i, 7, 'y'); px(img, i + 1, 8, 'y')
        save(img, name)

    img = new(16, 16)
    noise_fill(img, 'D', ['d', 'g'], 0.2)
    for i in range(0, 16, 8):
        for j in range(16):
            px(img, i, j, 'd')
    save(img, 'FloorConcrete')

    for name, a, b in (('FloorMorgueA', 'S', 'h'), ('FloorMorgueB', 's', 'S')):
        img = new(16, 16)
        rect(img, 0, 0, 15, 15, a)
        for i in range(16):
            px(img, i, 7, 'h'); px(img, i, 15, 'h'); px(img, 7, i, 'h'); px(img, 15, i, 'h')
        for _ in range(4):
            px(img, random.randrange(15), random.randrange(15), b)
        save(img, name)

    for name, a, b in (('FloorKitchenA', 'w', 'k'), ('FloorKitchenB', 'k', 'w')):
        img = new(16, 16)
        for y in range(16):
            for x in range(16):
                px(img, x, y, a if ((x // 8) + (y // 8)) % 2 == 0 else b)
        for _ in range(8):
            px(img, random.randrange(16), random.randrange(16), 'g')
        save(img, name)

    img = new(16, 16)
    for y in range(16):
        for x in range(16):
            px(img, x, y, 'B' if (y % 4) < 2 else 'b')
        if y % 4 == 3:
            for x in range(16):
                px(img, x, y, 'N')
    for y in range(16):
        px(img, 0, y, 'n'); px(img, 15, y, 'n')
    save(img, 'Stairs')

    img = new(16, 16)
    rect(img, 0, 0, 15, 15, 'd')
    for i in range(0, 16, 3):
        for j in range(16):
            px(img, i, j, 'g')
            px(img, j, i, 'D')
    for i in (0, 15):
        for j in range(16):
            px(img, j, i, 'y' if (j // 2) % 2 == 0 else 'k')
    save(img, 'TrapGrate')

    # parede vista de cima (telhado) e com face de tijolo
    img = new(16, 16)
    noise_fill(img, 'D', ['d', 'g'], 0.15)
    for i in range(16):
        px(img, i, 0, 'g')
    save(img, 'WallTop')

    img = new(16, 16)
    rect(img, 0, 0, 15, 15, 'b')
    for y in range(16):
        off = 0 if (y // 4) % 2 == 0 else 4
        for x in range(16):
            if y % 4 == 3 or (x + off) % 8 == 0:
                px(img, x, y, 'n')
            elif random.random() < 0.1:
                px(img, x, y, 'B')
    for x in range(16):
        px(img, x, 0, 'D'); px(img, x, 1, 'D'); px(img, x, 2, 'g')
    for x in range(16):
        px(img, x, 15, 'N')
    save(img, 'WallFace')

    for name, extra in (('GrassA', 0), ('GrassB', 1), ('GrassFlower', 2)):
        img = new(16, 16)
        noise_fill(img, 'e', ['E', 'v', 'k'], 0.22 if extra == 1 else 0.12)
        if extra == 2:
            for x, y in ((4, 5), (11, 10), (7, 13)):
                px(img, x, y, 'M'); px(img, x + 1, y, 'y'); px(img, x, y + 1, 'M')
        save(img, name)

    # janela: abertura escura com moldura
    img = new(16, 16)
    rect(img, 0, 0, 15, 15, 'n')
    rect(img, 2, 2, 13, 13, 'c')
    rect(img, 3, 3, 12, 12, 'K')
    for x, y in ((4, 4), (11, 5), (6, 10)):
        px(img, x, y, 'i')
    save(img, 'WindowFrame')

    img = new(14, 3)
    rect(img, 0, 0, 13, 2, 'B')
    rect(img, 0, 2, 13, 2, 'n')
    px(img, 1, 1, 'G'); px(img, 12, 1, 'G')
    save(outline(expand(img, 1)), 'Plank')

    # porta de tábuas com corrente
    img = new(16, 16)
    for x in range(16):
        for y in range(16):
            px(img, x, y, 'B' if (x // 4) % 2 == 0 else 'b')
        if x % 4 == 3:
            for y in range(16):
                px(img, x, y, 'n')
    for x in range(16):
        px(img, x, 3, 'N'); px(img, x, 12, 'N')
    for i in range(16):
        px(img, i, i, 'G' if i % 2 == 0 else 'g')
    save(img, 'Door')

    img = new(16, 16)
    rect(img, 0, 0, 15, 15, 'd')
    for x in range(16):
        px(img, x, 0, 'g'); px(img, x, 15, 'K')
    for y in range(2, 14):
        px(img, 7, y, 'u'); px(img, 8, y, 'U')
    for x, y in ((9, 4), (6, 7), (9, 10)):
        px(img, x, y, 'i')
    for x in range(0, 16, 4):
        px(img, x, 2, 'y'); px(img, x + 1, 13, 'y')
    save(img, 'DoorPower')


def expand(img, pad):
    out = new(img.width + pad * 2, img.height + pad * 2)
    out.paste(img, (pad, pad))
    return out


# ================================================================== objetos
def objects():
    bed = grid([
        "..gggggggggggg..",
        ".gWWWWWWWWWWWWg.",
        ".gWiiiiWWWWWWWg.",
        ".gWiiiiWWWWWWWg.",
        ".gWWWWWWWWWWWWg.",
        ".gWWWWWWWWWWWWg.",
        ".gWWGGGGGGGGWWg.",
        ".gWWGGGGGGGGWWg.",
        ".gWWGGGGGGGGWWg.",
        ".gWWGGGGGGGGWWg.",
        ".gWWGGGGGGGGWWg.",
        ".gWWGGGGrGGGWWg.",
        ".gWWGGGGGGGGWWg.",
        ".gWWWWWWWWWWWWg.",
        "..gggggggggggg..",
        "..d..........d..",
    ])
    save(bed, 'Bed')

    table = new(16, 16)
    rect(table, 1, 2, 14, 13, 'b')
    rect(table, 1, 2, 14, 2, 'B')
    rect(table, 1, 13, 14, 13, 'n')
    px(table, 5, 6, 'G'); px(table, 6, 6, 'G'); px(table, 10, 9, 'W'); px(table, 11, 9, 'g')
    px(table, 8, 5, 'R')
    save(outline(table), 'Table')

    toilet = grid([
        "................",
        "....GGGGGGGG....",
        "....GwwwwwwG....",
        "....GGGGGGGG....",
        ".....GwwwwG.....",
        "....GwwwwwwG....",
        "...GwwccccwwG...",
        "...GwcCCCCcwG...",
        "...GwcCCCCcwG...",
        "...GwwccccwwG...",
        "....GwwwwwwG....",
        ".....GGGGGG.....",
        "................",
    ])
    save(outline(toilet), 'Toilet')

    lamp = new(16, 16)
    for y in range(16):
        for x in range(16):
            d = math.hypot(x - 7.5, y - 7.5)
            if d < 2.5:
                px(lamp, x, y, 'Y')
            elif d < 3.5:
                px(lamp, x, y, 'y')
    px(lamp, 7, 7, 'w'); px(lamp, 8, 8, 'w')
    save(lamp, 'Lamp')

    note = grid([
        "...WWWWWWWW.",
        "...WgggggWW.",
        "...WWWWWWWW.",
        "...WgggWgWW.",
        "...WWWWWWWW.",
        "...WggggggW.",
        "...WWWWWWWW.",
        "...WgggWWrW.",
        "...WWWWWWWW.",
    ])
    save(outline(note), 'Note')

    spot = new(16, 16)
    for y in range(16):
        for x in range(16):
            d = math.hypot((x - 7.5) / 1.3, y - 8.5)
            if d < 4.5:
                px(spot, x, y, 'n' if random.random() < 0.6 else 'N')
    rect(spot, 3, 7, 10, 8, 'b')
    px(spot, 11, 6, 'B')
    save(spot, 'BoxSpot')

    def box(open_):
        img = new(16, 16)
        rect(img, 0, 3, 15, 12, 'n')
        rect(img, 1, 4, 14, 11, 'b')
        for x in range(1, 15):
            if x % 5 == 0:
                for y in range(4, 12):
                    px(img, x, y, 'N')
        rect(img, 0, 3, 15, 3, 'y')
        rect(img, 0, 12, 15, 12, 'y')
        if open_:
            rect(img, 2, 5, 13, 10, 'Y')
            rect(img, 3, 6, 12, 9, 'w')
        else:
            for qx in (3, 10):
                for (dx, dy) in ((0, 0), (1, 0), (2, 1), (1, 2), (1, 4)):
                    px(img, qx + dx, 5 + dy, 'W')
        return img

    save(box(False), 'Box')
    save(box(True), 'BoxOpen')

    teddy = grid([
        "..bb....bb..",
        ".bBBb..bBBb.",
        ".bBBbbbbBBb.",
        "..bBBBBBBb..",
        "..bBkBBkBb..",
        "..bBBBBBBb..",
        "..bBBYYBBb..",
        "...bBkkBb...",
        "....bbbb....",
    ])
    save(outline(teddy), 'Teddy')

    for state in ('Off', 'On'):
        img = new(16, 16)
        rect(img, 3, 1, 12, 14, 'g')
        rect(img, 4, 2, 11, 13, 'd')
        rect(img, 7, 4, 8, 11, 'G')
        if state == 'On':
            rect(img, 5, 3, 10, 5, 'G')
            rect(img, 6, 3, 9, 4, 'w')
            px(img, 5, 12, 'E'); px(img, 6, 12, 'E')
        else:
            rect(img, 5, 10, 10, 12, 'G')
            rect(img, 6, 11, 9, 12, 'w')
            px(img, 5, 3, 'R'); px(img, 6, 3, 'R')
        save(outline(img), 'Breaker_' + state)

    for state in ('Off', 'On'):
        img = new(16, 16)
        rect(img, 5, 4, 10, 11, 'g')
        rect(img, 6, 5, 9, 10, 'd')
        if state == 'On':
            rect(img, 7, 1, 8, 6, 'G'); rect(img, 6, 0, 9, 1, 'R')
        else:
            rect(img, 7, 9, 8, 14, 'G'); rect(img, 6, 14, 9, 15, 'R')
        px(img, 5, 4, 'y'); px(img, 10, 11, 'y')
        save(outline(img), 'Lever_' + state)

    arc = new(16, 16)
    pts = [(0, 8), (3, 4), (6, 11), (9, 3), (12, 12), (15, 7)]
    d = ImageDraw.Draw(arc)
    for a, b in zip(pts, pts[1:]):
        d.line([a, b], fill=C['i'], width=1)
    for a in pts:
        px(arc, a[0], a[1], 'w')
    save(arc, 'Arc')

    # refrigerantes (marcas próprias): máquina vista de cima, apagada / acesa
    perks = {
        'FerroNog': ('r', 'R'),
        'ReviveJa': ('u', 'U'),
        'TurboCola': ('e', 'L'),
        'GatilhoDuplo': ('o', 'y'),
    }
    for p, (dark, light) in perks.items():
        for state in ('Off', 'On'):
            img = new(16, 16)
            rect(img, 1, 1, 14, 14, 'K')
            rect(img, 2, 2, 13, 13, dark)
            rect(img, 2, 2, 13, 4, 'k' if state == 'Off' else light)
            rect(img, 4, 6, 11, 11, 'K' if state == 'Off' else 'w')
            if state == 'On':
                rect(img, 5, 7, 10, 10, light)
                px(img, 3, 13, 'w'); px(img, 12, 13, 'w')
            rect(img, 6, 12, 9, 13, 'G')
            save(img, f'Perk{p}_{state}')
        save(perk_icon(p, dark, light), 'Perk' + p)


def perk_icon(p, dark, light):
    img = new(16, 16)
    for y in range(16):
        for x in range(16):
            dd = math.hypot(x - 7.5, y - 7.5)
            if dd < 7.5:
                px(img, x, y, 'k')
            if dd < 6.6:
                px(img, x, y, dark)
            if dd < 5.2:
                px(img, x, y, light)
    if p == 'FerroNog':      # escudo
        for y, row in enumerate(["#######", "#######", "#######", ".#####.", ".#####.", "..###..", "...#..."]):
            for x, ch in enumerate(row):
                if ch == '#':
                    px(img, 4 + x, 4 + y, 'w')
        rect(img, 7, 5, 7, 8, dark)
    elif p == 'ReviveJa':    # cruz
        rect(img, 7, 3, 8, 12, 'w')
        rect(img, 3, 7, 12, 8, 'w')
    elif p == 'TurboCola':   # raio
        for y, row in enumerate(["...##", "..##.", ".##..", "#####", "..##.", ".##..", "##..."]):
            for x, ch in enumerate(row):
                if ch == '#':
                    px(img, 5 + x, 4 + y, 'w')
    else:                    # duas balas
        for bx in (4, 9):
            rect(img, bx, 6, bx + 2, 11, 'w')
            rect(img, bx, 4, bx + 2, 5, 'Y')
            px(img, bx + 1, 3, 'Y')
    return img


# ================================================================== personagens (de cima, olhando para a direita)
# arma que o soldado segura, vista de cima e apontada para a direita (por classe)
HELD_KINDS = ('pistol', 'rifle', 'smg', 'shotgun', 'lmg', 'launcher', 'flame', 'ray')


def draw_held(img, kind, dark, skin):
    if kind == 'launcher':
        # tubo apoiado no ombro direito (embaixo, na vista de cima), mãos à frente
        rect(img, 8, 9, 10, 9, dark); rect(img, 8, 6, 10, 6, dark)
        rect(img, 1, 9, 15, 11, 'v'); rect(img, 1, 10, 15, 10, 'V')
        rect(img, 14, 9, 15, 11, 'd'); rect(img, 1, 9, 2, 11, 'K')
        px(img, 11, 6, skin); px(img, 11, 9, skin)
        return
    if kind == 'flame':
        # tanques nas costas + lança na frente
        rect(img, 1, 5, 3, 7, 'R'); rect(img, 1, 8, 3, 10, 'R'); rect(img, 1, 7, 3, 8, 'r')
        rect(img, 8, 6, 10, 6, dark); rect(img, 8, 9, 10, 9, dark)
        rect(img, 9, 7, 15, 8, 'g'); rect(img, 14, 6, 15, 9, 'D'); px(img, 15, 7, 'o'); px(img, 15, 8, 'O')
        px(img, 11, 6, skin); px(img, 12, 9, skin)
        return
    if kind in ('pistol', 'ray'):
        # braços esticados, as duas mãos juntas na arma curta
        rect(img, 8, 6, 12, 6, dark); rect(img, 8, 9, 12, 9, dark)
        px(img, 12, 7, skin); px(img, 12, 8, skin)
        if kind == 'pistol':
            rect(img, 13, 7, 15, 8, 'k'); px(img, 13, 8, 'd')
        else:
            rect(img, 13, 6, 14, 9, 'R'); px(img, 15, 7, 'L'); px(img, 15, 8, 'l')
        return
    # armas longas: braço esquerdo apoia o cano, direito no gatilho
    rect(img, 8, 6, 11, 6, dark); rect(img, 8, 9, 11, 9, dark)
    if kind == 'rifle':
        rect(img, 8, 7, 11, 8, 'n'); rect(img, 12, 7, 15, 7, 'k'); rect(img, 12, 8, 14, 8, 'B')
    elif kind == 'smg':
        rect(img, 9, 7, 14, 8, 'd'); px(img, 15, 7, 'k'); rect(img, 12, 9, 12, 11, 'k')
    elif kind == 'shotgun':
        rect(img, 8, 7, 10, 8, 'n'); rect(img, 11, 6, 15, 7, 'k'); rect(img, 11, 8, 15, 8, 'k'); rect(img, 12, 9, 13, 9, 'B')
    elif kind == 'lmg':
        rect(img, 8, 7, 15, 8, 'd'); rect(img, 13, 7, 15, 7, 'k'); rect(img, 10, 9, 12, 11, 'y'); px(img, 11, 10, 'o')
    px(img, 11, 6, skin); px(img, 12, 9, skin)


def character(body, dark, skin, helmet=None, frame=0, attack=False, gun=False, arms_fwd=False, held=None):
    img = new(16, 16)
    sway = 1 if frame else -1
    # pés / pernas (aparecem atrás quando anda)
    rect(img, 3 + sway, 5, 5 + sway, 6, 'K')
    rect(img, 3 - sway, 9, 5 - sway, 10, 'K')
    # ombros / tronco
    for y in range(3, 13):
        for x in range(4, 11):
            dd = ((x - 7) / 3.5) ** 2 + ((y - 7.5) / 5.0) ** 2
            if dd <= 1.0:
                px(img, x, y, body if dd < 0.6 else dark)
    if gun or held:
        draw_held(img, held or 'rifle', dark, skin)
    elif attack:
        rect(img, 8, 2, 13, 3, dark); rect(img, 8, 12, 13, 13, dark)
        px(img, 14, 2, skin); px(img, 14, 3, skin); px(img, 14, 12, skin); px(img, 14, 13, skin)
    elif arms_fwd:
        off = 1 if frame else 0
        rect(img, 8, 4 + off, 13, 5 + off, dark); rect(img, 8, 10 - off, 13, 11 - off, dark)
        px(img, 14, 4 + off, skin); px(img, 14, 11 - off, skin)
    # cabeça
    for y in range(5, 11):
        for x in range(5, 11):
            if math.hypot(x - 7.5, y - 7.5) < 2.9:
                px(img, x, y, skin)
    if helmet:
        for y in range(4, 12):
            for x in range(4, 11):
                dd = math.hypot(x - 7.0, y - 7.5)
                if dd < 3.4 and x <= 8:
                    px(img, x, y, helmet)
        px(img, 5, 6, 'V'); px(img, 5, 9, 'v')
    else:
        # cabelo ralo de cima
        for y in range(5, 11):
            for x in range(5, 8):
                if math.hypot(x - 7.5, y - 7.5) < 2.9 and random.random() < 0.5:
                    px(img, x, y, 'h')
    return outline(img)


def characters():
    # Cabo Ramos (personagem original): capacete e farda verde-oliva
    for kind in HELD_KINDS:
        for f in (0, 1):
            save(character('V', 'v', 'O', helmet='h', frame=f, held=kind), f'Player_{kind}_{f}')
    for f in (0, 1):
        save(character('V', 'v', 'O', helmet='h', frame=f, held='rifle'), f'Player_{f}')
    down = new(16, 16)
    for y in range(4, 13):
        for x in range(2, 14):
            if ((x - 8) / 6) ** 2 + ((y - 8) / 3.5) ** 2 <= 1:
                px(down, x, y, 'V')
    for y in range(6, 11):
        for x in range(1, 6):
            if math.hypot(x - 3, y - 8) < 2.6:
                px(down, x, y, 'h')
    rect(down, 9, 12, 15, 13, 'k')
    save(outline(down), 'Player_Down')

    # zumbis: paciente (camisola), enfermeiro (verde-água), soldado (cinza, sem insígnias)
    variants = [('W', 'G', 's'), ('C', 'c', 'S'), ('g', 'D', 's')]
    for i, (body, dark, skin) in enumerate(variants):
        for f in (0, 1):
            random.seed(100 + i)
            save(character(body, dark, skin, frame=f, arms_fwd=True), f'Zombie{i}_{f}')
            # olhos vermelhos
            img = Image.open(os.path.join(OUT, f'Zombie{i}_{f}.png'))
            px(img, 9, 6, 'R'); px(img, 9, 9, 'R')
            save(img, f'Zombie{i}_{f}')
        random.seed(100 + i)
        att = character(body, dark, skin, attack=True)
        px(att, 9, 6, 'R'); px(att, 9, 9, 'R')
        save(att, f'Zombie{i}_Attack')

        for f in (0, 1):
            img = new(16, 16)
            for y in range(5, 11):
                for x in range(3, 9):
                    if ((x - 6) / 3) ** 2 + ((y - 7.5) / 3.2) ** 2 <= 1:
                        px(img, x, y, body)
            off = f
            rect(img, 7, 4 + off, 12, 5 + off, dark); rect(img, 7, 10 - off, 12, 11 - off, dark)
            for y in range(6, 10):
                for x in range(7, 11):
                    if math.hypot(x - 8.5, y - 7.5) < 2.2:
                        px(img, x, y, skin)
            rect(img, 1, 6, 3, 9, 'r')
            px(img, 10, 6, 'R'); px(img, 10, 9, 'R')
            save(outline(img), f'Crawler{i}_{f}')

        corpse = new(16, 16)
        for y in range(3, 13):
            for x in range(1, 15):
                if ((x - 8) / 6.5) ** 2 + ((y - 8) / 4) ** 2 <= 1:
                    px(corpse, x, y, body if random.random() > 0.15 else 'r')
        for y in range(6, 11):
            for x in range(10, 15):
                if math.hypot(x - 12, y - 8) < 2.4:
                    px(corpse, x, y, skin)
        save(outline(corpse, 'K'), f'Corpse{i}')

    slash = new(16, 16)
    for a in range(-60, 61, 4):
        r = 6.5
        x = int(4 + r * math.cos(math.radians(a)))
        y = int(8 + r * math.sin(math.radians(a)))
        px(slash, x, y, 'w')
        px(slash, x - 1, y, 'G')
    save(slash, 'KnifeSlash')


# ================================================================== efeitos
def effects():
    img = new(2, 2)
    rect(img, 0, 0, 1, 1, 'w')
    save(img, 'Px')

    for i in range(3):
        img = new(16, 16)
        random.seed(300 + i)
        for _ in range(4):
            cx, cy, r = random.uniform(4, 12), random.uniform(4, 12), random.uniform(1.5, 3.5)
            for y in range(16):
                for x in range(16):
                    if math.hypot(x - cx, y - cy) < r:
                        px(img, x, y, (120, 10, 16, 200))
        for _ in range(6):
            px(img, random.randrange(16), random.randrange(16), (120, 10, 16, 160))
        save(img, f'Blood{i}')

    img = new(24, 24)
    for y in range(24):
        for x in range(24):
            d = math.hypot(x - 11.5, y - 11.5)
            if d < 10 and random.random() < 1.1 - d / 10:
                px(img, x, y, (20, 16, 16, int(200 * (1 - d / 11))))
    save(img, 'Scorch')

    save(outline(grid(["..LLLLL..", ".LlwwwlL.", "..LLLLL.."])), 'RayBolt')
    save(outline(grid(["oO.gggGGgw", "Oy.ggGGGGw", "oO.gggGGgw"])), 'Rocket')
    save(outline(grid(["vvVVg", "vVVVG", "vvVVg"])), 'RifleGrenade')
    save(outline(grid(["...bbb", "VVVbBb", "VVVbbb"])), 'Grenade')
    save(outline(grid(["..W...", ".eLe..", "eLLLeo", "eLLLeO", ".eLe..", "..W..."])), 'Molotov')
    save(outline(grid([".ggg.", "gDDDg", "gDRDg", "gDDDg", ".ggg."])), 'Betty')

    # power-ups (verdes, como brinquedos de lata)
    icons = {
        'InstaKill': [
            "....wwwwwww.....",
            "...wwwwwwwww....",
            "..wwwwwwwwwww...",
            "..wwkkwwwkkww...",
            "..wwkkwwwkkww...",
            "..wwwwwkwwwww...",
            "...wwwwwwwww....",
            "....wkwkwkw.....",
            "....wwwwwww.....",
        ],
        'DoublePoints': [
            "................",
            "ww...ww..wwwww..",
            ".ww.ww..ww...ww.",
            "..www.......ww..",
            "..www......ww...",
            ".ww.ww...ww.....",
            "ww...ww.wwwwwww.",
        ],
        'Nuke': [
            "........y.......",
            ".......y........",
            "......ggg.......",
            "....kkkkkkk.....",
            "...kkkkkkkkk....",
            "...kkwkkkkkk....",
            "...kkkkkkkkk....",
            "...kkkkkkkkk....",
            "....kkkkkkk.....",
        ],
        'MaxAmmo': [
            "..bbbbbbbbbbbb..",
            "..bBBBBBBBBBBb..",
            "..bBYyBYyBYyBb..",
            "..bBYyBYyBYyBb..",
            "..bBYyBYyBYyBb..",
            "..bBBBBBBBBBBb..",
            "..bbbbbbbbbbbb..",
        ],
    }
    for name, rows in icons.items():
        img = new(16, 16)
        for y in range(16):
            for x in range(16):
                d = math.hypot(x - 7.5, y - 7.5)
                if d < 7.8:
                    px(img, x, y, (40, 140, 40, 230) if d > 6.8 else (16, 64, 20, 230))
        sym = grid(rows)
        img.alpha_composite(sym, ((16 - sym.width) // 2, (16 - sym.height) // 2))
        save(img, 'PowerUp' + name)


# ================================================================== armas (ícones da caixa e giz da parede)
GUNS = {
    #  id:            (comprimento, coronha, carregador, mira, cor do corpo, extra)
    'm1911':         (9, 0, 'grip', 0, 'd', ''),
    'magnum':        (11, 0, 'grip', 0, 'G', 'cyl'),
    'kar98k':        (24, 6, 'none', 0, 'b', 'bolt'),
    'springfield':   (24, 7, 'none', 0, 'B', 'bands'),
    'gewehr43':      (22, 6, 'box', 0, 'b', ''),
    'garand':        (22, 6, 'none', 0, 'B', ''),
    'garand_m7':     (24, 6, 'none', 0, 'B', 'nade'),
    'carbine':       (19, 5, 'box', 0, 'B', ''),
    'ptrs41':        (28, 5, 'none', 1, 'v', 'muzzle'),
    'stg44':         (21, 5, 'curve', 0, 'd', ''),
    'thompson':      (20, 5, 'drum', 0, 'b', 'grip2'),
    'mp40':          (18, 0, 'long', 0, 'd', 'fold'),
    'ppsh':          (20, 6, 'drum', 0, 'b', 'shroud'),
    'trenchgun':     (22, 5, 'none', 0, 'b', 'pump'),
    'doublebarrel':  (22, 6, 'none', 0, 'b', 'double'),
    'sawedoff':      (14, 3, 'none', 0, 'b', 'double'),
    'bar':           (25, 6, 'box', 0, 'b', 'bipod'),
    'mg42':          (26, 5, 'belt', 0, 'd', 'shroud'),
    'fg42':          (22, 5, 'side', 0, 'd', ''),
    'm1919':         (26, 4, 'belt', 0, 'd', 'cover'),
    'panzerschreck': (28, 0, 'none', 0, 'v', 'tube'),
    'flamethrower':  (22, 0, 'tank', 0, 'd', 'flame'),
    'raygun':        (13, 0, 'grip', 0, 'R', 'ray'),
}


def gun_icon(gid, chalk=False):
    length, stock, mag, scope, body, extra = GUNS[gid]
    W, H = 30, 10
    img = new(W, H)
    x0 = (W - length) // 2
    steel = 'w' if chalk else 'd'
    wood = 'w' if chalk else body
    hi = 'w' if chalk else 'G'

    if extra == 'tube':
        rect(img, x0, 3, x0 + length - 1, 5, 'w' if chalk else 'v')
        rect(img, x0 + length - 3, 2, x0 + length - 1, 6, steel)
        rect(img, x0 + 8, 6, x0 + 9, 8, steel)
        rect(img, x0 + 12, 1, x0 + 13, 2, steel)
    elif extra == 'ray':
        rect(img, x0, 3, x0 + 8, 6, 'w' if chalk else 'R')
        for i, h in enumerate((2, 3, 4)):
            rect(img, x0 + 2 + i * 2, 4 - h // 2, x0 + 2 + i * 2, 5 + h // 2, hi)
        rect(img, x0 + 9, 4, x0 + length - 1, 5, 'w' if chalk else 'L')
        rect(img, x0 + 2, 7, x0 + 4, 9, steel)
    elif extra == 'flame':
        rect(img, x0, 3, x0 + length - 1, 4, steel)
        rect(img, x0 + 3, 5, x0 + 10, 8, 'w' if chalk else 'R')
        rect(img, x0 + length - 2, 2, x0 + length - 1, 5, 'w' if chalk else 'o')
    else:
        barrel_y = 3
        rect(img, x0 + stock, barrel_y, x0 + length - 1, barrel_y + 1, steel)
        if extra == 'double':
            rect(img, x0 + stock, barrel_y - 1, x0 + length - 1, barrel_y - 1, steel)
        # corpo / guarda-mão
        rect(img, x0 + stock, barrel_y + 1, x0 + int(length * 0.72), barrel_y + 2, wood)
        if stock:
            rect(img, x0, barrel_y + 1, x0 + stock, barrel_y + 3, wood)
            rect(img, x0, barrel_y + 4, x0 + 2, barrel_y + 4, wood)
        if mag == 'grip':
            rect(img, x0 + 1, barrel_y + 2, x0 + 3, barrel_y + 5, wood)
            rect(img, x0, barrel_y - 1, x0 + length - 1, barrel_y + 1, steel)
        elif mag == 'box':
            rect(img, x0 + stock + 4, barrel_y + 3, x0 + stock + 6, barrel_y + 5, steel)
        elif mag == 'curve':
            for i in range(4):
                rect(img, x0 + stock + 4 + i // 2, barrel_y + 3 + i, x0 + stock + 6 + i // 2, barrel_y + 3 + i, steel)
        elif mag == 'drum':
            for y in range(H):
                for x in range(W):
                    if math.hypot(x - (x0 + stock + 6), y - (barrel_y + 4.5)) < 2.6:
                        px(img, x, y, steel)
        elif mag == 'long':
            rect(img, x0 + 6, barrel_y + 3, x0 + 7, barrel_y + 6, steel)
        elif mag == 'side':
            rect(img, x0 + stock + 3, barrel_y - 2, x0 + stock + 6, barrel_y - 1, steel)
        elif mag == 'belt':
            rect(img, x0 + stock + 3, barrel_y + 3, x0 + stock + 7, barrel_y + 5, 'w' if chalk else 'y')
        elif mag == 'tank':
            pass
        if extra in ('grip2',):
            rect(img, x0 + stock + 9, barrel_y + 3, x0 + stock + 10, barrel_y + 5, wood)
        if extra == 'pump':
            rect(img, x0 + length - 8, barrel_y + 2, x0 + length - 4, barrel_y + 3, wood)
        if extra == 'bolt':
            # Kar98k: ferrolho curvado para baixo e coronha com degrau
            px(img, x0 + stock + 3, barrel_y + 3, hi)
            px(img, x0 + stock + 4, barrel_y + 4, hi)
            px(img, x0 + 2, barrel_y + 4, wood)
        if extra == 'bands':
            # Springfield: ferrolho reto para cima, duas braçadeiras e massa de mira alta
            px(img, x0 + stock + 3, barrel_y - 1, hi)
            px(img, x0 + stock + 3, barrel_y - 2, hi)
            for bx in (x0 + int(length * 0.45), x0 + int(length * 0.65)):
                rect(img, bx, barrel_y - 1, bx, barrel_y + 2, steel if chalk else 'G')
            rect(img, x0 + length - 2, barrel_y - 2, x0 + length - 1, barrel_y - 1, steel)
        if extra == 'cover':
            # M1919: tampa alta e retangular no corpo
            rect(img, x0 + stock + 1, barrel_y - 2, x0 + stock + 9, barrel_y - 1, steel)
            for x in range(x0 + int(length * 0.55), x0 + length - 1, 3):
                px(img, x, barrel_y + 1, 'k' if not chalk else '.')
        if extra == 'cyl':
            rect(img, x0 + 4, barrel_y - 1, x0 + 6, barrel_y + 2, hi)
        if extra == 'shroud':
            for x in range(x0 + int(length * 0.55), x0 + length - 1, 2):
                px(img, x, barrel_y, 'k' if not chalk else '.')
        if extra == 'bipod':
            px(img, x0 + length - 4, barrel_y + 2, steel); px(img, x0 + length - 5, barrel_y + 3, steel)
        if extra == 'fold':
            rect(img, x0, barrel_y + 1, x0 + 3, barrel_y + 1, steel)
        if extra == 'nade':
            rect(img, x0 + length - 2, barrel_y - 1, x0 + length + 1, barrel_y + 2, 'w' if chalk else 'v')
        if extra == 'muzzle':
            rect(img, x0 + length - 2, barrel_y - 1, x0 + length - 1, barrel_y + 2, steel)
        if scope:
            rect(img, x0 + stock + 4, barrel_y - 2, x0 + stock + 9, barrel_y - 1, steel)
    if not chalk:
        outline(img)
    return img


def equipment_icons():
    for gid, rows in {
        'grenades': ["..........ddd.", "bbbbbbbbbbdgdd", "bBBBBBBBBBdddd", "bbbbbbbbbbdgdd", "..........ddd."],
        'molotov': ["....W.....", "...eLe....", "..eLLLe..o", "..eLLLe.oO", "...eLe....", "....W....."],
        'betty': [".gggg.", "gDDDDg", "gDRRDg", "gDDDDg", ".gggg."],
    }.items():
        img = grid(rows)
        canvas = new(30, 10)
        canvas.alpha_composite(img, ((30 - img.width) // 2, (10 - img.height) // 2))
        save(outline(canvas), 'Gun_' + gid)
        chalk = new(30, 10)
        for y in range(10):
            for x in range(30):
                if canvas.getpixel((x, y))[3] > 0:
                    chalk.putpixel((x, y), C['w'])
        save(chalk, 'Chalk_' + gid)


# ================================================================== interface
FONT = {
    ' ': ["     "] * 7,
    '!': ["  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "     ", "  #  "],
    '"': [" # # ", " # # ", "     ", "     ", "     ", "     ", "     "],
    '#': [" # # ", "#####", " # # ", " # # ", "#####", " # # ", "     "],
    '$': ["  #  ", " ####", "# #  ", " ### ", "  # #", "#### ", "  #  "],
    '%': ["##  #", "## # ", "  #  ", " #   ", "# ## ", "   ##", "     "],
    '&': [" ##  ", "#  # ", " ##  ", " ## #", "#  # ", "#  # ", " ## #"],
    "'": ["  #  ", "  #  ", "     ", "     ", "     ", "     ", "     "],
    '(': ["   # ", "  #  ", " #   ", " #   ", " #   ", "  #  ", "   # "],
    ')': [" #   ", "  #  ", "   # ", "   # ", "   # ", "  #  ", " #   "],
    '*': ["     ", "# # #", " ### ", "#####", " ### ", "# # #", "     "],
    '+': ["     ", "  #  ", "  #  ", "#####", "  #  ", "  #  ", "     "],
    ',': ["     ", "     ", "     ", "     ", "     ", "  #  ", " #   "],
    '-': ["     ", "     ", "     ", "#####", "     ", "     ", "     "],
    '.': ["     ", "     ", "     ", "     ", "     ", "     ", "  #  "],
    '/': ["    #", "   # ", "   # ", "  #  ", " #   ", " #   ", "#    "],
    '0': [" ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### "],
    '1': ["  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "],
    '2': [" ### ", "#   #", "    #", "   # ", "  #  ", " #   ", "#####"],
    '3': ["#####", "   # ", "  #  ", "   # ", "    #", "#   #", " ### "],
    '4': ["   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # "],
    '5': ["#####", "#    ", "#### ", "    #", "    #", "#   #", " ### "],
    '6': ["  ## ", " #   ", "#    ", "#### ", "#   #", "#   #", " ### "],
    '7': ["#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   "],
    '8': [" ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### "],
    '9': [" ### ", "#   #", "#   #", " ####", "    #", "   # ", " ##  "],
    ':': ["     ", "  #  ", "     ", "     ", "     ", "  #  ", "     "],
    ';': ["     ", "  #  ", "     ", "     ", "  #  ", " #   ", "     "],
    '<': ["   # ", "  #  ", " #   ", "#    ", " #   ", "  #  ", "   # "],
    '=': ["     ", "     ", "#####", "     ", "#####", "     ", "     "],
    '>': [" #   ", "  #  ", "   # ", "    #", "   # ", "  #  ", " #   "],
    '?': [" ### ", "#   #", "    #", "   # ", "  #  ", "     ", "  #  "],
    '@': [" ### ", "#   #", "# ###", "# # #", "# ###", "#    ", " ####"],
    'A': [" ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #"],
    'B': ["#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### "],
    'C': [" ### ", "#   #", "#    ", "#    ", "#    ", "#   #", " ### "],
    'D': ["###  ", "#  # ", "#   #", "#   #", "#   #", "#  # ", "###  "],
    'E': ["#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####"],
    'F': ["#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#    "],
    'G': [" ### ", "#   #", "#    ", "# ###", "#   #", "#   #", " ####"],
    'H': ["#   #", "#   #", "#   #", "#####", "#   #", "#   #", "#   #"],
    'I': [" ### ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "],
    'J': ["  ###", "   # ", "   # ", "   # ", "   # ", "#  # ", " ##  "],
    'K': ["#   #", "#  # ", "# #  ", "##   ", "# #  ", "#  # ", "#   #"],
    'L': ["#    ", "#    ", "#    ", "#    ", "#    ", "#    ", "#####"],
    'M': ["#   #", "## ##", "# # #", "# # #", "#   #", "#   #", "#   #"],
    'N': ["#   #", "#   #", "##  #", "# # #", "#  ##", "#   #", "#   #"],
    'O': [" ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
    'P': ["#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    "],
    'Q': [" ### ", "#   #", "#   #", "#   #", "# # #", "#  # ", " ## #"],
    'R': ["#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #"],
    'S': [" ####", "#    ", "#    ", " ### ", "    #", "    #", "#### "],
    'T': ["#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  "],
    'U': ["#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
    'V': ["#   #", "#   #", "#   #", "#   #", "#   #", " # # ", "  #  "],
    'W': ["#   #", "#   #", "#   #", "# # #", "# # #", "# # #", " # # "],
    'X': ["#   #", "#   #", " # # ", "  #  ", " # # ", "#   #", "#   #"],
    'Y': ["#   #", "#   #", " # # ", "  #  ", "  #  ", "  #  ", "  #  "],
    'Z': ["#####", "    #", "   # ", "  #  ", " #   ", "#    ", "#####"],
    '[': [" ### ", " #   ", " #   ", " #   ", " #   ", " #   ", " ### "],
    '\\': ["#    ", " #   ", " #   ", "  #  ", "   # ", "   # ", "    #"],
    ']': [" ### ", "   # ", "   # ", "   # ", "   # ", "   # ", " ### "],
    '^': ["  #  ", " # # ", "#   #", "     ", "     ", "     ", "     "],
    '_': ["     ", "     ", "     ", "     ", "     ", "     ", "#####"],
    '×': ["     ", "#   #", " # # ", "  #  ", " # # ", "#   #", "     "],
    '•': ["     ", "     ", " ### ", " ### ", " ### ", "     ", "     "],
}
ACCENTS = {'Á': ('A', "  #  "), 'À': ('A', " #   "), 'Â': ('A', " # # "), 'Ã': ('A', " ## #"),
           'É': ('E', "  #  "), 'Ê': ('E', " # # "), 'Í': ('I', "  #  "), 'Ó': ('O', "  #  "),
           'Ô': ('O', " # # "), 'Õ': ('O', " ## #"), 'Ú': ('U', "  #  "), 'Ü': ('U', " # # ")}
CHARSET = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_ÁÀÂÃÉÊÍÓÔÕÚÇÜ×•"


def glyph(ch):
    """Devolve 8 linhas de 5 colunas (linha 7 = descendente)."""
    if ch in FONT:
        return FONT[ch] + ["     "]
    if ch in ACCENTS:
        base, acc = ACCENTS[ch]
        rows = FONT[base]
        # tira uma linha repetida para caber o acento sem perder traços (a barra do A, do E)
        drop = next((i for i in range(1, 6) if rows[i] == rows[i + 1]), 3)
        squeezed = rows[:drop] + rows[drop + 1:]
        return [acc] + squeezed + ["     "]
    if ch == 'Ç':
        return FONT['C'] + ["  #  "]
    raise KeyError(ch)


def font():
    cols = 16
    rows = (len(CHARSET) + cols - 1) // cols
    img = new(cols * 6, rows * 8)
    for i, ch in enumerate(CHARSET):
        g = glyph(ch)
        cx, cy = (i % cols) * 6, (i // cols) * 8
        for y, row in enumerate(g):
            for x, v in enumerate(row):
                if v == '#':
                    img.putpixel((cx + x, cy + y), (255, 255, 255, 255))
    save(img, 'Font')


def ui():
    panel = new(8, 8)
    rect(panel, 0, 0, 7, 7, (210, 200, 190, 255))
    rect(panel, 1, 1, 6, 6, (40, 34, 44, 255))
    rect(panel, 2, 2, 5, 5, (60, 52, 64, 255))
    px(panel, 0, 0, '.'); px(panel, 7, 0, '.'); px(panel, 0, 7, '.'); px(panel, 7, 7, '.')
    save(panel, 'Panel')

    tally = new(5, 22)
    for y in range(22):
        w = 2 if 2 < y < 19 else 1
        x = 1 + (1 if y > 12 else 0)
        for i in range(w + 1):
            if random.random() > 0.08:
                px(tally, x + i, y, 'w')
    save(tally, 'Tally')

    slash = new(28, 18)
    for t in range(60):
        x = int(t * 27 / 59)
        y = int(16 - t * 14 / 59)
        px(slash, x, y, 'w'); px(slash, x, y + 1, 'w')
        if random.random() > 0.3:
            px(slash, x + 1, y, 'w')
    save(slash, 'TallySlash')

    vig = new(96, 54)
    for y in range(54):
        for x in range(96):
            dx = (x - 47.5) / 48
            dy = (y - 26.5) / 27
            d = max(abs(dx) ** 3, abs(dy) ** 3) ** (1 / 3)
            a = max(0.0, min(1.0, (d - 0.62) / 0.38))
            vig.putpixel((x, y), (200, 10, 20, int(255 * a ** 1.4)))
    save(vig, 'Vignette')

    scan = new(1, 2)
    scan.putpixel((0, 0), (0, 0, 0, 255))
    save(scan, 'Scanline')

    win = new(32, 40)
    for y in range(40):
        for x in range(32):
            top = y < 10 and math.hypot(x - 15.5, y - 10) > 15
            if top:
                continue
            border = x < 2 or x > 29 or y > 37 or (y < 12 and math.hypot(x - 15.5, y - 10) > 13.5)
            px(win, x, y, 'd' if border else ('c' if random.random() > 0.08 else 'u'))
    for x in (10, 21):
        rect(win, x, 2, x, 37, 'k')
    for y in (14, 26):
        rect(win, 2, y, 29, y, 'k')
    for y in range(4, 12):
        for x in range(20, 27):
            if math.hypot(x - 23, y - 7) < 2.6:
                px(win, x, y, 'W')
    save(win, 'MenuWindow')


# ================================================================== inimigos especiais e máquina
def enemies():
    # cão-zumbi magro (vista de cima, olhando para a direita): pelo cinza-marrom, costelas, olhos vermelhos
    for f in (0, 1):
        img = new(16, 16)
        leg = 1 if f else -1
        for x, y in ((4 + leg, 4), (4 - leg, 11), (10 - leg, 4), (10 + leg, 11)):
            rect(img, x, y, x + 1, y + 1, 'N')
        for y in range(5, 11):
            for x in range(2, 13):
                if ((x - 7) / 5.2) ** 2 + ((y - 7.5) / 2.6) ** 2 <= 1:
                    px(img, x, y, 'n' if (x + y) % 3 else 'D')
        for x in range(5, 10, 2):
            px(img, x, 6, 'r'); px(img, x, 9, 'r')          # costelas expostas
        rect(img, 12, 6, 15, 9, 'n'); rect(img, 14, 7, 15, 8, 'd')
        px(img, 13, 6, 'R'); px(img, 13, 9, 'R')            # olhos
        px(img, 1, 7 + (1 if f else 0), 'n')                # rabo
        save(outline(img), f'Dog_{f}')
    img = Image.open(os.path.join(OUT, 'Dog_0.png')).convert('RGBA')
    rect(img, 14, 6, 15, 9, 'w'); px(img, 15, 7, 'R')       # boca aberta
    save(img, 'Dog_Attack')
    corpse = new(16, 16)
    for y in range(6, 11):
        for x in range(2, 14):
            if ((x - 8) / 5.5) ** 2 + ((y - 8) / 2.2) ** 2 <= 1:
                px(corpse, x, y, 'n' if random.random() > 0.2 else 'r')
    save(outline(corpse, 'K'), 'DogCorpse')

    # Panzer: brutamontes de macacão blindado, original (24x24)
    def panzer(f, attack=False):
        img = new(24, 24)
        sway = 1 if f else -1
        rect(img, 6 + sway, 3, 9 + sway, 5, 'K'); rect(img, 6 - sway, 18, 9 - sway, 20, 'K')    # botas
        for y in range(3, 21):
            for x in range(4, 18):
                if ((x - 10.5) / 6.5) ** 2 + ((y - 11.5) / 8.5) ** 2 <= 1:
                    px(img, x, y, 'g' if (x * 3 + y) % 7 else 'G')
        for x, y in ((6, 7), (6, 16), (14, 6), (14, 17), (10, 4), (10, 19)):
            px(img, x, y, 'D')                                       # rebites
        rect(img, 3, 8, 6, 15, 'v'); rect(img, 3, 9, 4, 14, 'V')     # tanque nas costas
        px(img, 2, 10, 'd'); px(img, 2, 13, 'd')
        reach = 3 if attack else 0
        rect(img, 13, 3, 18 + reach, 5, 'D'); rect(img, 18 + reach, 2, 20 + reach, 6, 'd')       # braço-gancho (cima)
        px(img, 21 + reach, 2, 'G'); px(img, 21 + reach, 6, 'G')
        rect(img, 13, 18, 18, 20, 'D'); rect(img, 18, 18, 21, 20, 'k'); px(img, 22, 19, 'o')     # bocal de fogo (baixo)
        for y in range(8, 16):
            for x in range(10, 17):
                if math.hypot(x - 13, y - 11.5) < 3.4:
                    px(img, x, y, 'd')                               # capacete redondo
        px(img, 15, 11, 'Y'); px(img, 15, 12, 'Y'); px(img, 16, 11, 'w'); px(img, 16, 12, 'w')   # farol
        return outline(img)

    save(panzer(0), 'Panzer_0')
    save(panzer(1), 'Panzer_1')
    save(panzer(0, True), 'Panzer_Attack')
    corpse = new(24, 24)
    for y in range(6, 18):
        for x in range(2, 22):
            if ((x - 12) / 9.5) ** 2 + ((y - 12) / 5.5) ** 2 <= 1:
                px(corpse, x, y, 'g' if random.random() > 0.15 else 'K')
    save(outline(corpse, 'K'), 'PanzerCorpse')

    save(outline(grid([
        "G.....",
        "GG..G.",
        ".GGGGG",
        ".GGGGG",
        "GG..G.",
        "G.....",
    ])), 'Claw')

    # mira (11x11): cruz com vão no meio e ponto central
    img = new(11, 11)
    for i in (0, 1, 2, 3, 7, 8, 9, 10):
        px(img, i, 5, 'w'); px(img, 5, i, 'w')
    px(img, 5, 5, 'w')
    save(outline(img), 'Crosshair')

    # Soco em Lata: máquina roxa e dourada; o letreiro mostra um punho amassando uma lata
    for state in ('Off', 'On'):
        img = new(16, 16)
        rect(img, 1, 1, 14, 14, 'K')
        rect(img, 2, 2, 13, 13, 'm' if state == 'On' else 'd')
        rect(img, 2, 2, 13, 4, 'y' if state == 'On' else 'n')
        rect(img, 4, 6, 11, 12, 'K' if state == 'Off' else 'M')
        rect(img, 5, 9, 7, 11, 'G'); rect(img, 5, 8, 7, 8, 'g')     # lata
        rect(img, 8, 7, 10, 9, 'O'); px(img, 8, 10, 'O')           # punho
        if state == 'On':
            px(img, 3, 13, 'w'); px(img, 12, 13, 'w'); px(img, 7, 3, 'w'); px(img, 8, 3, 'w')
        save(img, 'PackPunch_' + state)


def main():
    tiles()
    objects()
    characters()
    enemies()
    effects()
    for gid in GUNS:
        save(gun_icon(gid), 'Gun_' + gid)
        save(gun_icon(gid, chalk=True), 'Chalk_' + gid)
    equipment_icons()
    font()
    ui()
    n = len([f for f in os.listdir(OUT) if f.endswith('.png')])
    print(f'ok: {n} sprites em {os.path.normpath(OUT)}')


if __name__ == '__main__':
    main()
