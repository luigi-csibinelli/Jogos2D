# GDD — Sanatório 8-bit

> Texto-base para o Milanote. Cada seção vira um quadro. Mudanças de rumo
> ficam registradas no fim, em "Diário de decisões".

## 1. Visão geral
- **Gênero:** shooter de sobrevivência em rodadas infinitas, visto de cima (twin-stick).
- **Plataforma:** navegador (WebGL, itch.io). Teclado e mouse ou controle.
- **Público:** fãs de *Nazi Zombies* clássico e de arcade 8-bit.
- **Pitch:** "Verrückt em 8 bits": o mapa de zumbis mais claustrofóbico de
  World at War, visto de cima como um GTA 1, com história própria e
  ambientado com a FEB.
- **Pilares:** (1) tensão de corredor estreito; (2) economia de pontos com
  decisões; (3) clareza 8-bit: tudo o que importa se lê no mundo, não só no HUD.

## 2. Narrativa e mundo
- **Quando e onde:** abril de 1945, Sanatório de Valgrigia (fictício), norte da Itália.
- **Protagonista:** Cabo Ramos, da FEB, perdido da patrulha.
- **Antagonista ausente:** Sociedade Lázaro, que testou o *Soro L* nos pacientes.
- **Entrega da história:** 8 fichas de paciente (diário da enfermeira, ordem de
  serviço, relatório médico, carta, inventário, necrotério, última anotação).
  As fichas explicam as próprias mecânicas: as tábuas nas janelas, as grades
  elétricas, os refrigerantes, a caixa que se muda. O epílogo destrava com as 8.
- **Tom:** horror pulp. Sem símbolos nazistas: os zumbis são pacientes,
  enfermeiros e soldados de farda cinza lisa.

## 3. Mecânicas
- **Movimento e mira:** 360°, mira no mouse ou no analógico direito (mira assistida opcional).
- **Combate:** 26 armas e equipamentos de WaW (`WeaponCatalog.cs`). Há tiro com
  penetração, escopetas de vários chumbos, projéteis explosivos, lança-chamas
  em cone e faca.
- **Crítico:** todo disparo de arma de fogo tem 20% de chance de causar dano
  dobrado. A rolagem é por bala ou chumbo (vale para os zumbis que ela
  atravessar) e, nos projéteis, só no acerto direto. Retorno: estouro amarelo e "tim".
- **Atravessar:** a bala segue com 50% do dano para o próximo zumbi.
- **Lança-chamas:** 50 por tique, munição infinita, superaquece (5 s de fogo
  contínuo, 5 s travado esfriando; rajadas curtas não superaquecem), como o M2 de WaW.
- **Caixa:** sorteia só armas que não estão à venda na parede.
- **Rodada dos cães:** primeira na 6 ou 7, depois a cada 7 ou 8 rodadas. Os
  cães nascem com um raio perto do jogador e têm 45% da vida do zumbi da
  rodada. O último sempre deixa Munição Máxima. O céu fica vermelho.
- **Panzer** (personagem original; regras do Panzersoldat): rodada 20 e depois
  a cada 4 rodadas. Vida 9000 + 700 por rodada, armadura que corta pela metade
  o dano de balas e fogo, lança-chamas curto, garra que puxa (solta com 1500 de
  dano), soco de 130. Sempre deixa um power-up do ciclo.
- **Soco em Lata** (Pack-a-Punch), no Necrotério: 5000 pontos e precisa de
  energia. 3,5 s melhorando e 12 s para pegar. Dano x2, pente x1,5, reserva x2,
  +1 de atravessar. Munição na parede: 4500.
- **Mira:** segue o mouse; fica vermelha ao acertar e apagada sem poder atirar.
- **Troca de armas:** teclas 1 (primária) e 2 (secundária); no controle, Y alterna.
- **Pontos:** acerto 10, abate 60, faca 130, tábua 10 (com limite de 400 por
  rodada), Bomba 400.
- **Zonas e portas:** 10 zonas, 9 portas pagas (750 ou 1000) e a porta
  elétrica, que abre com a energia.
- **Energia:** o disjuntor na Sala do Gerador liga refrigerantes, armadilhas, luzes e a porta elétrica.
- **Refrigerantes:** Ferro-Nog 2500, Revive-Já 500 (solo, até 3), Turbo-Cola 3000, Gatilho Duplo 2000 (+100% de cadência; no lança-chamas, esfria 2x mais rápido). Cair perde todos.
- **Caixa misteriosa:** 950 pontos. Arma aleatória por pesos (a Arma de Raios
  é rara). Depois de 3 usos pode sair o ursinho: devolve os pontos e a caixa se
  muda pelo **ciclo da caixa**, que passa por todos os outros pontos antes de
  repetir e pula zonas trancadas. Ela começa sempre no Gerador.
- **Power-ups:** Morte Instantânea, Pontos em Dobro, Bomba e Munição Máxima. A
  chance é de 2,5% por abate, no máximo 4 por rodada. O tipo segue um **ciclo**:
  os 4 saem antes de repetir e nunca o mesmo duas vezes seguidas.
- **Armadilhas elétricas:** 4 (duas por varanda). Custam 1000, ficam ligadas
  25 s, recarregam em 50 s e machucam o jogador também.
