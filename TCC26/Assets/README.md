# POP ADVENTURE — Unity 2D Setup Guide
## C# Scripts — Guia de Configuração

---

## 📁 Estrutura de Pastas no Unity
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── RhythmManager.cs       ← CORAÇÃO do jogo (BPM, eventos de beat)
│   │   ├── GameManager.cs         ← Estado global (vinis, fragmentos, upgrades)
│   │   └── CameraShake.cs         ← Pulso e tremor sincronizados
│   ├── Player/
│   │   ├── PlayerController.cs    ← Enzo: movimento, pulo, ataque
│   │   └── MusicProjectile.cs     ← Nota musical (projétil)
│   ├── Enemies/
│   │   ├── EnemyBase.cs           ← Classe base de todos os inimigos
│   │   └── EnemyPhase1.cs         ← Dançarino Disco + Bola de Disco
│   ├── Boss/
│   │   └── BossDonna.cs           ← Boss Fase 1 (Disco Fever)
│   ├── World/
│   │   ├── RhythmicPlatform.cs    ← Plataformas sincronizadas com o beat
│   │   ├── RhythmActivatable.cs   ← Luzes, portas ativáveis por ataque
│   │   └── Checkpoint.cs          ← Checkpoints + BossTrigger + Transição de cor
│   ├── Items/
│   │   └── Collectibles.cs        ← Vinyl, Nota Musical, Fragmento, Microfone
│   └── UI/
│       ├── HUDController.cs       ← HUD (vida, vinis, fragmentos, pulse)
│       └── HubUpgradeShop.cs      ← Loja de upgrades do Hub
```

---

## 🔧 Setup Obrigatório no Unity

### 1. RhythmManager (GameObject vazio — DontDestroyOnLoad)
- Adicione o componente `RhythmManager`
- Adicione também um `AudioSource` no mesmo objeto e arraste-o para o campo `musicSource`
- Configure o BPM inicial (120 para a Fase 1 — Disco)
- Este objeto persiste entre cenas automaticamente

### 2. GameManager (GameObject vazio — DontDestroyOnLoad)
- Adicione o componente `GameManager`
- Persiste entre cenas automaticamente

### 3. CameraShake (na Main Camera)
- Adicione `CameraShake` na Main Camera
- Adicione `Camera` (já existe por padrão)

### 4. PlayerController (Prefab do Enzo)
**Hierarquia sugerida:**
```
Enzo (PlayerController, Rigidbody2D, Animator, AudioSource)
├── Sprite (SpriteRenderer)
├── GroundCheck (Transform — posicionado embaixo dos pés)
├── AttackPoint (Transform — posicionado na frente)
└── Collider (CapsuleCollider2D)
```
**Configurações do Rigidbody2D:**
- Freeze Rotation Z: ✓
- Collision Detection: Continuous

**Tags:** O GameObject do player deve ter a tag `"Player"`

**Layers:**
- Crie a layer `"Enemy"` e configure o campo `enemyLayer` do PlayerController
- Crie a layer `"Ground"` para o chão e configure `groundLayer`

### 5. Inimigos (Prefabs)
**Estrutura base:**
```
Enemy (EnemyBase/EnemyDiscoDancer, Rigidbody2D, Animator, AudioSource)
├── Sprite (SpriteRenderer)
└── Collider (CapsuleCollider2D)
```
- Layer: `"Enemy"`
- O player deve ter layer `"Player"` para que a detecção funcione

### 6. Plataformas Rítmicas
- Adicione `RhythmicPlatform` em qualquer plataforma
- Configure o `PlatformMode` desejado
- Certifique-se que o `RhythmManager` está ativo na cena

### 7. BossDonna (Fase 1)
```
BossDonna (BossDonna, Animator, AudioSource, SpriteRenderer)
```
- Crie um `BossTrigger` (com Collider2D IsTrigger=true) antes da arena
- Arraste a referência do boss para o trigger

### 8. HUD (Canvas)
```
Canvas (HUDController)
├── HealthSlider (Slider)
├── VinylCountText (TextMeshPro)
├── VinylIcon (Image)
├── BeatIndicator (Image — pulsa no ritmo)
├── FragmentSlots/
│   ├── Fragment1 (Image)
│   ├── Fragment2 (Image)
│   ├── Fragment3 (Image)
│   └── Fragment4 (Image)
└── MicrophoneIndicator (Image)
```

---

## 🎮 Tags Necessárias (Edit > Project Settings > Tags)
- `Player`
- `Enemy`
- `Ground`
- `Wall`

---

## 📦 Pacotes Recomendados (Package Manager)
- **TextMeshPro** (incluído no Unity) — para textos da UI
- **Universal Render Pipeline (URP)** — para `Light2D` das luzes disco
- **Input System** (opcional) — para suporte a gamepad avançado

---

## 🎵 Configuração do Sistema Rítmico

O `RhythmManager` usa `AudioSettings.dspTime` para precisão máxima.

```
Fase 1 — Disco Fever: BPM 120 → BeatInterval = 0.5s
Fase 2 — The Hive:    BPM 128 → BeatInterval = 0.469s
Fase 3 — Graveyard:   BPM 110 → BeatInterval = 0.545s
Fase 4 — Theatre:     BPM 140 → BeatInterval = 0.429s
```

Para iniciar a música com BPM correto em cada fase:
```csharp
RhythmManager.Instance.StartMusic(myClip, 120f);
```

---

## ⬆️ Sistema de Upgrades (Hub)
| Upgrade | Efeito | Custo |
|---------|--------|-------|
| Amplificador | +2 dano | 25 Vinis |
| Salto Harmônico | +10% pulo | 30 Vinis |
| Vitalidade | +20 HP máx | 40 Vinis |

---

## 🗺️ Cenas Sugeridas
| Cena | Nome no Build Settings |
|------|------------------------|
| Hub / Porão | `Hub` |
| Fase 1 - Disco | `Phase1` |
| Fase 2 - Colmeia | `Phase2` |
| Fase 3 - Cemitério | `Phase3` |
| Fase 4 - Teatro | `Phase4` |

Use `GameManager.Instance.LoadPhase(1)` para carregar fases.

---

## 🎨 Assets Gratuitos Recomendados (para protótipo)

### Sprites / Arte
- **itch.io** — busque "2D platformer free pixel art"
- **Kenney.nl** → `kenney.nl/assets` — pacotes gratuitos de alta qualidade
- **OpenGameArt.org** — `opengameart.org`

### Música / SFX
- **FreeMusicArchive.org** — música livre de royalties
- **Freesound.org** — efeitos sonoros
- **ZapSplat.com** — SFX profissionais gratuitos

### Fontes Pop Art
- **Google Fonts** → "Bangers", "Boogaloo", "Righteous"

---

## ✅ Checklist de Implementação (Vertical Slice)

- [x] RhythmManager (BPM, beat events)
- [x] PlayerController (movimento, pulo, ataque)
- [x] MusicProjectile (nota musical)
- [x] EnemyBase (IA base, ritmo)
- [x] EnemyDiscoDancer + EnemyDiscoBall (Fase 1)
- [x] BossDonna (Fase 1, 2 fases de combate)
- [x] RhythmicPlatform (aparecer/sumir/mover no beat)
- [x] Collectibles (Vinyl, Nota, Fragmento, Microfone)
- [x] GameManager (estado global, upgrades, save)
- [x] HUDController (vida, vinis, fragmentos, pulse)
- [x] HubUpgradeShop (loja do hub)
- [x] Checkpoint + BossTrigger
- [ ] Animações (Animator Controllers para cada entidade)
- [ ] Level Design da Fase 1 (Unity TileMap ou sprites)
- [ ] Arte e SFX
- [ ] Menu principal e tela de game over
