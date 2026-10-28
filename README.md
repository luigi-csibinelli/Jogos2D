# Mansão Varkoff (nome provisório)

Survival horror 2D em 8-bit com câmera vista de cima (como GTA antigo / SAS 4), inspirado nos Resident Evil clássicos. O personagem gira para onde o mouse aponta, e a câmera fica próxima e com vinheta para o clima ficar sombrio. Projeto 2D da disciplina de Jogos Digitais (Insper, 2026-1).

Você é um agente do DHS enviado à mansão do excêntrico bilionário Aldous Varkoff, ligado ao grupo de bioterrorismo Ouroboros, para descobrir o que aconteceu com o Agente Okafor, desaparecido lá dentro. O foco é administrar recursos (munição, faca, cura, espaço no inventário) e resolver puzzles para destrancar a mansão.

## Como jogar

| Ação | Tecla |
| --- | --- |
| Mover | WASD ou setas |
| Mirar | Mouse |
| Atirar | Clique esquerdo |
| Faca | Clique direito |
| Recarregar | R |
| Interagir / pegar / ler / vasculhar | E |
| Inventário | Tab (ou I) |
| Pausa | Esc |

- **A vida não aparece no HUD.** Ferido, o agente passa a mancar e o corpo balança; muito ferido, manca mais e vai pingando sangue.
- O dano de cada tiro é `17 × aleatório(0,75 a 1,25)`. Com 40% da vida o zumbi **cai**. Enquanto ele está caído, a **faca o finaliza** gastando só 1 uso. Se você não o finalizar, ele se levanta e volta a perseguir.
- **A munição não fica no chão: está escondida no cenário.** Chegue perto dos móveis e use **[E] Vasculhar**; alguns esconderijos estão vazios. O bilhete do mordomo, na mesinha da entrada, avisa sobre isso.
- **Chaves não somem sozinhas.** Cabe a você perceber que uma chave não serve mais e largá-la para liberar espaço.
- O **inventário tem 6 espaços**, e documentos também ocupam espaço. Dá para **largar** qualquer item, e ele fica no chão onde você o deixou.
- **Abrir o inventário ou ler um documento não pausa o jogo.** Só o Esc pausa.
- Quando um zumbi se levanta, toca uma trilha tensa. Ela para quando ele cai ou morre e recomeça se ele levantar de novo.

## A primeira área (térreo + 1º andar)

- **Saguão (térreo):**
  - Você entra pela porta leste, que emperra atrás de você.
  - O interruptor ao lado dela acende as luzes, e o bilhete do mordomo está na mesinha logo abaixo.
  - O corpo do Agente Okafor está no canto nordeste, com a pistola dele ao lado e um documento no corpo.
  - Um zumbi caído no pé da escada se levanta quando você pega a pistola.
  - O retrato do Varkoff fica na parede sul. O relógio esconde 3 balas.
  - As portas norte e sul estão trancadas com fechaduras de brasão.
- **Galeria (1º andar):**
  - É um U em volta do vão do saguão, alcançado pela escada a oeste.
  - O cofre fica no fim da ala sul e guarda a Chave do Brasão.
  - Uma das estantes da ala norte esconde 4 balas.
  - As portas da galeria têm fechaduras de lua, sol e estrela, reservadas para os próximos puzzles.
- **Solução:** o documento diz que a combinação é "o ano em que ele nasceu de novo", e o retrato diz "Renascido em 1987". O código é **1987**. A chave abre as duas portas do térreo e fica no inventário até você largá-la.
- **Futuro:** uma porta sob a escada, que abre com a chave mestre, vai levar ao laboratório embaixo da mansão (caminho sem volta).

## Estrutura do projeto

```
Assets/
  Scripts/            código do jogo (Jogador, Arma, Zumbi, Inventario, Musica, Energia...)
    Interacao/        tudo que usa [E]: ItemNoChao, Corpo, Examinavel, Esconderijo, Cofre, Porta, Interruptor, Escada
    UI/               HUD, InventarioUI, LeituraUI, TecladoUI, MenuPrincipal, FimDemo
    Debug/            TesteAutomatico (roteiro de teste da sala)
  Editor/MontaCena.cs monta cenas, prefabs e tiles do zero; também faz o build WebGL
  Editor/TesteSala.cs teste do Test Runner que roda o roteiro da sala no editor
  Scenes/             MainMenu, Sala01_Saguao, FimDemo
  Prefabs/            Jogador, Zumbi, ItemNoChao
  Sprites/ Audio/ Fontes/ Tiles/ Dados/
Ferramentas/          scripts Python que geram sprites, sons e a fonte pixel
```

### Gerar os assets e montar as cenas

```
python Ferramentas/gera_sprites.py
python Ferramentas/gera_sons.py
python Ferramentas/gera_fonte.py
```

No Unity (6000.3.25f1), use o menu **Mansao > Montar tudo**. **Atenção:** isso recria as cenas do zero, então só rode de novo se quiser descartar as edições feitas à mão nelas. **Mansao > Build WebGL** gera o build em `Build/WebGL`.

Se você editar as paredes à mão no Tile Palette, confira se o `CompositeCollider2D` do tilemap "Paredes" tem formas. Quando montada por script, a geometria dele precisa ser gerada explicitamente; caso contrário, as paredes ficam sem colisão.

### Energia (para expandir)

`RedeEnergia` guarda quais seções da mansão têm energia. Cada `SecaoEnergia` controla as luzes de uma seção, e um `Interruptor` só funciona se a seção dele tiver energia. O próximo passo é um `PainelEnergia` que liga seções novas e libera portas elétricas e similares.

### Teste automático

Um roteiro (`Scripts/Debug/TesteAutomatico.cs`) confere cerca de 50 regras da sala: zumbi, faca, música, esconderijos, documentos, colisão das paredes (térreo e galeria), escada, cofre, portas, inventário e morte. Há duas formas de rodar:

- **No editor:** Window > General > Test Runner > EditMode > `RoteiroDaSala01`. O resultado aparece no Console (`TESTE ok` / `TESTE FALHA`).
- **No build WebGL:** abra a página com `?teste=1` no fim da URL.

## Créditos

- Sprites, efeitos sonoros, músicas e a fonte pixel (PixelDHS) foram gerados por código para este projeto (pasta `Ferramentas/`).
- Engine: Unity 6 (URP 2D).
