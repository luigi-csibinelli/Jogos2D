"""Gera a capa do itch.io (630x500) a partir dos próprios sprites do jogo."""
import os
import random

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, '..', 'Assets', 'Resources', 'Art')
OUT = os.path.join(HERE, '..', 'capa_itch.png')
random.seed(9)


def sp(name):
    return Image.open(os.path.join(ART, name + '.png')).convert('RGBA')


def text(img, s, x, y, scale, color):
    font = sp('Font')
    charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_ÁÀÂÃÉÊÍÓÔÕÚÇÜ×•"
    for i, ch in enumerate(s.upper()):
        k = charset.find(ch)
        if k < 0:
            continue
        g = font.crop(((k % 16) * 6, (k // 16) * 8, (k % 16) * 6 + 6, (k // 16) * 8 + 8))
        tinted = Image.new('RGBA', g.size, color + (255,))
        tinted.putalpha(g.getchannel('A'))
        for dx, dy, c in ((1, 1, (0, 0, 0)),):
            sh = Image.new('RGBA', g.size, c + (255,))
            sh.putalpha(g.getchannel('A'))
            img.alpha_composite(sh.resize((6 * scale, 8 * scale), Image.NEAREST), (x + i * 6 * scale + dx * scale, y + dy * scale))
        img.alpha_composite(tinted.resize((6 * scale, 8 * scale), Image.NEAREST), (x + i * 6 * scale, y))


def main():
    W, H = 210, 167                      # desenha pequeno e amplia 3x
    img = Image.new('RGBA', (W, H), (12, 10, 16, 255))
    for y in range(0, H, 16):
        for x in range(0, W, 16):
            img.alpha_composite(sp('FloorTileA' if (x + y) // 16 % 2 else 'FloorTileB'), (x, y))
    for x in range(0, W, 16):
        img.alpha_composite(sp('WallFace'), (x, 0))
    img.alpha_composite(sp('WindowFrame'), (64, 0))
    for i in range(3):
        img.alpha_composite(sp('Plank'), (64, 2 + i * 4))
    # escuridão com a "lanterna"
    dark = Image.new('RGBA', (W, H), (0, 0, 10, 0))
    px = dark.load()
    for y in range(H):
        for x in range(W):
            d = ((x - 95) ** 2 + (y - 105) ** 2) ** 0.5
            px[x, y] = (5, 5, 20, int(min(220, max(0, (d - 30) * 3))))
    img.alpha_composite(dark)
    for name, pos, rot in (('Zombie0_0', (165, 95), 200), ('Zombie1_1', (150, 110), 170),
                           ('Zombie2_Attack', (125, 135), 150), ('Crawler0_0', (40, 120), 20)):
        img.alpha_composite(sp(name).rotate(rot, Image.NEAREST, expand=True), pos)
    img.alpha_composite(sp('Player_0'), (88, 98))
    img.alpha_composite(sp('Box'), (20, 80))
    img.alpha_composite(sp('Gun_raygun'), (12, 70))
    img.alpha_composite(sp('PerkFerroNog_On'), (180, 30))
    big = img.resize((W * 3, H * 3), Image.NEAREST).crop((0, 0, 630, 500))
    text(big, 'SANATÓRIO', 105, 110, 9, (196, 32, 40))
    text(big, 'UMA HOMENAGEM 8-BIT A VERRÜCKT', 52, 192, 3, (232, 226, 210))
    big.convert('RGB').save(OUT)
    print('ok ->', os.path.normpath(OUT))


if __name__ == '__main__':
    main()
