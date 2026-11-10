"""Gera Assets/Scripts/Map/VerrucktMap.cs a partir de retângulos + marcadores.

A planta em ASCII no .cs é a fonte da verdade para o jogo; este script só
existe para montar a primeira versão sem erro de contagem de colunas.
Rode: python3 Tools/gerar_mapa.py
"""
import os

W, H = 92, 56
g = [['o'] * W for _ in range(H)]

# zona -> retângulo interno (x0, y0, x1, y1), inclusive
ROOMS = {
    '0': (8, 36, 39, 51),    # Enfermaria Oeste (spawn)
    '1': (4, 17, 13, 32),    # Varanda Oeste
    '2': (4, 4, 27, 13),     # Andar Superior Oeste
    '3': (31, 4, 44, 12),    # Sala do Gerador
    '4': (44, 36, 66, 51),   # Enfermaria Leste (spawn)
    '5': (68, 45, 87, 51),   # Necrotério (sala dos fundos)
    '6': (68, 36, 87, 41),   # Corredor Leste
    '7': (74, 17, 83, 32),   # Varanda Leste
    '8': (66, 4, 79, 12),    # Andar Superior Leste
    '9': (48, 4, 62, 12),    # Cozinha
}

for z, (x0, y0, x1, y1) in ROOMS.items():
    for y in range(y0 - 1, y1 + 2):
        for x in range(x0 - 1, x1 + 2):
            if g[y][x] == 'o':
                g[y][x] = '#'
for z, (x0, y0, x1, y1) in ROOMS.items():
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            g[y][x] = '.'

# frestas estreitas entre salas viram parede (não queremos grama de 1 tile)
def solid(c):
    return c != 'o'

changed = True
while changed:
    changed = False
    for y in range(H):
        for x in range(W):
            if g[y][x] != 'o':
                continue
            for dx, dy in ((1, 0), (0, 1)):
                a = b = None
                for d in range(1, 4):
                    xx, yy = x - dx * d, y - dy * d
                    if 0 <= xx < W and 0 <= yy < H and solid(g[yy][xx]):
                        a = d
                        break
                for d in range(1, 4):
                    xx, yy = x + dx * d, y + dy * d
                    if 0 <= xx < W and 0 <= yy < H and solid(g[yy][xx]):
                        b = d
                        break
                if a and b and a + b <= 4:
                    g[y][x] = '#'
                    changed = True
                    break

