# Gera todos os sprites 8-bit do jogo em Assets/Sprites (visao de cima, estilo GTA antigo / SAS).
# Personagens sao desenhados olhando para a DIREITA; no jogo o sprite gira para a mira.
# Uso: python Ferramentas/gera_sprites.py   (rodar na raiz do projeto)
import os
import random
from PIL import Image, ImageDraw

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SAIDA = os.path.join(RAIZ, "Assets", "Sprites")
os.makedirs(SAIDA, exist_ok=True)

T = (0, 0, 0, 0)
CONTORNO = (20, 12, 24, 255)


def rgb(r, g, b, a=255):
    return (int(r), int(g), int(b), a)


def escurece(c, f):
    return rgb(c[0] * f, c[1] * f, c[2] * f, c[3])


def salva(nome, img):
    img.save(os.path.join(SAIDA, nome + ".png"))


def nova(w, h):
    return Image.new("RGBA", (w, h), T)


def ret(img, x, y, w, h, cor):
    for i in range(x, x + w):
        for j in range(y, y + h):
            if 0 <= i < img.width and 0 <= j < img.height:
                img.putpixel((i, j), cor)


def px(img, x, y, cor):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), cor)


def contorno(img, cor=CONTORNO):
    """Contorno escuro de 1px em volta de tudo que for opaco (visual 8-bit)."""
    w, h = img.size
    src = img.copy()
    for x in range(w):
        for y in range(h):
            if src.getpixel((x, y))[3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src.getpixel((nx, ny))[3] != 0 and src.getpixel((nx, ny)) != cor:
                    img.putpixel((x, y), cor)
                    break
    return img


def elipse(d, caixa, cor):
    d.ellipse(caixa, fill=cor)


# ---------------------------------------------------------------- personagens (de cima)
AGENTE = {
    "cabelo": rgb(44, 30, 22), "pele": rgb(232, 176, 128), "roupa": rgb(36, 54, 104),
    "roupa2": rgb(22, 30, 64), "detalhe": rgb(236, 204, 72), "bota": rgb(30, 24, 22),
}
ZUMBI = {
    "cabelo": rgb(62, 54, 42), "pele": rgb(124, 164, 100), "roupa": rgb(118, 96, 82),
    "roupa2": rgb(78, 60, 52), "detalhe": rgb(150, 18, 22), "bota": rgb(36, 30, 30),
}
CORPO_OKAFOR = dict(AGENTE)
CORPO_OKAFOR.update({"pele": rgb(176, 160, 150), "detalhe": rgb(150, 18, 22)})

ARMA = rgb(70, 70, 82)
LAMINA = rgb(214, 214, 224)


def pes(d, pal, quadro):
    """Pes aparecem na frente/atras do corpo ao andar (quadro 0 = parado)."""
    b = pal["bota"]
    if quadro == 0:
        d.rectangle((6, 4, 8, 5), fill=b)
        d.rectangle((6, 10, 8, 11), fill=b)
    elif quadro == 1:
        d.rectangle((10, 4, 12, 5), fill=b)     # pe esquerdo a frente
        d.rectangle((3, 10, 5, 11), fill=b)
    else:
        d.rectangle((3, 4, 5, 5), fill=b)
        d.rectangle((10, 10, 12, 11), fill=b)   # pe direito a frente


def tronco(d, pal):
    d.ellipse((4, 2, 10, 13), fill=pal["roupa"])
    d.ellipse((4, 8, 10, 13), fill=pal["roupa2"])          # sombra (luz vem de cima/esquerda)
    d.ellipse((4, 2, 10, 11), fill=pal["roupa"])


def cabeca(d, pal, img):
    d.ellipse((5, 5, 10, 10), fill=pal["cabelo"])
    px(img, 10, 7, pal["pele"])
    px(img, 10, 8, pal["pele"])
    px(img, 9, 7, pal["pele"])


def agente(quadro, arma):
    img = nova(16, 16)
    d = ImageDraw.Draw(img)
    p = AGENTE
    pes(d, p, quadro)
    tronco(d, p)
    px(img, 6, 4, p["detalhe"])                            # distintivo no ombro
    if arma == "pistola":
        d.line((7, 3, 12, 6), fill=p["roupa"], width=2)
        d.line((7, 12, 12, 9), fill=p["roupa"], width=2)
        d.rectangle((12, 6, 13, 9), fill=p["pele"])
        d.rectangle((13, 7, 15, 8), fill=ARMA)
    elif arma == "faca":
        d.line((7, 12, 12, 11), fill=p["roupa"], width=2)
        d.rectangle((12, 10, 13, 11), fill=p["pele"])
        d.rectangle((14, 10, 15, 11), fill=LAMINA)
        d.line((7, 3, 9, 2), fill=p["roupa"], width=2)
    else:
        d.line((7, 3, 9, 2), fill=p["roupa"], width=2)
        d.line((7, 12, 9, 13), fill=p["roupa"], width=2)
    cabeca(d, p, img)
    return contorno(img)


def zumbi(quadro):
    img = nova(16, 16)
    d = ImageDraw.Draw(img)
    p = ZUMBI
    pes(d, p, quadro)
    tronco(d, p)
    # bracos esticados para frente
    d.line((7, 3, 14, 4), fill=p["pele"], width=2)
    d.line((7, 12, 14, 11), fill=p["pele"], width=2)
    d.line((7, 3, 9, 3), fill=p["roupa2"], width=2)
    d.line((7, 12, 9, 12), fill=p["roupa2"], width=2)
    px(img, 5, 6, p["detalhe"]); px(img, 6, 11, p["detalhe"]); px(img, 8, 9, p["detalhe"])
    d.ellipse((5, 5, 10, 10), fill=p["pele"])
    px(img, 6, 6, p["cabelo"]); px(img, 7, 6, p["cabelo"]); px(img, 6, 8, p["cabelo"])
    px(img, 9, 6, rgb(230, 40, 30)); px(img, 9, 9, rgb(230, 40, 30))   # olhos
    return contorno(img)


def deitado(pal, tam=16):
    """Corpo largado no chao, de barriga para cima, bracos abertos."""
    img = nova(tam, tam)
    d = ImageDraw.Draw(img)
    o = (tam - 16) // 2
    d.line((o + 2, o + 6, o + 5, o + 7), fill=pal["roupa2"], width=2)        # pernas
    d.line((o + 2, o + 10, o + 5, o + 9), fill=pal["roupa2"], width=2)
    d.rectangle((o + 1, o + 5, o + 2, o + 6), fill=pal["bota"])
    d.rectangle((o + 1, o + 10, o + 2, o + 11), fill=pal["bota"])
    d.ellipse((o + 5, o + 4, o + 11, o + 11), fill=pal["roupa"])           # tronco
    d.line((o + 8, o + 5, o + 10, o + 1), fill=pal["roupa"], width=2)       # bracos abertos
    d.line((o + 8, o + 10, o + 11, o + 14), fill=pal["roupa"], width=2)
    px(img, o + 10, o + 1, pal["pele"]); px(img, o + 11, o + 14, pal["pele"])
    d.ellipse((o + 11, o + 5, o + 14, o + 10), fill=pal["pele"])           # cabeca
    d.ellipse((o + 12, o + 5, o + 14, o + 10), fill=pal["cabelo"])
    px(img, o + 7, o + 7, pal["detalhe"])
    return contorno(img)


for q in range(3):
    salva(f"jog_pistola_{q}", agente(q, "pistola"))
    salva(f"jog_faca_{q}", agente(q, "faca"))
    salva(f"jog_maos_{q}", agente(q, None))
    salva(f"zum_{q}", zumbi(q))
salva("jog_morto", deitado(AGENTE))
salva("zum_caido", deitado(ZUMBI))

lev = nova(16, 16)
d = ImageDraw.Draw(lev)
d.ellipse((4, 3, 11, 12), fill=ZUMBI["roupa2"])                 # agachado
d.line((8, 3, 13, 2), fill=ZUMBI["pele"], width=2)               # maos apoiando no chao
d.line((8, 12, 13, 13), fill=ZUMBI["pele"], width=2)
d.ellipse((7, 5, 11, 10), fill=ZUMBI["pele"])
px(lev, 10, 6, rgb(230, 40, 30)); px(lev, 10, 9, rgb(230, 40, 30))
salva("zum_levantando", contorno(lev))

# corpo do Okafor (32x32) numa poca de sangue
co = nova(32, 32)
d = ImageDraw.Draw(co)
d.ellipse((3, 6, 29, 27), fill=rgb(110, 14, 18))
d.ellipse((9, 11, 24, 23), fill=rgb(140, 20, 24))
d.ellipse((22, 20, 30, 26), fill=rgb(110, 14, 18))
ok = deitado(CORPO_OKAFOR, 24)
co.paste(ok, (4, 4), ok)
salva("corpo_agente", co)

# ---------------------------------------------------------------- piso, tapete, paredes
random.seed(7)
MADEIRA = [rgb(104, 62, 36), rgb(116, 70, 40), rgb(96, 56, 32)]


def piso(v, brilho=1.0, semente=7):
    random.seed(semente + v)
    t = nova(16, 16)
    for faixa in range(4):
        base = escurece(random.choice(MADEIRA), brilho)
        ret(t, 0, faixa * 4, 16, 4, base)
        ret(t, 0, faixa * 4 + 3, 16, 1, escurece(rgb(66, 38, 24), brilho))
        emenda = random.randint(2, 13)
        ret(t, emenda, faixa * 4, 1, 3, escurece(rgb(70, 40, 24), brilho))
        for _ in range(2):
            px(t, random.randint(0, 15), faixa * 4 + random.randint(0, 2), escurece(rgb(130, 82, 48), brilho))
    return t


for v in range(3):
    salva(f"piso_{v}", piso(v))

VERMELHO = rgb(122, 22, 32)
VERMELHO2 = rgb(96, 16, 26)
OURO = rgb(204, 158, 60)


def tapete(borda):
    t = nova(16, 16)
    ret(t, 0, 0, 16, 16, VERMELHO)
    for y in range(16):
        for x in range(16):
            if (abs((x % 8) - 4) + abs((y % 8) - 4)) == 3:
                px(t, x, y, VERMELHO2)
            if (x % 8, y % 8) == (4, 4):
                px(t, x, y, OURO)
    if borda == "t":
        ret(t, 0, 0, 16, 3, OURO)
        ret(t, 0, 1, 16, 1, rgb(150, 110, 40))
        ret(t, 0, 3, 16, 1, VERMELHO2)
    if borda == "b":
        ret(t, 0, 13, 16, 3, OURO)
        ret(t, 0, 14, 16, 1, rgb(150, 110, 40))
        ret(t, 0, 12, 16, 1, VERMELHO2)
    return t


salva("tapete_t", tapete("t"))
salva("tapete_c", tapete("c"))
salva("tapete_b", tapete("b"))

# parede vista de cima: tampo escuro com quina iluminada
topo = nova(16, 16)
ret(topo, 0, 0, 16, 16, rgb(44, 34, 38))
for x in range(0, 16, 8):
    ret(topo, x, 0, 1, 16, rgb(36, 28, 32))
ret(topo, 0, 7, 16, 1, rgb(36, 28, 32))
ret(topo, 0, 0, 16, 1, rgb(66, 54, 58))
ret(topo, 0, 0, 1, 16, rgb(66, 54, 58))
salva("parede_topo", topo)

# sombras que as paredes jogam no piso (luz vinda de cima/esquerda)
def sombra(lados):
    s = nova(16, 16)
    alfas = [150, 110, 70, 35]
    for i, a in enumerate(alfas):
        if "n" in lados:
            for x in range(16):
                p = s.getpixel((x, i))
                s.putpixel((x, i), (0, 0, 0, max(p[3], a)))
        if "w" in lados:
            for y in range(16):
                p = s.getpixel((i, y))
                s.putpixel((i, y), (0, 0, 0, max(p[3], a)))
    return s


salva("sombra_n", sombra("n"))
salva("sombra_w", sombra("w"))
salva("sombra_nw", sombra("nw"))

# ---------------------------------------------------------------- portas (de cima)
MAD_PORTA = rgb(92, 52, 30)
MAD_PORTA2 = rgb(64, 34, 20)
SERPENTE = rgb(70, 160, 80)
SOLEIRA = rgb(58, 50, 48)

ph = nova(32, 16)
ret(ph, 0, 0, 32, 16, SOLEIRA)
ret(ph, 0, 5, 32, 6, MAD_PORTA)
ret(ph, 0, 5, 32, 1, rgb(122, 74, 44))
ret(ph, 15, 5, 2, 6, MAD_PORTA2)
px(ph, 13, 8, OURO); px(ph, 18, 8, OURO)
ret(ph, 14, 7, 4, 3, OURO)
px(ph, 15, 8, SERPENTE); px(ph, 16, 8, SERPENTE)
salva("porta_h_fechada", ph)
pha = nova(32, 16)
ret(pha, 0, 0, 32, 16, SOLEIRA)
ret(pha, 0, 5, 3, 11, MAD_PORTA)          # folhas abertas para dentro (para baixo)
ret(pha, 29, 5, 3, 11, MAD_PORTA)
ret(pha, 0, 5, 1, 11, rgb(122, 74, 44))
salva("porta_h_aberta", pha)
salva("porta_v_fechada", ph.rotate(90, expand=True))
salva("porta_v_aberta", pha.rotate(90, expand=True))

# ---------------------------------------------------------------- escada (de cima, com volume)
def escada_terreo():
    """9 degraus, 144x48; sobe para a ESQUERDA (o degrau 0 e o mais alto)."""
    img = nova(144, 48)
    for i in range(9):
        f = 1.15 - i * 0.07                      # degraus de cima mais claros
        x0 = i * 16
        ret(img, x0, 6, 16, 36, escurece(rgb(120, 76, 44), f))
        ret(img, x0, 14, 16, 20, escurece(VERMELHO, f))                 # passadeira
        ret(img, x0, 14, 16, 1, escurece(OURO, f))
        ret(img, x0, 33, 16, 1, escurece(OURO, f))
        ret(img, x0, 6, 3, 36, escurece(rgb(40, 24, 16), f))            # espelho do degrau de cima (sombra)
        ret(img, x0 + 3, 6, 1, 36, escurece(rgb(70, 44, 28), f))
        ret(img, x0 + 14, 6, 2, 36, escurece(rgb(170, 120, 74), f))     # quina iluminada
        for y in (16, 24, 31):
            px(img, x0 + 15, y, escurece(OURO, f))                     # varetas da passadeira
    # corrimaos (em cima e embaixo) com balaustres e sombra nos degraus
    for y0, sombra_y in ((0, 6), (42, 40)):
        ret(img, 0, y0, 144, 6, rgb(70, 42, 26))
        ret(img, 0, y0 + 1, 144, 3, rgb(128, 82, 48))
        ret(img, 0, y0 + 1, 144, 1, rgb(170, 118, 70))
        for x in range(2, 144, 6):
            ret(img, x, y0 + 4, 2, 2, rgb(56, 32, 20))
    for x in range(144):
        for y, a in ((6, 120), (7, 70), (40, 50), (41, 90)):
            p = img.getpixel((x, y))
            img.putpixel((x, y), escurece(p, 1 - a / 255))
    # pilares no pe da escada
    for y0 in (0, 41):
        ret(img, 138, y0, 6, 7, rgb(150, 100, 60))
        ret(img, 139, y0 + 1, 4, 4, rgb(190, 140, 84))
    return img


salva("escada_terreo", escada_terreo())

eg = nova(32, 48)
for i in range(2):
    f = 1.0 - i * 0.35
    x0 = i * 16
    ret(eg, x0, 6, 16, 36, escurece(rgb(120, 76, 44), f))
    ret(eg, x0, 14, 16, 20, escurece(VERMELHO, f))
    ret(eg, x0 + 14, 6, 2, 36, escurece(rgb(170, 120, 74), f))
    ret(eg, x0, 6, 3, 36, escurece(rgb(40, 24, 16), f))
for x in range(20, 32):                                   # some no escuro do vao
    for y in range(6, 42):
        p = eg.getpixel((x, y))
        k = (x - 20) / 12
        eg.putpixel((x, y), escurece(p, 1 - 0.9 * k))
for y0 in (0, 42):
    ret(eg, 0, y0, 32, 6, rgb(70, 42, 26))
    ret(eg, 0, y0 + 1, 32, 3, rgb(128, 82, 48))
    ret(eg, 0, y0 + 1, 32, 1, rgb(170, 118, 70))
salva("escada_galeria", eg)

# vao: o saguao la embaixo, bem escuro
for v in range(2):
    vt = piso(v, 0.22, 30)
    salva(f"vao_{v}", vt)

# balaustrada vista de cima: corrimao com balaustres e sombra no vao
bh = nova(16, 16)
ret(bh, 0, 5, 16, 5, rgb(70, 42, 26))
ret(bh, 0, 6, 16, 2, rgb(140, 92, 54))
ret(bh, 0, 6, 16, 1, rgb(180, 128, 76))
for x in range(1, 16, 4):
    ret(bh, x, 10, 2, 2, rgb(96, 60, 36))
ret(bh, 0, 12, 16, 3, (0, 0, 0, 110))
salva("corrimao_h", bh)
salva("corrimao_h2", bh.transpose(Image.FLIP_TOP_BOTTOM))   # sombra para cima
salva("corrimao_v", bh.rotate(90, expand=True))

# ---------------------------------------------------------------- moveis (de cima)
ACO = rgb(100, 104, 118)
ACO2 = rgb(70, 72, 84)
ACO3 = rgb(150, 154, 170)

cf = nova(16, 16)
ret(cf, 2, 2, 12, 12, ACO2)
ret(cf, 3, 3, 10, 10, ACO)
ret(cf, 3, 3, 10, 1, ACO3)
ret(cf, 3, 3, 1, 10, ACO3)
d = ImageDraw.Draw(cf)
d.ellipse((5, 8, 9, 12), fill=ACO3, outline=ACO2)
px(cf, 7, 10, CONTORNO)
for i in range(3):
    px(cf, 11, 8 + i, rgb(200, 200, 90) if i % 2 else ACO3)
px(cf, 11, 12, rgb(60, 200, 80))
salva("cofre_fechado", contorno(cf))
ca = cf.copy()
ret(ca, 3, 3, 10, 10, rgb(16, 14, 18))                    # aberto: interior escuro
ret(ca, 0, 12, 3, 4, ACO)                                  # porta girada para fora
ret(ca, 0, 12, 3, 1, ACO3)
salva("cofre_aberto", contorno(ca))

est = nova(32, 16)
ret(est, 0, 0, 32, 10, rgb(70, 40, 24))
for x in range(0, 32, 5):
    ret(est, x, 1, 1, 8, rgb(84, 50, 30))
ret(est, 0, 0, 32, 1, rgb(110, 68, 40))
LIVROS = [rgb(140, 30, 30), rgb(40, 70, 130), rgb(50, 110, 60), rgb(160, 120, 50), rgb(90, 50, 110), rgb(170, 160, 140)]
random.seed(3)
x = 1
while x < 31:
    w = random.choice([2, 2, 3])
    ret(est, x, 10, w, random.randint(3, 5), random.choice(LIVROS))
    x += w
salva("estante", contorno(est))

rel = nova(16, 16)
ret(rel, 3, 2, 10, 12, rgb(78, 44, 26))
ret(rel, 3, 2, 10, 1, rgb(110, 66, 40))
ret(rel, 5, 1, 6, 2, OURO)
ret(rel, 4, 11, 8, 3, rgb(100, 60, 36))
px(rel, 8, 12, OURO)
salva("relogio", contorno(rel))

sofa = nova(32, 16)
VEL = rgb(118, 26, 40)
VEL2 = rgb(84, 16, 28)
ret(sofa, 0, 1, 32, 14, VEL2)
ret(sofa, 3, 6, 26, 8, VEL)
ret(sofa, 15, 6, 1, 8, VEL2)
ret(sofa, 0, 1, 32, 4, rgb(70, 12, 22))                   # encosto
ret(sofa, 0, 1, 32, 1, rgb(140, 40, 54))
ret(sofa, 0, 1, 3, 14, rgb(100, 20, 34))
ret(sofa, 29, 1, 3, 14, rgb(100, 20, 34))
salva("sofa", contorno(sofa))


def mesa():
    m = nova(16, 16)
    ret(m, 2, 2, 12, 12, rgb(92, 54, 32))
    ret(m, 3, 3, 10, 10, rgb(110, 66, 38))
    ret(m, 3, 3, 10, 1, rgb(140, 90, 52))
    for y in (6, 9):
        ret(m, 4, y, 8, 1, rgb(98, 58, 34))
    return m


salva("mesa_pequena", contorno(mesa()))

vaso = nova(16, 16)
d = ImageDraw.Draw(vaso)
d.ellipse((3, 3, 12, 12), fill=rgb(170, 90, 50))
d.ellipse((5, 5, 10, 10), fill=rgb(60, 36, 24))
random.seed(11)
for _ in range(10):
    import math
    a = random.uniform(0, 6.28)
    r = random.uniform(2, 7)
    x, y = 7.5 + math.cos(a) * r, 7.5 + math.sin(a) * r
    d.line((7.5, 7.5, x, y), fill=random.choice([rgb(40, 110, 50), rgb(60, 140, 60), rgb(30, 80, 40)]))
salva("vaso", contorno(vaso))


def abajur(aceso):
    a = mesa()
    d = ImageDraw.Draw(a)
    cor = rgb(252, 232, 160) if aceso else rgb(140, 128, 100)
    d.ellipse((4, 4, 11, 11), fill=escurece(cor, 0.8))
    d.ellipse((5, 5, 10, 10), fill=cor)
    if aceso:
        d.ellipse((7, 7, 8, 8), fill=rgb(255, 255, 230))
    return contorno(a)


salva("abajur_apagado", abajur(False))
salva("abajur_aceso", abajur(True))


def candelabro(aceso):
    c = nova(16, 16)
    ret(c, 7, 0, 2, 5, OURO)                                # braco saindo da parede (em cima)
    d = ImageDraw.Draw(c)
    d.ellipse((3, 4, 12, 9), fill=OURO)
    d.ellipse((4, 5, 11, 8), fill=rgb(160, 120, 40))
    for x in (4, 7, 10):
        ret(c, x, 5, 2, 2, rgb(236, 230, 210))
        if aceso:
            px(c, x, 5, rgb(255, 230, 120))
            px(c, x + 1, 6, rgb(255, 150, 40))
    return contorno(c)


salva("candelabro_apagado", candelabro(False))
salva("candelabro_aceso", candelabro(True))


def vela(f):
    v = nova(16, 16)
    d = ImageDraw.Draw(v)
    d.ellipse((6, 6, 9, 9), fill=rgb(236, 230, 210))
    d.ellipse((5, 9, 10, 11), fill=OURO)
    if f == 0:
        px(v, 7, 6, rgb(255, 230, 120)); px(v, 8, 7, rgb(255, 150, 40))
    else:
        px(v, 8, 6, rgb(255, 230, 120)); px(v, 7, 7, rgb(255, 170, 50)); px(v, 8, 5, rgb(255, 200, 80))
    return contorno(v)


salva("vela_0", vela(0))
salva("vela_1", vela(1))


def interruptor(ligado):
    s = nova(16, 16)
    ret(s, 5, 4, 6, 8, rgb(222, 212, 182))
    ret(s, 5, 4, 6, 1, rgb(250, 244, 220))
    ret(s, 7, 5 if ligado else 8, 2, 3, rgb(60, 50, 44))
    px(s, 7, 10 if ligado else 5, rgb(60, 200, 80) if ligado else rgb(200, 50, 40))
    return contorno(s)


salva("interruptor_off", interruptor(False))
salva("interruptor_on", interruptor(True))

# retrato visto de cima: so a borda dourada da moldura presa na parede
rt = nova(32, 8)
ret(rt, 0, 1, 32, 4, OURO)
ret(rt, 0, 2, 32, 1, rgb(240, 210, 110))
ret(rt, 1, 4, 30, 1, rgb(150, 110, 40))
ret(rt, 2, 5, 28, 2, (0, 0, 0, 90))
salva("retrato", rt)

# retrato em close (48x64), usado na tela de leitura
rc = nova(48, 64)
d = ImageDraw.Draw(rc)
d.rectangle((0, 0, 47, 63), fill=OURO)
d.rectangle((2, 2, 45, 61), fill=rgb(150, 110, 40))
d.rectangle((4, 4, 43, 50), fill=rgb(28, 50, 38))
d.rectangle((8, 34, 39, 50), fill=rgb(28, 26, 34))
d.polygon([(20, 34), (27, 34), (24, 46)], fill=rgb(230, 230, 230))
d.rectangle((23, 36, 24, 44), fill=rgb(150, 20, 30))
d.rectangle((14, 39, 16, 41), fill=SERPENTE)
d.ellipse((15, 12, 32, 33), fill=rgb(226, 186, 150))
d.ellipse((13, 8, 34, 18), fill=rgb(220, 220, 224))
d.rectangle((17, 18, 30, 19), fill=rgb(200, 200, 204))
px(rc, 19, 21, CONTORNO); px(rc, 27, 21, CONTORNO)
d.ellipse((25, 19, 30, 24), outline=OURO)
d.rectangle((19, 27, 28, 28), fill=rgb(220, 220, 224))
d.rectangle((21, 30, 26, 30), fill=rgb(160, 90, 80))
d.rectangle((10, 53, 37, 59), fill=rgb(240, 210, 110))
d.rectangle((12, 55, 35, 57), fill=rgb(170, 130, 50))
salva("retrato_close", rc)

for v in range(2):
    random.seed(20 + v)
    p = nova(16, 16)
    d = ImageDraw.Draw(p)
    d.ellipse((2, 3, 13, 12), fill=rgb(110, 14, 18))
    for _ in range(5):
        x = random.randint(1, 12)
        y = random.randint(2, 12)
        d.ellipse((x, y, x + 3, y + 2), fill=rgb(110, 14, 18))
    d.ellipse((5, 6, 9, 9), fill=rgb(140, 20, 24))
    salva(f"poca_{v}", p)

# ---------------------------------------------------------------- itens (icones)
it = nova(16, 16)
ret(it, 3, 6, 10, 3, rgb(96, 96, 108))
ret(it, 3, 6, 10, 1, rgb(150, 150, 164))
ret(it, 13, 7, 1, 1, rgb(60, 60, 70))
ret(it, 4, 9, 3, 4, rgb(70, 50, 38))
px(it, 8, 9, rgb(60, 60, 70)); px(it, 8, 10, rgb(60, 60, 70)); px(it, 7, 10, rgb(60, 60, 70))
salva("item_pistola", contorno(it))

it = nova(16, 16)
for i in range(4):
    ret(it, 4 + i * 2, 4, 1, 3, rgb(226, 194, 96))
    px(it, 4 + i * 2, 3, rgb(180, 120, 60))
ret(it, 3, 6, 10, 7, rgb(108, 116, 60))
ret(it, 3, 6, 10, 1, rgb(140, 150, 80))
ret(it, 4, 9, 8, 2, rgb(226, 194, 96))
salva("item_municao", contorno(it))

it = nova(16, 16)
for i in range(7):
    px(it, 5 + i, 10 - i, rgb(214, 214, 224))
    px(it, 6 + i, 10 - i, rgb(160, 160, 176))
px(it, 12, 3, rgb(240, 240, 250))
ret(it, 3, 10, 3, 3, rgb(70, 44, 30))
px(it, 6, 9, rgb(190, 150, 60))
salva("item_faca", contorno(it))

it = nova(16, 16)
ret(it, 6, 5, 4, 9, rgb(60, 160, 84))
ret(it, 6, 5, 1, 9, rgb(100, 200, 120))
ret(it, 7, 3, 2, 2, rgb(220, 220, 220))
ret(it, 6, 8, 4, 3, rgb(240, 240, 240))
px(it, 8, 9, rgb(200, 30, 30)); px(it, 7, 9, rgb(200, 30, 30)); px(it, 8, 8, rgb(200, 30, 30)); px(it, 8, 10, rgb(200, 30, 30))
salva("item_spray", contorno(it))

it = nova(16, 16)
ret(it, 4, 2, 8, 12, rgb(232, 222, 192))
for y in (4, 6, 8, 10):
    ret(it, 5, y, 6 if y != 10 else 4, 1, rgb(120, 110, 100))
ret(it, 10, 2, 2, 2, rgb(190, 180, 150))
px(it, 9, 11, rgb(150, 20, 24)); px(it, 10, 12, rgb(150, 20, 24))
salva("item_documento", contorno(it))

it = nova(16, 16)
ret(it, 2, 4, 5, 5, OURO)
ret(it, 3, 5, 3, 3, T)
px(it, 4, 6, SERPENTE)
ret(it, 7, 6, 7, 1, OURO)
ret(it, 7, 7, 7, 1, rgb(160, 120, 40))
ret(it, 11, 8, 1, 2, OURO)
ret(it, 13, 8, 1, 3, OURO)
salva("item_chave", contorno(it))

# ---------------------------------------------------------------- efeitos e UI
salva("pixel", Image.new("RGBA", (1, 1), (255, 255, 255, 255)))

cl = nova(8, 8)
for x, y in ((3, 0), (4, 0), (3, 7), (4, 7), (0, 3), (0, 4), (7, 3), (7, 4)):
    px(cl, x, y, rgb(255, 200, 60))
ret(cl, 2, 2, 4, 4, rgb(255, 230, 120))
ret(cl, 1, 3, 6, 2, rgb(255, 230, 120))
ret(cl, 3, 1, 2, 6, rgb(255, 230, 120))
ret(cl, 3, 3, 2, 2, rgb(255, 255, 255))
salva("clarao", cl)

br = nova(8, 8)
ret(br, 3, 0, 1, 7, rgb(255, 255, 220))
ret(br, 0, 3, 7, 1, rgb(255, 255, 220))
px(br, 3, 3, rgb(255, 255, 255))
salva("brilho", br)

m = nova(16, 16)
BR = rgb(240, 240, 240)
for x, y in ((7, 1), (7, 2), (7, 3), (7, 11), (7, 12), (7, 13), (1, 7), (2, 7), (3, 7), (11, 7), (12, 7), (13, 7)):
    px(m, x, y, BR)
px(m, 7, 7, rgb(255, 60, 60))
salva("mira", contorno(m))


def painel(nome, fundo, borda, luz):
    p = nova(16, 16)
    ret(p, 0, 0, 16, 16, borda)
    ret(p, 2, 2, 12, 12, fundo)
    ret(p, 2, 2, 12, 1, luz)
    salva(nome, p)


painel("ui_painel", rgb(18, 14, 20), rgb(130, 100, 64), rgb(36, 28, 38))
painel("ui_slot", rgb(28, 22, 30), rgb(84, 70, 60), rgb(44, 36, 46))
painel("ui_slot_sel", rgb(40, 32, 30), rgb(232, 190, 80), rgb(60, 50, 40))
painel("ui_botao", rgb(56, 34, 30), rgb(150, 112, 66), rgb(86, 54, 44))
painel("ui_papel", rgb(226, 214, 180), rgb(120, 96, 70), rgb(240, 232, 206))

# fundo do menu (320x180): mansao na noite
f = Image.new("RGBA", (320, 180), rgb(10, 10, 24))
d = ImageDraw.Draw(f)
random.seed(5)
for y in range(0, 120, 6):
    d.rectangle((0, y, 319, y + 5), fill=rgb(10 + y // 10, 10 + y // 14, 26 + y // 8))
for _ in range(50):
    px(f, random.randint(0, 319), random.randint(0, 90), rgb(200, 200, 220))
d.ellipse((240, 18, 270, 48), fill=rgb(236, 232, 200))
d.ellipse((248, 22, 262, 34), fill=rgb(214, 210, 180))
SIL = rgb(6, 6, 12)
d.rectangle((60, 70, 260, 160), fill=SIL)
d.polygon([(55, 70), (160, 30), (265, 70)], fill=SIL)
d.rectangle((140, 20, 180, 70), fill=SIL)
d.polygon([(136, 20), (160, 2), (184, 20)], fill=SIL)
d.rectangle((80, 50, 96, 70), fill=SIL)
d.rectangle((222, 48, 236, 70), fill=SIL)
for wx in range(72, 250, 22):
    for wy in (86, 116):
        acesa = random.random() < 0.25
        d.rectangle((wx, wy, wx + 8, wy + 12), fill=rgb(230, 190, 90) if acesa else rgb(24, 24, 40))
d.rectangle((150, 130, 170, 160), fill=rgb(60, 30, 20))
d.rectangle((0, 160, 319, 179), fill=rgb(8, 14, 10))
for x in range(0, 320, 3):
    h = random.randint(2, 7)
    d.line((x, 160, x, 160 - h), fill=rgb(8, 14, 10))
for tx in (20, 290):
    d.rectangle((tx, 100, tx + 4, 160), fill=SIL)
    d.ellipse((tx - 16, 70, tx + 20, 112), fill=SIL)
salva("fundo_menu", f)

print("sprites gerados em", SAIDA)
