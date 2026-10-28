# Gera os efeitos sonoros e as musicas (chiptune) em Assets/Audio.
# Uso: python Ferramentas/gera_sons.py
import os
import wave
import numpy as np

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SAIDA = os.path.join(RAIZ, "Assets", "Audio")
os.makedirs(SAIDA, exist_ok=True)
SR = 22050
rng = np.random.default_rng(42)


def t(dur):
    return np.arange(int(SR * dur)) / SR


def quadrada(freq, dur, duty=0.5):
    fase = np.cumsum(np.broadcast_to(freq, t(dur).shape) / SR)
    return np.where((fase % 1.0) < duty, 1.0, -1.0)


def triangulo(freq, dur):
    fase = np.cumsum(np.broadcast_to(freq, t(dur).shape) / SR)
    return 4 * np.abs((fase % 1.0) - 0.5) - 1


def ruido(dur, passo=1):
    n = int(SR * dur)
    r = rng.uniform(-1, 1, n // passo + 1)
    return np.repeat(r, passo)[:n]


def env(n, ataque=0.005, queda=None, sustain=1.0):
    e = np.ones(n) * sustain
    a = max(1, int(SR * ataque))
    e[:a] = np.linspace(0, 1, a)
    if queda is not None:
        e = e * np.exp(-np.arange(n) / (SR * queda))
    return e


def salva(nome, sinal, vol=0.8):
    s = np.clip(sinal, -1, 1) * vol
    dados = (s * 32767).astype(np.int16)
    with wave.open(os.path.join(SAIDA, nome + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(dados.tobytes())


def junta(*partes):
    return np.concatenate(partes)


def mistura(*sinais):
    n = max(len(s) for s in sinais)
    out = np.zeros(n)
    for s in sinais:
        out[:len(s)] += s
    return out


def nota(n):
    return 440.0 * 2 ** ((n - 69) / 12)


# ------------------------------------------------------------------ efeitos
d = 0.35
salva("tiro", mistura(ruido(d, 2) * env(len(t(d)), queda=0.05),
                      quadrada(np.linspace(180, 40, len(t(d))), d) * env(len(t(d)), queda=0.06) * 0.7), 0.9)

d = 0.06
salva("vazio", junta(quadrada(1800, 0.01) * 0.5, np.zeros(int(SR * 0.03)), quadrada(1200, 0.02) * 0.3) , 0.6)

clique = quadrada(1500, 0.015) * env(len(t(0.015)), queda=0.005)
deslize = ruido(0.12, 3) * env(len(t(0.12)), queda=0.05) * 0.5
salva("recarga", junta(clique, np.zeros(int(SR * 0.25)), deslize, np.zeros(int(SR * 0.1)), clique, np.zeros(int(SR * 0.05)), clique), 0.7)

d = 0.18
salva("faca", ruido(d, 1) * env(len(t(d)), ataque=0.03, queda=0.05) * np.linspace(0.2, 1, len(t(d))) ** 2, 0.6)

d = 0.15
salva("impacto", mistura(ruido(d, 6) * env(len(t(d)), queda=0.04),
                         quadrada(np.linspace(120, 60, len(t(d))), d) * env(len(t(d)), queda=0.03) * 0.5), 0.7)

d = 1.4
f = 70 + 12 * np.sin(2 * np.pi * 5.5 * t(d)) + np.linspace(0, -15, len(t(d)))
salva("gemido", mistura(quadrada(f, d, 0.3) * env(len(t(d)), ataque=0.2) * np.linspace(1, 0, len(t(d))) * 0.6,
                        ruido(d, 18) * 0.25 * np.linspace(1, 0, len(t(d)))), 0.7)

d = 1.0
f = np.linspace(55, 95, len(t(d))) + 8 * np.sin(2 * np.pi * 9 * t(d))
salva("grunhido", mistura(quadrada(f, d, 0.25) * env(len(t(d)), ataque=0.05) * np.linspace(1, 0.2, len(t(d))),
                          ruido(d, 10) * 0.5 * np.linspace(1, 0, len(t(d)))), 0.85)

d = 0.4
salva("dano", mistura(quadrada(np.linspace(600, 120, len(t(d))), d) * env(len(t(d)), queda=0.12) * 0.6,
                      ruido(d, 4) * env(len(t(d)), queda=0.05) * 0.6), 0.8)

salva("pegar", junta(*[quadrada(nota(n), 0.06, 0.25) * env(len(t(0.06)), queda=0.05) for n in (72, 76, 79, 84)]), 0.5)

salva("bip", quadrada(988, 0.06, 0.5) * env(len(t(0.06)), queda=0.04), 0.4)
salva("erro", quadrada(110, 0.45, 0.5) * env(len(t(0.45)), queda=0.3), 0.5)
salva("cofre", junta(quadrada(90, 0.12) * env(len(t(0.12)), queda=0.05) * 0.8, np.zeros(int(SR * 0.1)),
                     *[quadrada(nota(n), 0.09, 0.25) * env(len(t(0.09)), queda=0.08) for n in (67, 71, 74, 79, 83)]), 0.6)

batida = mistura(ruido(0.08, 5) * env(len(t(0.08)), queda=0.02), quadrada(90, 0.08) * env(len(t(0.08)), queda=0.02) * 0.6)
salva("trancada", junta(batida, np.zeros(int(SR * 0.07)), batida, np.zeros(int(SR * 0.07)), batida * 0.7), 0.7)

d = 0.9
f = 300 + 120 * np.sin(2 * np.pi * 3 * t(d)) + np.linspace(0, 200, len(t(d)))
salva("porta", mistura(quadrada(f, d, 0.15) * 0.3 * env(len(t(d)), ataque=0.05) * np.linspace(1, 0, len(t(d))),
                       ruido(d, 30) * 0.2), 0.6)

salva("interruptor", junta(quadrada(2200, 0.01) * 0.6, np.zeros(int(SR * 0.02)), quadrada(1400, 0.015) * 0.4), 0.6)

d = 0.25
salva("papel", ruido(d, 2) * env(len(t(d)), ataque=0.04, queda=0.06) * 0.6, 0.5)

salva("cura", junta(ruido(0.3, 1) * env(len(t(0.3)), ataque=0.02, queda=0.15) * 0.5,
                    *[triangulo(nota(n), 0.08) * env(len(t(0.08)), queda=0.06) for n in (64, 67, 72)]), 0.6)

salva("faca_quebra", mistura(quadrada(np.linspace(2400, 900, len(t(0.25))), 0.25, 0.2) * env(len(t(0.25)), queda=0.08),
                             ruido(0.25, 2) * env(len(t(0.25)), queda=0.04) * 0.5), 0.6)

d = 2.2
salva("morte", mistura(quadrada(np.linspace(220, 40, len(t(d))), d, 0.5) * env(len(t(d)), queda=0.9) * 0.6,
                       triangulo(np.linspace(110, 30, len(t(d))), d) * env(len(t(d)), queda=1.2)), 0.8)


# ------------------------------------------------------------------ musicas
def sequencia(notas, bpm, gerador, duty=0.5, volume=1.0, queda=0.25):
    """notas: lista de (midi ou None, duracao em batidas)."""
    partes = []
    for n, dur in notas:
        seg = 60 / bpm * dur
        if n is None:
            partes.append(np.zeros(len(t(seg))))
        else:
            s = gerador(nota(n), seg) if gerador is triangulo else gerador(nota(n), seg, duty)
            partes.append(s * env(len(s), ataque=0.01, queda=queda) * volume)
    return np.concatenate(partes)


# ambiente: drone grave em re menor, sinos esparsos (32 batidas a 60 bpm = 32s)
BPM = 60
baixo = sequencia([(38, 8), (36, 8), (34, 8), (33, 8)], BPM, triangulo, volume=0.55, queda=6)
sinos = sequencia([(None, 2), (74, 1), (None, 3), (77, 1), (None, 1), (76, 2), (None, 6), (69, 1), (None, 3),
                   (74, 1), (None, 2), (73, 2), (None, 5)], BPM, quadrada, duty=0.125, volume=0.18, queda=0.6)
pad = sequencia([(50, 8), (48, 8), (46, 8), (45, 8)], BPM, quadrada, duty=0.25, volume=0.06, queda=8)
amb = mistura(baixo, sinos, pad)
amb = amb + ruido(len(amb) / SR, 40)[:len(amb)] * 0.02
salva("musica_ambiente", amb, 0.7)

# tensa: ostinato rapido, dissonancias e percussao (16 compassos de 4 a 150 bpm)
BPM = 150
ost = []
for comp in range(8):
    raiz = [38, 38, 39, 38, 41, 40, 39, 37][comp]
    for _ in range(2):
        ost += [(raiz, 0.5), (raiz + 12, 0.5), (raiz, 0.5), (raiz + 6, 0.5)]
osti = sequencia(ost, BPM, quadrada, duty=0.25, volume=0.35, queda=0.12)
stabs = sequencia([((62 if i % 2 == 0 else 63) if i % 4 != 3 else 68, 4) for i in range(8)], BPM,
                  quadrada, duty=0.5, volume=0.12, queda=0.4)
perc = []
for i in range(64):
    seg = 60 / BPM
    if i % 2 == 0:
        b = mistura(quadrada(np.linspace(120, 40, len(t(0.12))), 0.12) * env(len(t(0.12)), queda=0.04) * 0.6)
    else:
        b = ruido(0.06, 1) * env(len(t(0.06)), queda=0.015) * 0.25
    perc.append(junta(b, np.zeros(max(0, len(t(seg)) - len(b))))[:len(t(seg))])
perc = np.concatenate(perc)
tensa = mistura(osti, stabs, perc)
salva("musica_tensa", tensa, 0.75)

print("sons gerados em", SAIDA)
