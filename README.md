# Sanatório 8-bit

Shooter de sobrevivência em rodadas, visto de cima (estilo GTA 1 / Dead Ops
Arcade), em pixel art 8-bit. É uma **homenagem a Verrückt**, o segundo mapa
de zumbis de *Call of Duty: World at War*: a planta, as regras, os preços e as
armas reais da Segunda Guerra seguem o original. Já a história, os personagens,
as marcas dos refrigerantes, a arte, os sons e a música são todos **nossos**.

Projeto 2D, disciplina **Jogos Digitais**, Insper 2026.1.

- **Jogue no navegador:** [_JOGAR_](https://luigi-csibinelli.itch.io/sanatorio-8-bit)
- **GDD:** [_MILANOTE_](https://app.milanote.com/1Xr6Ok1AwMfQ4P?p=EACCOJcWKfw). O texto-base está em [GDD.md](GDD.md).

## Equipe:
- Kauã Figueira
- Luigi Sibinelli
- Mathias Zanotto
- Matheus Constante
---

## História

Abril de 1945, norte da Itália. O **Cabo Ramos**, da Força Expedicionária
Brasileira, se perde da patrulha numa tempestade e se abriga no velho
**Sanatório de Valgrigia**. Ali a **Sociedade Lázaro** testava o *Soro L* nos
pacientes, e eles continuam lá, mortos e de pé. Oito **fichas de paciente**
espalhadas pelo prédio contam o que aconteceu. Quem encontra todas desbloqueia
o epílogo no menu.

## Como jogar

| Ação | Teclado e mouse | Controle |
|---|---|---|
| Mover | WASD / setas | Analógico esquerdo |
| Mirar | Mouse | Analógico direito (mira assistida opcional) |
| Atirar | Clique esquerdo | RT |
| Faca | V / clique direito | RB |
| Recarregar | R | X |
| Arma primária / secundária | 1 / 2 | Y (alterna) |
| Granada | G | LB |
| Bouncing Betty | B | LT |
| Usar / comprar / segurar para reconstruir | F / E | A |
| Pausa | Esc / P | Start |

Regras (as de Verrückt):

- Armas de fogo têm **20% de chance de crítico**, que dobra o dano daquele
  tiro (cada chumbo da escopeta rola separado; explosões não). O crítico solta
  um estouro amarelo e um "tim" agudo.
- Bala que atravessa um zumbi segue com **50%** do dano para o próximo.
- A **caixa misteriosa** só dá armas que **não** são vendidas na parede.
- O **lança-chamas** (50 de dano por tique) não gasta munição, mas
  **superaquece** depois de 5 s de fogo contínuo e trava por 5 s esfriando.
  Rajadas curtas nunca superaquecem.
- O soldado segura um modelo diferente para cada tipo de arma, e a silhueta da
  arma atual aparece ao lado da munição (separa Kar98k e Springfield).
- **Rodada dos cães:** a primeira cai na rodada 6 ou 7 e as próximas a cada 7
  ou 8 rodadas. Os cães nascem em raios perto de você, são mais rápidos que o
  jogador e o **último sempre deixa uma Munição Máxima**. O céu fica vermelho.
- **Panzer:** a partir da rodada 20 (e a cada 4 rodadas) chega de
  mochila-foguete um soldado blindado. Balas e fogo causam metade do dano,
  explosivos causam o dano cheio. Ele usa lança-chamas de perto, uma garra que
  te puxa de longe (cause 1500 de dano nele para se soltar) e um soco que
  derruba quem não tem Ferro-Nog. Ao cair, sempre deixa um power-up.
- **Soco em Lata** (no Necrotério, com energia, 5000): melhora a arma na mão.
  Dano x2, pente x1,5, reserva x2, atravessa um zumbi a mais e o traçante fica
  roxo. Pegue a arma antes que ela afunde de volta na máquina! Munição de arma
  melhorada na parede custa 4500.
- A **mira** segue o mouse, fica vermelha quando acerta e apagada quando não dá
  para atirar (recarregando ou superaquecido).
- Cada acerto vale **10 pontos**. Um abate vale **60**, ou **130** na faca.
  Pregar uma tábua na janela vale **10**.
- Com os pontos você abre **portas** (750 ou 1000), compra **armas desenhadas a
  giz na parede** (a munição custa metade do preço) e roda a **caixa
  misteriosa** (950).
- O **disjuntor** fica na Sala do Gerador. Ele liga os **4 refrigerantes**, as
  **4 armadilhas elétricas** das varandas, as lâmpadas e a **porta elétrica**
  entre as duas alas.
- Você começa na Ala Oeste ou na Ala Leste, sorteada a cada partida.
- **2 golpes derrubam** (5 com Ferro-Nog). Cair sem **Revive-Já** encerra a
  partida. Com ele, você levanta sozinho (no máximo 3 compras) e perde os
  refrigerantes.
- Segredo: dê descarga **3 vezes na privada da esquerda** no Andar Superior Oeste.

| Original | No jogo | Preço | Efeito |
|---|---|---|---|
| Juggernog | Ferro-Nog | 2500 | aguenta 5 golpes |
| Quick Revive | Revive-Já | 500 | levanta sozinho (solo) |
| Speed Cola | Turbo-Cola | 3000 | recarga e reconstrução 2x mais rápidas |
| Double Tap | Gatilho Duplo | 2000 | +100% de cadência (no lança-chamas, esfria 2x mais rápido) |

## Rubrica: onde cada item está

| # | Rubrica | Onde |
|---|---|---|
| 1 | Conceito e narrativa | Lore original (FEB, Sociedade Lázaro, Soro L) em `Scripts/Core/Lore.cs`. São 8 fichas colecionáveis, guardadas entre partidas, mais o epílogo. |
| 2 | Mecânicas | Rodadas, economia de pontos, portas e zonas, 26 armas (`WeaponCatalog.cs`), caixa com ciclo, refrigerantes, armadilhas, barricadas, granadas, molotov, Betty, rastejantes e 4 power-ups. |
| 3 | Feedback | O giz das armas acende quando você chega perto. Preços aparecem nas portas. As máquinas acendem quando a energia liga. Os zumbis piscam ao levar dano e levantam os braços antes de atacar. Tábuas caem. Borda vermelha ao levar dano, câmera tremendo e sons posicionais. |
| 4 | UI e menus | Menu (jogar, como jogar, opções, recordes, arquivos, créditos), pausa e fim de jogo com estatísticas. HUD com rodada em giz, pontos subindo, munição, refrigerantes e power-ups com tempo. Fonte pixel própria com acentos. |
| 5 | Engajamento | Trilha e ambiente sonoro, luz escura antes da energia, lanterna em cone, dicas no momento certo e lore aos poucos. |
| 6 | Level design | A planta de Verrückt: 10 zonas, 2 spawns, 9 portas pagas e a porta elétrica. Dicas de primeira vez (`Hints.cs`) ensinam cada mecânica quando ela aparece. |
| 7 | Som e música | 82 sons originais sintetizados (`Tools/gerar_audio.py`): tiros por classe, gemidos, jingles, vinhetas, trilha ambiente, tema do menu e música secreta. |
| 8 | Efeitos visuais | Luzes 2D (URP): lanterna, lâmpadas que piscam, luar nas janelas, clarões de tiro e explosão. Partículas de sangue, faíscas, fogo e poeira. Manchas no chão, traçantes, tremor de tela e linhas de TV opcionais. |
| 9 | Re-jogabilidade | Spawn sorteado, caixa com ciclo aleatório, power-ups em ciclo, modo Insano, recordes por modo e fichas para encontrar. |
| 10 | Balanceamento | Todos os números estão em `Scripts/Core/Balance.cs`: vida dos zumbis igual à de WaW (150 + 100 por rodada, depois x1,1) e curva de quantidade solo. Os testes travam as regras. |
| 11 | Estados e erros | Planta validada na montagem (zonas alcançáveis, nenhum piso isolado, portas ligando 2 zonas). Zumbi travado volta ao pátio, então a rodada nunca emperra. Pontos sempre disponíveis (faca e tábuas). O jogo pausa ao trocar de aba. Saves protegidos e reinício com um botão. |
| 12 | GDD | [Board no Milanote](https://app.milanote.com/1Xr6Ok1AwMfQ4P?p=EACCOJcWKfw) (Design / Dinâmica / Experiência) e texto-base em [GDD.md](GDD.md). |

## Estrutura do projeto

```
Assets/
  Scripts/Core      Game (estado), RoundManager, Balance, ShuffleBag, PowerUps, Lore, SaveData, Audio
  Scripts/Map       VerrucktMap (planta ASCII), MapData (análise e validação), NavGrid (campo de fluxo), WorldBuilder
  Scripts/Player    Player, Inventory, PlayerInput (Input System em código)
  Scripts/Weapons   WeaponCatalog, Gunner (tiro), Explosives (projéteis, granadas, Betty)
  Scripts/Zombies   Zombie (janela, perseguição, ataque, anti-travamento)
  Scripts/Interact  WallBuy, Door, PowerSwitch, PerkMachine, MysteryBox, TrapLever, Barricade, Toilet, LoreNote
  Scripts/UI        PixelFont/PixelText, UIKit, Hud, MainMenu, OptionsPanel
  Scripts/FX        Fx (partículas, traçantes, manchas), LightKit, CameraRig
  Editor            GameSetup (monta o projeto), BuildWebGL
  Tests/Editor      testes de mapa, ciclos, campo de fluxo e balanceamento
Tools/              gerar_mapa.py, gerar_sprites.py, gerar_audio.py
```

O mundo é montado em código a partir da planta ASCII de
`Assets/Scripts/Map/VerrucktMap.cs`. Editar a planta muda o jogo, e o validador
aponta qualquer erro antes de rodar.

## Como compilar

Requer Unity **6000.3.26f1** com o módulo WebGL e Python 3 com `numpy` e `Pillow`.

```bash
python3 Tools/gerar_sprites.py && python3 Tools/gerar_audio.py
```

Depois, no Unity: **Sanatório > Montar projeto** e **Sanatório > Gerar build
WebGL**. A build sai em `Build/WebGL`. Pela linha de comando:

```bash
Unity -batchmode -projectPath . -executeMethod Sanatorio.EditorTools.GameSetup.BuildFromCommandLine -quit
Unity -batchmode -projectPath . -runTests -testPlatform EditMode
Unity -batchmode -projectPath . -executeMethod Sanatorio.EditorTools.BuildWebGL.BuildFromCommandLine -quit
```

### Testar rápido no navegador

Com a build aberta, o console do navegador aceita comandos de teste:
`sanatorio('points 5000')`, `sanatorio('power')`, `sanatorio('round 10')`,
`sanatorio('zone 3')` (vai para uma zona pelo número), `sanatorio('god')`,
`sanatorio('give raygun')`, `sanatorio('kill')`, `sanatorio('dogs')` (próxima
rodada é de cães), `sanatorio('panzer')` e `sanatorio('pack')` (melhora a arma na mão). O jogador comum nunca vê esses comandos.

## Créditos e referências

- **Inspiração:** *Verrückt*, mapa de *Call of Duty: World at War*
  (Treyarch / Activision, 2009). Usamos como referência a planta, os preços, a
  lista de armas e as regras, a partir da
  [Call of Duty Wiki](https://callofduty.fandom.com/wiki/Verr%C3%BCckt). Não há
  nenhum asset, nome de produto, personagem ou música do jogo original.
- **Arte, sons, música, história e código:** feitos pelo grupo para este
  projeto. Sprites gerados por `Tools/gerar_sprites.py`, sons e músicas por
  `Tools/gerar_audio.py`.
- **Motor:** Unity 6, URP 2D (luzes 2D), Input System.
