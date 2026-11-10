"""Gera todos os sons e músicas (originais) em Assets/Resources/Audio.

Síntese no estilo 8-bit: ondas quadradas com duty variável, triângulo e
ruído (como os canais do NES), envelopes curtos e um pouco de eco.
Rode: python3 Tools/gerar_audio.py
"""
import os
import wave

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', 'Assets', 'StreamingAssets', 'Audio')
os.makedirs(OUT, exist_ok=True)
SR = 22050
rng = np.random.default_rng(935 + 1)


def hz(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def t(d):
    return np.arange(int(SR * d)) / SR


def square(f, d, duty=0.5):
    ph = (np.cumsum(np.full(int(SR * d), f) / SR) if np.isscalar(f) else np.cumsum(f / SR)) % 1.0
    return np.where(ph < duty, 1.0, -1.0)


def tri(f, d):
    ph = (np.cumsum(np.full(int(SR * d), f) / SR) if np.isscalar(f) else np.cumsum(f / SR)) % 1.0
    return 4 * np.abs(ph - 0.5) - 1


def noise(d, period=1):
    n = int(SR * d)
    base = rng.uniform(-1, 1, n // period + 1)
    return np.repeat(base, period)[:n]


def env(n, a=0.002, dcy=0.1, s=0.0, r=0.05, total=None):
    total = total or n / SR
    tt = np.arange(n) / SR
    e = np.ones(n)
    e = np.where(tt < a, tt / max(a, 1e-6), e)
    dec = np.clip((tt - a) / max(dcy, 1e-6), 0, 1)
    e = np.where(tt >= a, 1 - (1 - s) * dec, e)
    rel = np.clip((total - tt) / max(r, 1e-6), 0, 1)
    return e * rel


def expdecay(n, k):
    return np.exp(-np.arange(n) / SR * k)


def sweep(f0, f1, d, curve=1.0):
    k = np.linspace(0, 1, int(SR * d)) ** curve
    return f0 + (f1 - f0) * k


def lowpass(x, a):
    y = np.zeros_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a * (x[i] - acc)
        y[i] = acc
    return y


def echo(x, delay=0.12, fb=0.35, taps=3):
    out = np.concatenate([x, np.zeros(int(SR * delay * taps))])
    for k in range(1, taps + 1):
        off = int(SR * delay * k)
        out[off:off + len(x)] += x * (fb ** k)
    return out


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def place(buf, x, at):
    i = int(at * SR)
    end = min(len(buf), i + len(x))
    if i < len(buf):
        buf[i:end] += x[:end - i]


def write(name, x, peak=0.85):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


# ================================================================== armas
def gunshot(d, body_hz, crack, low=0.4, duty=0.25, k=18):
    n = int(SR * d)
    ns = noise(d, 2) * expdecay(n, k) * crack
    pitch = sweep(body_hz, body_hz * 0.35, d, 0.5)
    sq = square(pitch, d, duty) * expdecay(n, k * 0.8) * low
    thump = tri(sweep(110, 40, d), d) * expdecay(n, 10) * 0.6
    return mix(ns, sq, thump)


def weapons():
    write('ShotPistol', gunshot(0.18, 900, 1.0, 0.5, k=26))
    write('ShotMagnum', echo(gunshot(0.35, 500, 1.0, 0.7, k=12), 0.07, 0.3))
    write('ShotRifle', echo(gunshot(0.4, 420, 1.0, 0.8, k=10), 0.09, 0.3))
    write('ShotSniper', echo(gunshot(0.6, 260, 1.0, 1.0, k=7), 0.12, 0.35))
    write('ShotAuto', gunshot(0.14, 600, 1.0, 0.6, k=30))
    write('ShotSmg', gunshot(0.11, 1100, 0.9, 0.4, 0.125, k=38))
    write('ShotShotgun', echo(gunshot(0.45, 300, 1.0, 0.9, 0.5, k=9) + noise(0.45, 4) * expdecay(int(SR * 0.45), 8) * 0.6, 0.08, 0.3))
    d = 0.5
    write('ShotLauncher', mix(noise(d, 3) * expdecay(int(SR * d), 6) * 0.8,
                              square(sweep(200, 60, d), d, 0.5) * expdecay(int(SR * d), 5) * 0.5))
    d = 0.2
    write('Flame', lowpass(noise(d, 1), 0.25) * env(int(SR * d), 0.02, 0.1, 0.6, 0.06))
    d = 0.25
    f = sweep(1800, 300, d, 0.6)
    write('ShotRay', mix(square(f, d, 0.25) * expdecay(int(SR * d), 9), tri(f * 0.5, d) * expdecay(int(SR * d), 9) * 0.6))
    d = 0.3
    write('RayHit', mix(square(sweep(700, 1600, d), d, 0.5) * expdecay(int(SR * d), 10) * 0.5, noise(d, 6) * expdecay(int(SR * d), 14) * 0.5))

    d = 1.1
    n = int(SR * d)
    boom = mix(noise(d, 6) * expdecay(n, 3.5), noise(d, 2) * expdecay(n, 14) * 0.8,
               tri(sweep(90, 28, d), d) * expdecay(n, 3) * 1.0)
    write('Explosion', echo(lowpass(boom, 0.35), 0.15, 0.25))
    write('Tick', square(1800, 0.03, 0.5) * expdecay(int(SR * 0.03), 80))
    write('Throw', lowpass(noise(0.18, 1), 0.2) * np.sin(np.linspace(0, np.pi, int(SR * 0.18))))
    d = 0.8
    write('Molotov', mix(noise(0.08, 1) * 0.8, np.concatenate([np.zeros(int(SR * 0.06)), lowpass(noise(d, 1), 0.15) * env(int(SR * d), 0.05, 0.3, 0.5, 0.3)])))
    write('BettyPlace', mix(square(400, 0.05) * 0.4, np.concatenate([np.zeros(int(SR * 0.08)), square(900, 0.05) * 0.4])))
    write('BettyJump', mix(square(sweep(300, 1200, 0.25), 0.25, 0.5) * 0.5, noise(0.25, 2) * expdecay(int(SR * 0.25), 12) * 0.5))
    write('DryFire', square(2400, 0.02, 0.5) * expdecay(int(SR * 0.02), 120) * 0.6)
    write('ReloadOut', mix(noise(0.06, 2) * expdecay(int(SR * 0.06), 50), np.concatenate([np.zeros(int(SR * 0.08)), square(600, 0.04, 0.25) * 0.4])))
    write('ReloadIn', mix(square(900, 0.03, 0.25) * 0.5, np.concatenate([np.zeros(int(SR * 0.07)), noise(0.07, 2) * expdecay(int(SR * 0.07), 40)])))
    write('Swap', mix(square(500, 0.04, 0.25) * 0.4, np.concatenate([np.zeros(int(SR * 0.05)), square(750, 0.04, 0.25) * 0.4])))
    write('KnifeSwing', lowpass(noise(0.12, 1), 0.5) * np.sin(np.linspace(0, np.pi, int(SR * 0.12))))
    write('KnifeHit', mix(noise(0.1, 3) * expdecay(int(SR * 0.1), 30), tri(sweep(220, 90, 0.1), 0.1) * 0.8))
    d = 0.22
    write('Crit', echo(mix(square(sweep(1900, 2600, d, 0.3), d, 0.125) * expdecay(int(SR * d), 22) * 0.6,
                           tri(hz(100), d) * expdecay(int(SR * d), 30) * 0.5,
                           noise(0.03, 1) * 0.5), 0.05, 0.25))
    write('Hit', mix(noise(0.06, 4) * expdecay(int(SR * 0.06), 50), tri(sweep(180, 90, 0.06), 0.06) * 0.5))


# ================================================================== zumbis e jogador
def groan(d, f0, f1, rough=0.35, seed=0):
    n = int(SR * d)
    vib = 1 + 0.04 * np.sin(2 * np.pi * 5.5 * t(d))
    f = sweep(f0, f1, d, 1.2) * vib
    voice = mix(square(f, d, 0.3) * 0.5, tri(f * 0.5, d) * 0.6, lowpass(noise(d, 3), 0.2) * rough)
    return lowpass(voice, 0.18) * env(n, 0.08, d * 0.5, 0.6, d * 0.4)


def creatures():
    write('Groan0', groan(1.1, 110, 80))
    write('Groan1', groan(0.9, 95, 120))
    write('Groan2', groan(1.3, 130, 70, 0.5))
    write('GroanRun', groan(0.6, 180, 140, 0.6))
    write('ZombieAttack', mix(groan(0.35, 210, 120, 0.8), noise(0.35, 2) * expdecay(int(SR * 0.35), 9) * 0.3))
    write('ZombieDie0', mix(groan(0.6, 150, 50, 0.5), noise(0.2, 3) * expdecay(int(SR * 0.2), 20) * 0.5))
    write('ZombieDie1', mix(groan(0.5, 120, 40, 0.7), noise(0.15, 2) * expdecay(int(SR * 0.15), 25) * 0.6))
    d = 0.35
    write('PlayerHurt', mix(noise(d, 2) * expdecay(int(SR * d), 12) * 0.7, square(sweep(500, 180, d), d, 0.5) * expdecay(int(SR * d), 8) * 0.5))
    d = 1.4
    write('PlayerDown', echo(square(sweep(400, 60, d, 0.6), d, 0.5) * env(int(SR * d), 0.01, 0.5, 0.5, 0.5) * 0.6, 0.2, 0.4))
    notes = [60, 64, 67, 72]
    buf = np.zeros(int(SR * 0.9))
    for i, nn in enumerate(notes):
        place(buf, square(hz(nn), 0.15, 0.25) * expdecay(int(SR * 0.15), 12), i * 0.1)
    write('Revive', echo(buf, 0.1, 0.3))
    write('Step', lowpass(noise(0.05, 4), 0.3) * expdecay(int(SR * 0.05), 60))
    write('Drink', mix(*[np.concatenate([np.zeros(int(SR * i * 0.13)), tri(hz(50 + i * 2), 0.09) * expdecay(int(SR * 0.09), 25)]) for i in range(5)],
                       np.concatenate([np.zeros(int(SR * 0.7)), square(hz(76), 0.12, 0.25) * 0.5])))


# ================================================================== mundo e interface
def chord(notes, d, duty=0.25, k=6):
    return mix(*[square(hz(n), d, duty) * expdecay(int(SR * d), k) for n in notes])


def world():
    write('Buy', mix(square(hz(84), 0.06, 0.25), np.concatenate([np.zeros(int(SR * 0.06)), square(hz(91), 0.1, 0.25) * expdecay(int(SR * 0.1), 20)])))
    write('Deny', square(hz(40), 0.25, 0.5) * env(int(SR * 0.25), 0.005, 0.1, 0.8, 0.05) * 0.7)
    write('AmmoBuy', mix(noise(0.1, 2) * expdecay(int(SR * 0.1), 30), np.concatenate([np.zeros(int(SR * 0.1)), square(hz(79), 0.08, 0.25) * 0.5])))
    write('WeaponBuy', echo(mix(chord([67, 74], 0.2), np.concatenate([np.zeros(int(SR * 0.12)), chord([72, 79], 0.3)])), 0.1, 0.3))
    d = 0.9
    write('DoorOpen', mix(lowpass(noise(d, 3), 0.2) * env(int(SR * d), 0.01, 0.3, 0.4, 0.3),
                          square(sweep(140, 90, d), d, 0.5) * expdecay(int(SR * d), 3) * 0.3,
                          noise(0.15, 1) * expdecay(int(SR * 0.15), 25) * 0.8))
    d = 1.2
    write('DoorPower', mix(square(sweep(60, 120, d), d, 0.5) * env(int(SR * d), 0.05, 0.6, 0.6, 0.4) * 0.6, noise(d, 8) * expdecay(int(SR * d), 3) * 0.4))
    d = 2.8
    hum = mix(square(sweep(30, 60, d, 0.5), d, 0.5) * 0.5, tri(sweep(60, 120, d, 0.5), d) * 0.6)
    write('PowerOn', echo(mix(noise(0.3, 1) * expdecay(int(SR * 0.3), 8), hum * env(int(SR * d), 0.3, 1.2, 0.6, 1.0)), 0.25, 0.35))
    write('Plank', mix(noise(0.08, 2) * expdecay(int(SR * 0.08), 40), tri(sweep(300, 150, 0.08), 0.08) * 0.6))
    write('PlankBreak', mix(noise(0.25, 3) * expdecay(int(SR * 0.25), 14), square(sweep(200, 80, 0.2), 0.2, 0.5) * expdecay(int(SR * 0.2), 12) * 0.4))
    d = 1.6
    write('Flush', lowpass(noise(d, 1), 0.12) * env(int(SR * d), 0.1, 0.8, 0.4, 0.6))
    write('Paper', lowpass(noise(0.2, 1), 0.6) * env(int(SR * 0.2), 0.01, 0.1, 0.3, 0.05))

    # jingle da caixa: caixinha de música curta, em lá menor (original)
    seq = [81, 84, 88, 86, 84, 81, 83, 80, 81, 76, 81, 84]
    buf = np.zeros(int(SR * 3.8))
    for i, nn in enumerate(seq):
        place(buf, tri(hz(nn), 0.28) * expdecay(int(SR * 0.28), 9) * 0.8, i * 0.29)
        if i % 3 == 0:
            place(buf, square(hz(nn - 24), 0.25, 0.5) * expdecay(int(SR * 0.25), 8) * 0.25, i * 0.29)
    write('BoxJingle', echo(buf, 0.14, 0.3))
    write('BoxReveal', echo(chord([69, 76, 81], 0.6, 0.25, 4), 0.12, 0.35))
    write('BoxClose', mix(noise(0.12, 3) * expdecay(int(SR * 0.12), 25), tri(sweep(120, 70, 0.12), 0.12)))
    write('BoxLand', echo(mix(noise(0.4, 4) * expdecay(int(SR * 0.4), 8), chord([57, 64, 69], 0.5, 0.5, 5)), 0.15, 0.3))
    buf = np.zeros(int(SR * 2.2))
    for i, nn in enumerate([76, 75, 74, 73, 72]):
        place(buf, tri(hz(nn), 0.35) * expdecay(int(SR * 0.35), 5), i * 0.32)
    write('Teddy', echo(buf, 0.2, 0.4))
    write('TrapOn', mix(noise(0.15, 1) * expdecay(int(SR * 0.15), 20), square(sweep(80, 240, 0.5), 0.5, 0.5) * env(int(SR * 0.5), 0.01, 0.3, 0.5, 0.1) * 0.5))
    d = 0.45
    buzz = square(120 + 30 * np.sign(np.sin(2 * np.pi * 9 * t(d))), d, 0.5) * 0.5 + noise(d, 2) * 0.35
    write('TrapBuzz', buzz * env(int(SR * d), 0.01, 0.1, 0.8, 0.05))

    # power-up: arpejo ascendente
    buf = np.zeros(int(SR * 1.0))
    for i, nn in enumerate([72, 76, 79, 84, 88]):
        place(buf, square(hz(nn), 0.12, 0.125) * expdecay(int(SR * 0.12), 14), i * 0.07)
    write('PowerUp', echo(buf, 0.09, 0.35))
    write('PowerUpSpawn', echo(mix(tri(hz(88), 0.15) * expdecay(int(SR * 0.15), 10), np.concatenate([np.zeros(int(SR * 0.1)), tri(hz(95), 0.2) * expdecay(int(SR * 0.2), 10)])), 0.12, 0.3))

    write('UiClick', square(hz(88), 0.04, 0.25) * expdecay(int(SR * 0.04), 40))
    d = 1.1
    hiss = lowpass(noise(d, 1), 0.6) * env(int(SR * d), 0.01, 0.4, 0.35, 0.5)
    write('Overheat', mix(hiss, square(sweep(900, 300, 0.25), 0.25, 0.25) * expdecay(int(SR * 0.25), 12) * 0.4))
    d = 1.6
    write('Thunder', echo(mix(noise(0.12, 1) * 1.0, lowpass(noise(d, 4), 0.08) * expdecay(int(SR * d), 2.5) * 1.6), 0.2, 0.3))
    write('DogBark', mix(square(sweep(520, 300, 0.12), 0.12, 0.3) * expdecay(int(SR * 0.12), 14),
                         noise(0.12, 3) * expdecay(int(SR * 0.12), 20) * 0.5))
    write('DogGrowl', lowpass(mix(square(70 + 8 * np.sin(2 * np.pi * 30 * t(0.7)), 0.7, 0.4) * 0.6, noise(0.7, 4) * 0.5), 0.2)
          * env(int(SR * 0.7), 0.05, 0.3, 0.7, 0.2))
    write('DogDie', mix(square(sweep(700, 200, 0.4), 0.4, 0.3) * expdecay(int(SR * 0.4), 6), noise(0.2, 2) * expdecay(int(SR * 0.2), 15) * 0.4))
    d = 1.0
    write('PanzerRoar', echo(lowpass(mix(square(sweep(90, 60, d), d, 0.45) * 0.7, noise(d, 6) * 0.5,
                                          tri(sweep(45, 35, d), d) * 0.6), 0.25) * env(int(SR * d), 0.08, 0.5, 0.6, 0.3), 0.12, 0.3))
    d = 1.4
    write('PanzerLand', mix(lowpass(noise(d, 1), 0.4) * np.linspace(0.2, 1, int(SR * d)) * 0.6,
                            square(sweep(200, 600, d), d, 0.5) * 0.15))
    write('Claw', mix(square(sweep(300, 900, 0.3), 0.3, 0.25) * expdecay(int(SR * 0.3), 6) * 0.5,
                      noise(0.08, 2) * 0.6))
    d = 3.4
    mach = mix(square(55 + 5 * np.sign(np.sin(2 * np.pi * 6 * t(d))), d, 0.5) * 0.3, noise(d, 12) * 0.25)
    for i in range(6):
        mach[int(SR * (0.4 + i * 0.5)):int(SR * (0.4 + i * 0.5)) + int(SR * 0.08)] += noise(0.08, 2) * 0.9
    write('PackPunch', mach * env(int(SR * d), 0.05, 1.0, 0.8, 0.4))
    buf = np.zeros(int(SR * 2.6))
    for i, nn in enumerate([67, 71, 74, 79, 78, 79, 83]):
        place(buf, square(hz(nn), 0.22, 0.25) * expdecay(int(SR * 0.22), 7) * 0.6, i * 0.2)
        place(buf, tri(hz(nn - 24), 0.2) * 0.45, i * 0.2)
    write('PackPunchDone', echo(buf, 0.12, 0.3))
    write('DogRound', stinger([45, 46, 45, 46, 52, 51], 0.28, 0.9, 0.5))
    write('UiMove', square(hz(81), 0.025, 0.125) * expdecay(int(SR * 0.025), 60) * 0.6)

    # jingles dos refrigerantes (melodias originais, curtinhas)
    jingles = {
        'FerroNog': ([57, 57, 60, 64, 62, 60, 57], 0.5),
        'ReviveJa': ([72, 76, 79, 76, 72, 74, 76], 0.25),
        'TurboCola': ([67, 71, 74, 71, 76, 74, 79], 0.125),
        'GatilhoDuplo': ([64, 64, 67, 64, 69, 67, 64], 0.5),
    }
    for name, (seq, duty) in jingles.items():
        buf = np.zeros(int(SR * 2.4))
        for i, nn in enumerate(seq):
            place(buf, square(hz(nn), 0.2, duty) * expdecay(int(SR * 0.2), 7) * 0.6, i * 0.22)
            place(buf, tri(hz(nn - 12), 0.2) * 0.4, i * 0.22)
        write('Jingle' + name, echo(buf, 0.11, 0.3))


# ================================================================== música
def stinger(notes, step, d, duty=0.5, bass=True):
    buf = np.zeros(int(SR * (len(notes) * step + d + 0.8)))
    for i, nn in enumerate(notes):
        place(buf, square(hz(nn), d, duty) * env(int(SR * d), 0.01, d * 0.6, 0.4, d * 0.3) * 0.5, i * step)
        if bass:
            place(buf, tri(hz(nn - 24), d) * env(int(SR * d), 0.01, d, 0.6, d * 0.3) * 0.6, i * step)
    return echo(buf, 0.18, 0.35)


def track(bpm, bars, melody, bassline, drums=True, duty=0.25, lead_vol=0.35, swing=0.0):
    """melody / bassline: listas de (nota ou None, duração em batidas)."""
    beat = 60 / bpm
    total = bars * 4 * beat
    buf = np.zeros(int(SR * total) + SR)

    def lay(seq, voice, vol, octave=0):
        pos = 0.0
        while pos < bars * 4:
            for nn, dur in seq:
                if pos >= bars * 4:
                    break
                if nn is not None:
                    d = dur * beat
                    if voice == 'sq':
                        x = square(hz(nn + octave), d * 0.92, duty) * env(int(SR * d * 0.92), 0.005, d * 0.5, 0.45, d * 0.2)
                    else:
                        x = tri(hz(nn + octave), d * 0.95) * env(int(SR * d * 0.95), 0.005, d * 0.3, 0.8, d * 0.2)
                    place(buf, x * vol, pos * beat)
                pos += dur

    lay(melody, 'sq', lead_vol)
    lay(bassline, 'tri', 0.55, -12)
    if drums:
        for b in range(bars * 4):
            at = b * beat
            if b % 2 == 0:
                place(buf, tri(sweep(120, 45, 0.12), 0.12) * expdecay(int(SR * 0.12), 25) * 0.6, at)
            else:
                place(buf, noise(0.08, 2) * expdecay(int(SR * 0.08), 40) * 0.25, at)
            place(buf, noise(0.03, 1) * expdecay(int(SR * 0.03), 90) * 0.12, at + beat * 0.5)
    return buf[:int(SR * total)]


def music():
    # Trilha ambiente (lá menor, lenta, pulsando): original
    a_mel = [(69, 1), (None, 1), (72, 1), (71, 1), (69, 2), (64, 2),
             (65, 1), (None, 1), (64, 1), (62, 1), (64, 4),
             (69, 1), (None, 1), (72, 1), (76, 1), (75, 2), (71, 2),
             (72, 1), (71, 1), (69, 1), (68, 1), (69, 4)]
    a_bass = [(45, 2), (45, 2), (41, 2), (41, 2), (38, 2), (38, 2), (40, 2), (40, 2)]
    amb = track(84, 16, a_mel, a_bass, drums=True, duty=0.25, lead_vol=0.22)
    pad = np.zeros_like(amb)
    beat = 60 / 84
    for bar in range(16):
        root = [57, 53, 50, 52][bar % 4]
        place(pad, tri(hz(root + 12), beat * 4) * env(int(SR * beat * 4), 0.4, 1.0, 0.5, 0.8) * 0.15, bar * 4 * beat)
    write('MusicAmbient', mix(amb, pad), 0.7)

    # Menu: tema mais melódico (ré menor)
    m_mel = [(74, 1.5), (72, 0.5), (70, 1), (69, 1), (70, 1.5), (69, 0.5), (67, 1), (65, 1),
             (67, 1), (69, 1), (70, 1), (72, 1), (69, 4),
             (74, 1.5), (76, 0.5), (77, 1), (76, 1), (74, 1.5), (72, 0.5), (70, 1), (69, 1),
             (67, 1), (65, 1), (64, 1), (61, 1), (62, 4)]
    m_bass = [(50, 1), (57, 1), (50, 1), (57, 1), (46, 1), (53, 1), (46, 1), (53, 1),
              (43, 1), (50, 1), (45, 1), (52, 1), (45, 1), (49, 1), (45, 1), (49, 1)]
    write('MusicMenu', track(96, 16, m_mel, m_bass, drums=False, duty=0.5, lead_vol=0.3), 0.7)

    # Música secreta (privada): canção de ninar em caixinha de música, original
    s_mel = [(76, 1), (79, 1), (76, 1), (74, 1), (72, 2), (71, 1), (69, 1),
             (71, 1), (72, 1), (74, 1), (76, 1), (74, 3), (None, 1),
             (76, 1), (79, 1), (81, 1), (79, 1), (77, 2), (76, 1), (74, 1),
             (72, 1), (74, 1), (71, 1), (68, 1), (69, 3), (None, 1)]
    s_bass = [(45, 4), (40, 4), (41, 4), (40, 4), (45, 4), (38, 4), (40, 4), (45, 4)]
    beat = 60 / 72
    bars = 16
    buf = np.zeros(int(SR * bars * 4 * beat) + SR)
    pos = 0.0
    while pos < bars * 4:
        for nn, dur in s_mel:
            if pos >= bars * 4:
                break
            if nn is not None:
                d = dur * beat
                bell = tri(hz(nn + 12), d) * expdecay(int(SR * d), 3.5) * 0.5 + square(hz(nn + 24), d, 0.125) * expdecay(int(SR * d), 9) * 0.12
                place(buf, bell, pos * beat)
            pos += dur
    pos = 0.0
    while pos < bars * 4:
        for nn, dur in s_bass:
            if pos >= bars * 4:
                break
            d = dur * beat
            place(buf, tri(hz(nn), d) * env(int(SR * d), 0.05, d, 0.7, d * 0.3) * 0.35, pos * beat)
            pos += dur
    write('MusicSecret', echo(buf[:int(SR * bars * 4 * beat)], 0.3, 0.3, 2), 0.75)

    # Ambiente do sanatório: vento + zumbido grave de prédio velho
    d = 12.0
    n = int(SR * d)
    wind = lowpass(noise(d, 1), 0.02) * (0.6 + 0.4 * np.sin(2 * np.pi * t(d) / d * 3))
    drone = tri(55, d) * 0.15 + tri(55.4, d) * 0.15
    amb = wind * 2.5 + drone
    fade = int(SR * 0.5)
    amb[:fade] *= np.linspace(0, 1, fade)
    amb[-fade:] *= np.linspace(1, 0, fade)
    write('AmbienceAsylum', amb, 0.5)

    # vinhetas de rodada (originais)
    write('RoundFirst', stinger([57, 60, 64, 63, 62], 0.35, 0.8, 0.5))
    write('RoundStart', stinger([57, 57, 64, 63], 0.3, 0.7, 0.5))
    write('RoundEnd', stinger([64, 62, 60, 57], 0.32, 0.8, 0.25))
    write('GameOver', stinger([57, 56, 55, 54, 45], 0.5, 1.2, 0.5))


def main():
    weapons()
    creatures()
    world()
    music()
    names = sorted(f[:-4] for f in os.listdir(OUT) if f.endswith('.wav'))
    # índice lido pelo jogo (no navegador não dá para listar pastas)
    with open(os.path.join(OUT, 'index.txt'), 'w') as f:
        f.write(','.join(names))
    n = len(names)
    print(f'ok: {n} sons em {os.path.normpath(OUT)}')


if __name__ == '__main__':
    main()