def rect(ch, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            g[y][x] = ch

# portas (letras minúsculas; tiles contíguos com a mesma letra = uma porta)
rect('a', 9, 33, 11, 35)     # Enfermaria O -> Varanda O        1000
rect('b', 6, 14, 8, 16)      # Varanda O -> Superior O          750
rect('c', 28, 8, 30, 10)     # Superior O -> Gerador            1000
rect('d', 67, 47, 67, 49)    # Enfermaria L -> Necrotério       750
rect('e', 67, 37, 67, 39)    # Enfermaria L -> Corredor L       750
rect('f', 77, 33, 79, 35)    # Corredor L -> Varanda L          1000
rect('g', 75, 13, 77, 16)    # Varanda L -> Superior L          750
rect('h', 63, 8, 65, 10)     # Superior L -> Cozinha            1000
rect('i', 45, 7, 47, 9)      # Cozinha -> Gerador               1000
rect('p', 40, 42, 43, 44)    # porta elétrica entre os spawns

# divisória da salinha do Ferro-Nog (canto sudoeste da Enfermaria O)
rect('#', 8, 45, 11, 45)
rect('#', 14, 45, 15, 45)
rect('#', 15, 46, 15, 51)

# escada do gerador até a janela
rect('=', 36, 11, 38, 12)

M = [
    # zona 0
    (25, 44, '0'), (25, 46, '<'), (7, 39, 'W'), (25, 52, 'W'), (34, 52, 'W'), (28, 35, 'W'),
    (20, 52, 'K'), (30, 52, 'N'), (7, 48, 'G'), (9, 51, 'J'), (12, 51, 'X'),
    (25, 40, '+'), (11, 48, '+'), (35, 40, '+'), (38, 50, '?'),
    (18, 38, '&'), (21, 38, '&'), (31, 38, '&'), (34, 38, '&'), (30, 49, '%'),
    # zona 1
    (8, 25, '1'), (3, 19, 'M'), (3, 30, 'B'), (14, 22, 'Y'), (13, 23, 'U'), (14, 28, 'W'), (3, 25, 'W'),
    (13, 30, 'X'), (14, 19, '!'), (3, 27, '!'), (8, 22, '+'), (8, 29, '+'), (5, 32, '?'),
    # zona 2
    (15, 9, '2'), (12, 3, 'W'), (3, 8, 'W'), (20, 3, 'S'), (20, 14, 'T'), (27, 12, 'X'),
    (5, 4, 'L'), (7, 4, 'L'), (9, 4, 'L'), (10, 9, '+'), (22, 8, '+'), (26, 4, '?'),
    (16, 5, '&'), (19, 5, '&'),
    # zona 3
    (40, 8, '3'), (38, 4, '$'), (34, 3, 'P'), (37, 13, 'W'), (37, 7, '+'), (44, 12, '?'),
    # zona 9
    (55, 9, '9'), (55, 3, 'W'), (52, 6, '+'), (59, 6, '+'), (62, 4, '?'),
    (51, 8, '%'), (52, 8, '%'), (58, 8, '%'), (59, 8, '%'),
    # zona 8
    (72, 9, '8'), (78, 3, 'W'), (70, 3, 'O'), (80, 7, 'N'), (76, 4, 'C'), (72, 7, '+'), (66, 12, '?'),
    # zona 7
    (78, 25, '7'), (84, 20, 'T'), (73, 24, 'A'), (84, 28, 'Y'), (73, 20, 'W'), (84, 24, 'W'), (73, 30, 'W'),
    (84, 22, '!'), (73, 27, '!'), (78, 19, '+'), (78, 30, '+'), (74, 32, '?'),
    # zona 6
    (78, 38, '6'), (72, 35, 'H'), (84, 35, 'B'), (88, 39, 'N'), (70, 35, 'W'), (88, 37, 'W'), (88, 41, 'W'),
    (71, 41, 'X'), (78, 37, '+'),
    # zona 5
    (78, 48, '5'), (88, 48, 'A'), (78, 46, '+'), (87, 51, '?'), (87, 45, 'Z'),   # Z = Soco em Lata
    (71, 46, '&'), (74, 46, '&'), (82, 46, '&'), (71, 50, '&'), (74, 50, '&'), (82, 50, '&'),
    # zona 4
    (55, 44, '4'), (55, 46, '>'), (52, 35, 'W'), (62, 35, 'W'), (55, 52, 'W'),
    (48, 52, 'F'), (62, 52, 'R'), (66, 40, 'Q'), (50, 40, '+'), (60, 48, '+'),
    (48, 38, '&'), (51, 38, '&'), (58, 38, '&'), (61, 38, '&'),
]
for x, y, c in M:
    g[y][x] = c

# armadilhas: faixas de grade elétrica atravessando as varandas
rect('~', 4, 20, 13, 20)
rect('~', 4, 26, 13, 26)
rect('~', 74, 22, 83, 22)
rect('~', 74, 27, 83, 27)

rows = [''.join(r) for r in g]
print('\n'.join(f'{i:2d} {r}' for i, r in enumerate(rows)))

here = os.path.dirname(os.path.abspath(__file__))
out = os.path.join(here, '..', 'Assets', 'Scripts', 'Map', 'VerrucktMap.cs')
os.makedirs(os.path.dirname(out), exist_ok=True)
body = ',\n'.join(f'            "{r}"' for r in rows)
open(out, 'w', encoding='utf-8').write(open(os.path.join(here, 'VerrucktMap.template.cs'), encoding='utf-8').read().replace('__ROWS__', body))
print('ok ->', os.path.normpath(out))
