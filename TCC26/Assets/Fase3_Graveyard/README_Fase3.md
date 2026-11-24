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

1. **Configurar sprites + Tiles + Paletas + Prefabs** — importa os sprites (PPU/filtro/pivô), cria os Tiles, as duas Tile Palettes e todos os prefabs (o boss já sai com o rig animado por código).
2. **Construir cena da Fase 3** — cria `Assets/Scenes/Fase3_GraveyardGroove.unity` e adiciona ao Build Settings. Se a cena já existir, pergunta antes de substituir.
3. **Tudo (1 + 2)** — os dois.
3b. **Recriar Boss com Rig Procedural** (também em `Pop Adventure > Fase 3`) — troca o visual do prefab do boss pelas suas peças com animação automática. Use se o prefab do boss já existia de antes (com placeholder + Animator).
4. **Preparar Player / Projétil** (opcional) — adiciona `GGPlayerHealth` + tag `Player` ao seu Player e `GGProjectileRelay` ao projétil (pede confirmação, modifica os seus prefabs).

> Rodar o passo 1 de novo regenera os prefabs (apaga ajustes feitos neles). **Exceção preservada:** `Boss_Graveyard` (prefab do boss) — nunca é recriado se já existir (só o botão **Recriar Boss com Rig Procedural** o atualiza, e mesmo assim no mesmo arquivo: a instância na cena e os campos do `GGBoss` ficam).

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
- `Platform_Glowing_Blink` / `_Rise` (`GGRhythmPlatform`): `Period Beats` 8, `On Beats` 6 (presente 3,2 s, ausente 1 s; a última batida presente é o AVISO — pisca devagar e continua sólida — e a última ausente mostra um "fantasma"). `Offset Beats` em cadeia: Blink 0, 2, 4, 6 — Rise 0, 3. `Fade Seconds` (Blink) e `Rise Seconds` (Rise) controlam a suavidade; maior = mais lento.
  - **Plataformas rápidas demais numa cena que você já construiu?** Menu **Pop Adventure > Fase 3 > Deixar plataformas rítmicas mais lentas** (ou o botão na janela) com a cena aberta: converte tudo que ainda está em Period 4 para o ciclo acima (Blink: offsets ×2; Rise: ×1,5) e os prefabs `Platform_Glowing_*`. Pode rodar de novo sem efeito. Depois salve a cena. À mão: selecione cada plataforma em `RhythmPlatforms` e ajuste os três campos.
- `Platform_Hanging_Rail`, `SpikePlatform_Moving`, `Gear_Rail`: `GGRailMover` → `Travel` (x, y), `Beats Per Cycle`.
- `SpikedBall_Swing_*`: `GGPendulum` → `Max Angle`, `Period Beats`. A raiz é o pivô.
- `ZombieHand`: `Period Beats`, `Offset Beats`, `Proximity Only`.
- Inimigos: `Enemy_Crow` (`Hover Mode` = paira), `Enemy_Wolf` (`Patrol Range`), `Enemy_Ghost`. Vida/dano: corvo 20/15, lobo 40/30, fantasma 10/15.
- Puzzle: `Resonance_Lantern`/`Resonance_Tombstone` + `Gate_Iron` (marque `Start Closed`) + `Door_Resonance` (arraste os alvos em `Targets` e o portão em `Gate`).

## 6. O boss (animação AUTOMÁTICA por código)

Prefab: `Generated/Prefabs/Boss/Boss_Graveyard` (já está na cena, em `BossFight`). Você NÃO anima nada: o `GGBossRig` move as 12 peças do zumbi (`Art/BossRig/gboss_*.png`) no ritmo da música (114 BPM).