- **Barricadas:** 20 janelas com 6 tábuas cada. O zumbi arranca uma tábua por
  vez e consegue bater em quem está colado na janela.
- **Zumbis:** nascem no pátio e entram pelas janelas. Perseguem por campo de
  fluxo e o ataque é telegrafado. Uma explosão que não mata pode virar o zumbi
  em rastejante.

## 4. Progressão e balanceamento
- Vida dos zumbis: 150 + 100 por rodada até a 9, depois x1,1 por rodada.
- Quantidade (solo): 6, 8, 13, 18, 24, 27, 28, 28, 29 e depois cresce em curva. No máximo 24 vivos ao mesmo tempo.
- Corredores: 0% na rodada 1 e +16% por rodada. O Insano soma +35%, +15% de velocidade e +25% de zumbis.
- Jogador: 100 de vida (250 com Ferro-Nog) e golpe de zumbi de 50. Regenera depois de 2,6 s.
- Tudo está em `Balance.cs`. Os testes de EditMode travam as regras de WaW.

## 5. Level design
A planta ASCII fica em `VerrucktMap.cs`. Os andares de Verrückt foram
"achatados" num plano só (as escadas viram tiles). O lado oeste concentra as
armas alemãs e o leste as americanas, como no original.

| Zona | Abre com | Conteúdo |
|---|---|---|
| Enfermaria Oeste (spawn) | início | Kar98k, Gewehr 43, granadas, Ferro-Nog, caixa |
| Varanda Oeste | 1000 | MP40, Cano Duplo, Betty, Gatilho Duplo, 2 armadilhas, caixa |
| Andar Superior Oeste | 750 | STG-44, Trench Gun, caixa, privadas (segredo) |
| Sala do Gerador | 1000 (por qualquer lado) | caixa inicial, disjuntor |
| Enfermaria Leste (spawn) | início | Springfield, M1 Garand, Revive-Já |
| Necrotério | 750 | BAR, Soco em Lata (melhoria de armas) |
| Corredor Leste | 750 | Thompson, Cano Duplo, granadas, caixa |
| Varanda Leste | 1000 | Trench Gun, BAR, Betty, 2 armadilhas |
| Andar Superior Leste | 750 | Cano Duplo Serrada, granadas, Turbo-Cola |
| Cozinha | 1000 | passagem para o Gerador (1000) |

**Ensino:** dicas aparecem na primeira vez que cada mecânica surge (andar,
reconstruir, giz de arma, portas, disjuntor, caixa, sem munição).

## 6. Arte
- 16x16 por tile, paleta inspirada no NES, câmera Pixel Perfect de 384x216.
- Tudo gerado por código (`Tools/gerar_sprites.py`): tiles por zona,
  personagens de cima, ícones de 26 armas (coloridos e em giz), máquinas
  acesas e apagadas e fonte 5x7 com acentos.
- Iluminação 2D: escuro antes da energia, com lanterna em cone e luar; depois,
  lâmpadas quentes que piscam.

## 7. Som
- Síntese de 8 bits (`Tools/gerar_audio.py`): quadrada, triângulo e ruído.
- Tiros por classe, gemidos, tábuas, jingles da caixa e dos refrigerantes, vinhetas de rodada.
- Músicas originais: ambiente (lá menor), menu (ré menor) e a canção de ninar secreta.
- Som posicional falso (volume e pan pela distância), igual em todo navegador.

## 8. Interface
- **HUD:** rodada em giz vermelho, pontos com ganhos subindo, munição, granadas
  e Betties, refrigerantes, power-ups com tempo e aviso de interação com preço
  (fica vermelho se não dá para pagar).
- **Menus:** principal, como jogar, opções (volume, tremor, linhas de TV, mira
  assistida, tela cheia), recordes, arquivos e créditos. Também pausa e fim de
  jogo com estatísticas.

## 9. Robustez
- Validação da planta na montagem e nos testes.
- Anti-travamento: um zumbi sem progresso por 15 s volta ao pátio.
- Pausa automática ao perder o foco. Saves com try/catch. Sempre existe uma forma de ganhar pontos.

## 10. Diário de decisões
- **04/11:** escopo "mapa completo fiel". Jogador solo com spawn sorteado e
  Revive-Já no estilo BO (500, 3 usos). Homenagem com lore própria em vez de
  remake direto, por causa da rubrica de originalidade e da propriedade
  intelectual.
- **10/11 (2):** lança-chamas trava 5 s e as chamas ficaram maiores; mira;
  rodada dos cães; Soco em Lata no Necrotério; Panzer a partir da rodada 20.
- **10/11:** sprites do soldado por tipo de arma e silhueta da arma no HUD;
  atravessar mantém 50% do dano; Gatilho Duplo +100%; caixa sem armas de
  parede; lança-chamas com 50 de dano, munição infinita e superaquecimento.
- **05/11:** depois do primeiro teste, troca de arma nas teclas 1 e 2 e
  crítico de 20% (dano x2) nas armas de fogo.
- **04/11:** a porta Cozinha→Gerador custa 1000, igual à do lado oeste. A caixa
  e os power-ups usam ciclos (ShuffleBag) em vez de sorteio puro.
