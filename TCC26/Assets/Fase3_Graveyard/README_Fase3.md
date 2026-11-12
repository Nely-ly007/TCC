# POP ADVENTURE — Fase 3: Graveyard Groove

Pacote independente da Fase 2 (não depende de nenhum script seu). Unity 2022.3 LTS ou Unity 6, módulo 2D Tilemap ativo (vem por padrão).
Música: **114 BPM** (1 batida = 0,526 s). Tudo na fase (plataformas, espinhos, mãos de zumbi, pêndulos, inimigos, boss) lê o mesmo relógio: `GGBeatClock` (baseado em `AudioSettings.dspTime`).

---

## 1. Instalar

1. Copie a pasta `Fase3_Graveyard` **inteira** para dentro de `Assets/` do seu projeto.
2. Espere a compilação. O Console não pode ter erro vermelho. (Se tiver, copie a mensagem e me envie.)
3. Menu **Pop Adventure > Fase 3 > Construtor da Fase 3**.

## 2. Construtor (janela)

| Campo | O que colocar |
|---|---|
| Player Prefab | O prefab do seu Player (da pasta Project, não da cena). Opcional. |
| Projétil (nota) Prefab | O prefab do projétil da melodia. Opcional. |
| Música da fase / do boss | Seus AudioClips. Opcional (pode atribuir depois no objeto `Systems`). |
| BPM | 114 |
| Gerar layout base | Marcado = fase completa (~265 unidades). Desmarcado = só chão reto. |

Botões, nesta ordem:

1. **Configurar sprites + Tiles + Paletas + Prefabs** — importa os sprites (PPU/filtro/pivô), cria os Tiles, as duas Tile Palettes, todos os prefabs e o Animator do boss.
2. **Construir cena da Fase 3** — cria `Assets/Scenes/Fase3_GraveyardGroove.unity` e adiciona ao Build Settings. Se a cena já existir, pergunta antes de substituir.
3. **Tudo (1 + 2)** — os dois.
4. **Preparar Player / Projétil** (opcional) — adiciona `GGPlayerHealth` + tag `Player` ao seu Player e `GGProjectileRelay` ao projétil (pede confirmação, modifica os seus prefabs).

> Rodar o passo 1 de novo regenera os prefabs (apaga ajustes feitos neles). **Exceções preservadas:** `Boss_Graveyard` (prefab do boss) e `BossAnimator.controller` com seus clips — nunca são recriados se já existirem.

## 3. Ligar ao seu Player (5 minutos)

**Player:** tag `Player`, `Rigidbody2D` (Dynamic) e um Collider2D. Adicione `GGPlayerHealth`.

- Usando a vida do pacote: deixe **Use Internal Health** marcado (HP 100, sem regeneração, invulnerabilidade de 1 s). Ligue `onHealthChanged (atual, máximo)` ao seu HUD em UI Toolkit.
- Usando a SUA vida: desmarque **Use Internal Health** e ligue
  - `onDamaged (int)` → seu método de dano,
  - `onHealed (int)` → seu método de cura (notas +15, coração +30),
  - quando seu HP chegar a 0, chame `GGPlayerHealth.NotifyDied()`,
  - `GGLevelManager.onPlayerRespawned` → seu método de restaurar vida.

**Projétil da melodia:** adicione `GGProjectileRelay` (dano 10). O prefab precisa ter Collider2D (trigger serve). Ele repassa o dano para tudo que implementa `IGGDamageable` (inimigos, boss, orbs do boss, alvos de ressonância).

**HUD / GameManager** (todos UnityEvents, ligue no Inspector):
- `GGLevelManager`: `onVinylsChanged (total)`, `onVinylCollected (qtd)`, `onFragmentCollected`, `onPlayerRespawned`, `onLevelComplete`. Campo `Next Scene Name` opcional.
- `GGBoss`: `onFightStarted`, `onHealthNormalized (0..1)`, `onPhaseChanged (1..3)`, `onHurt`, `onDefeated`. Para usar só o seu HUD, desative o filho `HealthBar` do boss.

## 4. A cena (hierarquia)