**Passo a passo (uma vez):**
1. Copie a pasta `Fase3_Graveyard` inteira para `Assets` (inclui `Art/BossRig`).
2. Menu **Pop Adventure > Fase 3 > Recriar Boss com Rig Procedural** (ou o botão "Boss" na janela do construtor). Ele configura as 12 peças (PPU 530, pivô na articulação de cada uma) e monta/atualiza o prefab `Boss_Graveyard`.
3. **Teste rápido:** arraste `Boss_Graveyard` para uma cena vazia, selecione `Visual`, marque **Demo Loop** no `GGBossRig` e dê Play. Ele passa por tudo (dormindo, intro, dança, andar, deslize, mãos, giro, orbs, invocar, mudança de fase, dano, morte); o nome da etapa aparece no canto da tela. Desmarque depois.

```
Boss_Graveyard   GGBoss, Rigidbody2D (Kinematic), BoxCollider2D (hurtbox + dano de contato)   ← raiz = PÉS
  Visual         GGBossRig + SortingGroup (Order 2)  ← o código vira o scale.x aqui. NÃO mexa no scale.x do Visual.
    Rig          pés: recebe giro/pulo/queda/escala (giro de lado, morte)
      Hips       quadril (translada: agachar/dançar)
        Torso → Head, ArmFront_Upper → ArmFront_Lower, ArmBack_Upper → ArmBack_Lower
        LegFront_Thigh → LegFront_Calf → LegFront_Foot      (IK de 2 ossos: pés planos e plantados)
        LegBack_Thigh  → LegBack_Calf  → LegBack_Foot
  Muzzle         de onde saem os orbs (o X é "para frente": espelha sozinho conforme o lado)
  HealthBar      barra de vida em espaço de mundo
```

Cada junta é um GameObject com o SpriteRenderer da peça e o pivô do sprite exatamente na articulação (não mexa nas posições locais das juntas: o IK lê o esqueleto de repouso daqui).

**Animações (disparadas pelo `GGBoss`, todas travadas na batida):**

| Animação | Quando | O que faz |
|---|---|---|
| Dormant | antes da luta | largado, cabeça baixa, braços pendurados, respirando |
| Idle | descanso entre ataques | dança no ritmo: joelhos afundam a cada batida, cabeça balança, garras ondulam (mais intensa a cada fase) |
| Andar | automático | medido pela velocidade real do boss (anda para frente ou de costas); pés ao ritmo da passada |
| Intro | início da luta, 8 batidas | espasmos, acorda, levanta e ruge |
| Dash | deslize | 2 batidas agachado tremendo (aviso) → moonwalk deslizando → recuperação. Fase 3: vai, vira e volta |
| Hands | mãos de zumbi | levanta os braços e "rege": um braço pisa a cada batida junto das mãos no chão |
| Spin | giro | agacha, gira de lado com pulinho e **pisa numa batida: a onda de choque sai nesse instante** (1 / 2 / 3 giros nas fases 1 / 2 / 3) |
| Orbs | orbs (fase 2+) | empurra as mãos para frente a cada batida; cada orb sai nesse empurrão |
| Summon | fantasmas (fase 2+) | braços para o alto; pisão em 2 batidas e os fantasmas aparecem |
| PhaseChange | HP 66 % e 33 %, 4 batidas | agacha e ruge tremendo |
| Hurt | levou dano | recuo sobreposto (tronco, cabeça, braços), no máx. 1 a cada 0,2 s |
| Die | derrotado | levanta os braços, cai para trás e fica largado no chão (o fragmento aparece 1,8 s depois) |

**Ajustes finos (sem mexer no resto):**
- Intensidade, amplitudes e ângulos de cada pose: `Scripts/Runtime/GGBossMotion.cs` (funções `Idle`, `Walk`, `Roar`, `Dash`, `Hands`, `Spin`, `Orbs`, `Summon`, `PhaseChange`, `Die`, `HurtDelta`). São só números em graus/unidades; ângulo positivo = "para frente/para cima".
- Velocidade dos passos: `GGBossRig > Walk Stride` (distância por ciclo; menor = pés mais rápidos). `Walk Threshold` = velocidade mínima para andar. `Crossfade Seconds` = suavização entre animações.
- Duração dos giros / dos avisos: `GGBoss.cs` (`SpinAttack`: 4 batidas na fase 1 e 3 nas outras; `DashAttack`: aviso 2 / 1 batida).
- Ordem de desenho do boss (se o Player ficar atrás dele): `Visual > SortingGroup > Order in Layer`.
- Flash de dano e Hurt funcionam sozinhos. O `GGBoss` descobre sozinho para onde o desenho olha (o zumbi enviado olha para a **esquerda**); não precisa mexer em `Sprite Faces Right`.
- Quer animar à mão em vez do rig? Deixe o campo **Rig** do `GGBoss` vazio e use um Animator no `Visual` com os parâmetros `IsMoving`, `Intro`, `AttackDash`, `AttackHands`, `AttackSpin`, `AttackOrbs`, `AttackSummon`, `Hurt`, `PhaseChange`, `Die` (os nomes podem ser trocados no `GGBoss`; se um não existir, é ignorado).

Luta: HP 250, 3 fases (66 % / 33 %). Ataques: Deslize (30 de dano), Mãos de zumbi (15, sempre sobra um vão), Giro com ondas no chão (pule), Orbs (fase 2+, a nota destrói), Fantasmas (fase 2+). Boss largando até 2 notas de cura por luta. Se o Player morrer na luta, tudo reinicia no checkpoint.

## 6b. Cutscene (intro do boss, derrota, fim de fase)

Script: `GGCutscene` (em `Scripts/Runtime`). É uma cutscene **por passos**: cada passo espera um tempo e dispara um UnityEvent.

Criar:
1. `GameObject > Create Empty`, nome `Cutscene_BossIntro`. `Add Component > GGCutscene`.
2. `Time Unit`: `Beats` (sincroniza com a música, 114 BPM) ou `Seconds`.
3. `Disable During Cutscene`: arraste o **PlayerController** da instância do Player na cena (ele é desligado durante a cutscene e religado no fim). Deixe `Stop Player Motion` marcado.
4. Em `Steps`, adicione um item por momento. `Wait` = espera ANTES do passo. No evento do passo, clique em `+`, arraste um objeto e escolha o método. Exemplos:
   - Passo 1 (`Wait` 0): `Cutscene_BossIntro > GGCutscene.ShakeCamera` com `0.4`. (O boss fica largado/dormindo durante a cutscene e acorda sozinho — animação Intro de 8 batidas — quando a luta começa.)
   - Passo 2 (`Wait` 2): seu painel de diálogo (UI Toolkit) ou `GameObject.SetActive`.
   - Passo 3 (`Wait` 3): esconder o diálogo.
   Também funciona `AudioSource.Play`, `PlayableDirector.Play` (Timeline) e qualquer método público dos seus scripts.
5. Para pular: ligue um botão ao método `GGCutscene.Skip()` (dispara os passos restantes na hora e termina).

Ligar:
- **Intro do boss:** selecione `BossFight > BossArena` e arraste `Cutscene_BossIntro` em `GGBossArena > Intro Cutscene`. A ordem fica: portões fecham, câmera trava, a cutscene toca e só quando termina a luta começa. Se o Player morrer durante ela, é interrompida e toca de novo na próxima tentativa.
- **Derrota do boss:** crie outra (`Cutscene_BossDefeated`) e arraste em `Defeated Cutscene`. O fragmento do Disco Dourado aparece 1,8 s depois da morte; use `Wait` 2 ou mais para a cutscene entrar depois dele.
- **Qualquer outro momento:** em um UnityEvent, por exemplo `GGLevelManager.onFragmentCollected`, arraste o objeto e escolha `GGCutscene.Play`. Ao terminar, `onFinished` pode chamar o seu SceneController.
- **Ao carregar a cena:** marque `Play On Start`.

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
  Art/BossRig/ as 12 peças do zumbi (o construtor configura pivô e PPU delas)
  Scripts/Runtime/   código da fase (prefixo GG). Boss: GGBoss (lógica), GGBossRig (animação automática), GGBossMotion (poses — os números para ajustar), GGBossPose/GGBossSolver (matemática do rig)
  Scripts/Editor/    construtor (menu Pop Adventure > Fase 3)
  Generated/   criado pelo construtor: Tiles, Palettes, Prefabs
```