```
Main Camera            GGCameraFollow (segue tag Player; sem Cinemachine)
Systems                GGBeatClock (114 BPM, músicas), GGLevelManager
Grid
  Background (-2)      Tilemap decorativo
  Ground (0)           Tilemap + TilemapCollider2D (Composite, Merge) — colisão
  Platforms (1)        Tilemap one-way (PlatformEffector2D) — plataformas finas
  Foreground (5)       Tilemap sem colisão, na frente do Player
  Decor / Hazards / RhythmPlatforms / Pickups / Interactables / Enemies   (grupos de objetos)
BossFight              Gate_Iron + BossArena + Boss_Graveyard
Backdrop               fundo com paralaxe + Fog_Back / Fog_Front
KillZone               abismo: -15 HP e volta ao checkpoint
PlayerSpawn            ponto inicial (0, 0.3)
```

Layout gerado (coordenadas do mundo, chão em y = 0):

| Trecho | X | O que tem |
|---|---|---|
| A Entrada | 0–45 | corvos, espinhos fixos, espinhos rítmicos, plataforma de espinhos móvel |
| B Ponte rítmica | 45–77 | plataformas que piscam no beat, que sobem, e uma pendurada em trilho sobre o abismo |
| C Cripta | 77–107 | checkpoint, mãos de zumbi no ritmo, lobo, rampas |
| D Escalada | 107–137 | pêndulos de espinhos, plataformas que sobem alternadas (1 batida de diferença) |
| E Platô | 137–177 | serra em trilho, lobo, fantasmas, corvos, área secreta no alto (vinis + coração) |
| F Descida | 177–213 | degraus com armadilhas, **puzzle de ressonância**: acerte as 3 lanternas com a nota (ficam acesas 10 batidas) para abrir o portão; checkpoint; notas de cura |
| G Arena | 213–249 | portões fecham, câmera trava, música troca no compasso, boss |

## 5. Pintar com a Tile Palette (para editar a fase)

`Window > 2D > Tile Palette`. Duas paletas em `Generated/Palettes`:

- **Graveyard_Tiles** — pincel de Tile normal. Alvo (*Active Tilemap*):
  - `Ground`: `Ground_Top_L/M/R/Single`, `Wall_*`, `Stone_Block`, `Ramp_*` (têm colisão).
  - `Platforms`: `Plat_Thin_L/Single/R` (one-way: dá para pular por baixo). Use L + Single + R.
  - `Background` / `Foreground`: qualquer tile, sem colisão.
- **Graveyard_Objects** — escolha o **GameObject Brush**. Alvo: `Decor`, `Hazards`, `RhythmPlatforms`, `Pickups`, `Interactables` ou `Enemies`. **Clique na célula da BASE do objeto** (onde ele "pisa"), não no topo de objetos altos.

Convenção dos prefabs: a raiz fica no centro de uma célula e a base do sprite na base dela, então pintar na primeira célula acima do chão apoia o objeto no chão. Exceções: **Lobo e Boss** têm a raiz nos pés (o lobo se ajusta ao chão sozinho); **plataformas** têm a superfície de pisar em `raiz + 0,5`.

Ajustes por objeto (Inspector, depois de pintar):
- `SpikeTrap_Rhythm`: `Period Beats`, `Active Beats`, `Offset Beats` (defasagem em batidas).
- `Platform_Glowing_Blink` / `_Rise`: `Period Beats`, `On Beats`, `Offset Beats`. Em cadeia, use offsets 0, 1, 2, 3.
- `Platform_Hanging_Rail`, `SpikePlatform_Moving`, `Gear_Rail`: `GGRailMover` → `Travel` (x, y), `Beats Per Cycle`.
- `SpikedBall_Swing_*`: `GGPendulum` → `Max Angle`, `Period Beats`. A raiz é o pivô.
- `ZombieHand`: `Period Beats`, `Offset Beats`, `Proximity Only`.
- Inimigos: `Enemy_Crow` (`Hover Mode` = paira), `Enemy_Wolf` (`Patrol Range`), `Enemy_Ghost`. Vida/dano: corvo 20/15, lobo 40/30, fantasma 10/15.
- Puzzle: `Resonance_Lantern`/`Resonance_Tombstone` + `Gate_Iron` (marque `Start Closed`) + `Door_Resonance` (arraste os alvos em `Targets` e o portão em `Gate`).

## 6. O boss (você coloca o sprite e anima no Animator)

Prefab: `Generated/Prefabs/Boss/Boss_Graveyard` (já está na cena, em `BossFight`).

```
Boss_Graveyard   GGBoss, Rigidbody2D (Kinematic), BoxCollider2D (hurtbox + dano de contato)   ← raiz = PÉS
  Visual         Animator (BossAnimator)  ← o código vira o scale.x aqui. NÃO anime o scale.x do Visual.
    Body         SpriteRenderer           ← coloque o SEU sprite aqui e anime este objeto
  Muzzle         de onde saem os orbs
  HealthBar      barra de vida em espaço de mundo
```

Passo a passo:
1. Selecione `Body` e troque o **Sprite** do SpriteRenderer pelo seu. Ajuste o **Local Position Y** do `Body` para os pés ficarem na altura do chão (y = 0 da raiz).
2. Ajuste o `BoxCollider2D` da raiz ao tamanho do corpo. Se o sprite original olha para a esquerda, desmarque **Sprite Faces Right** no `GGBoss`.
3. Selecione `Visual`, abra `Window > Animation > Animation`, escolha o clip no menu (`Generated/Animation/BossClips/Boss_*.anim` — estão vazios) e grave os keyframes (Sprite do `Body` ou ossos/partes).
4. O Controller `Generated/Animation/BossAnimator.controller` já tem os parâmetros e as transições. Os estados de ataque voltam para `Idle` ao terminar o clip.

Parâmetros do Animator (se algum não existir, o código ignora em silêncio; os nomes podem ser trocados no `GGBoss`):

| Parâmetro | Quando dispara | Duração a animar (114 BPM) |
|---|---|---|
| `IsMoving` (Bool) | andando / deslizando | loop (`Walk`) |
| `Intro` (Trigger) | início da luta | 8 batidas ≈ 4,2 s |
| `AttackDash` (Trigger) | aviso do deslize (moonwalk) | 2 batidas ≈ 1,05 s (fase 3: 1) e depois desliza com `IsMoving` |
| `AttackHands` (Trigger) | invoca a onda de mãos de zumbi | 1 batida ≈ 0,5 s |
| `AttackSpin` (Trigger) | giro + ondas de choque no chão | 1 batida ≈ 0,5 s |
| `AttackOrbs` (Trigger) | orbs mirados (fase 2+) | 1 batida ≈ 0,5 s |
| `AttackSummon` (Trigger) | invoca 2 fantasmas (fase 2+) | 2 batidas ≈ 1 s |
| `Hurt` (Trigger) | levou dano (no máx. 1 a cada 0,8 s) | curto |
| `PhaseChange` (Trigger) | HP 66 % e 33 % | 4 batidas ≈ 2,1 s |
| `Die` (Trigger) | derrotado | sem retorno (o fragmento aparece 1,8 s depois) |

Luta: HP 250, 3 fases (66 % / 33 %). Ataques: Deslize (30 de dano), Mãos de zumbi (15, sempre sobra um vão), Giro com ondas no chão (pule), Orbs (fase 2+, a nota destrói), Fantasmas (fase 2+). Boss largando até 2 notas de cura por luta. Se o Player morrer na luta, tudo reinicia no checkpoint.

## 7. Se algo não sair como esperado

- **Sprites pretos (URP 2D):** adicione uma Light 2D Global (`GameObject > Light > Global Light 2D`) ou troque o material dos SpriteRenderers para Sprite-Unlit.
- **Nada causa dano:** o Player precisa de `GGPlayerHealth` e tag `Player`.
- **A nota não acerta inimigos:** o projétil precisa de `GGProjectileRelay` e um Collider2D.
- **Paleta não aparece no Tile Palette:** crie uma pela janela (`Create New Palette`) e arraste os assets de `Generated/Tiles`; os prefabs estão em `Generated/Prefabs`.
- **Câmera:** `GGCameraFollow` é simples; se preferir o Cinemachine da fase 2, remova o componente (o resto funciona; só `Shake`/`LockBounds` deixam de ter efeito).
- Para mudar o BPM depois: `Systems > GGBeatClock > Bpm`.

## 8. Pastas

```
Fase3_Graveyard/
  Art/         sprites originais já limpos (tiles, props, hazards, inimigos, coletáveis, fundo)
  Scripts/Runtime/   código da fase (prefixo GG)
  Scripts/Editor/    construtor (menu Pop Adventure > Fase 3)
  Generated/   criado pelo construtor: Tiles, Palettes, Prefabs, Animation
```
