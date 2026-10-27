using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PopAdventure.Hive
{
    public enum HiveMarkerType
    {
        PlayerSpawn,
        Checkpoint,
        VinylRow,
        HealNote,
        BeatPlatform,
        MovingPlatform,
        CrumblePlatform,
        HoneyPool,
        HoneyDrip,
        KillZone,
        WaxThorns,
        WorkerBee,
        HornetGuard,
        BossArena,
        LevelExit,
        BlockoutBlock,
        MusicCue   // no FIM da lista de propósito: não muda os valores salvos dos marcadores já existentes
    }

    /// <summary>
    /// Marcador invisível que você coloca na cena (use a janela "Paleta de Marcadores").
    /// Em Play, o HiveLevelBootstrap lê todos os marcadores e CRIA os objetos reais por código.
    /// Você só pinta os tiles; todo o resto é marcador.
    /// </summary>
    [DisallowMultipleComponent]
    public class HiveMarker : MonoBehaviour
    {
        public HiveMarkerType type = HiveMarkerType.BeatPlatform;

        [Tooltip("Largura/altura (plataformas, poças, zonas, arena, espinhos = só largura).")]
        public Vector2 size = new Vector2(3f, 0.5f);

        [Tooltip("MovingPlatform: deslocamento até o ponto B. Abelha/Vespa: x = alcance total da patrulha.")]
        public Vector2 travel = new Vector2(4f, 0f);

        [Tooltip("MovingPlatform: batidas por trecho. CrumblePlatform: batidas tremendo antes de cair. HoneyDrip: a cada N batidas.")]
        public int beats = 4;

        [Tooltip("BeatPlatform: 1 = sólida, 0 = some. Cada caractere = 1 batida. Ex.: 1100, 1010, 10.")]
        public string pattern = "1100";

        [Tooltip("Deslocamento em batidas (BeatPlatform / MovingPlatform / HoneyDrip).")]
        public int offset = 0;

        [Tooltip("VinylRow: quantidade. CrumblePlatform: compassos até reaparecer.")]
        public int count = 5;

        [Tooltip("VinylRow: distância entre vinis.")]
        public float spacing = 1f;

        [Tooltip("VinylRow: altura do arco no meio da fileira (0 = reta).")]
        public float arc = 0f;

        [Tooltip("Vida (Abelha/Vespa/BossArena; 0 = padrão) ")]
        public float value = 0f;

        [Tooltip("BeatPlatform: 0 = mel (dourada), 1 = cera escura com luz rosa. BlockoutBlock: 0 chão, 1 plataforma, 2 dourado. MusicCue: 1 = LOOP PRINCIPAL, 2 = COMBATE.")]
        public int tier = 0;

        public Vector2 TravelPoint { get { return (Vector2)transform.position + travel; } }

        /// <summary>Posição do i-ésimo vinil de uma VinylRow (usada pelo gizmo e pelo Bootstrap).</summary>
        public Vector3 VinylPoint(int i)
        {
            int n = Mathf.Max(1, count);
            float x = (i - (n - 1) * 0.5f) * spacing;
            float t = n > 1 ? (float)i / (n - 1) : 0.5f;
            float y = arc * 4f * t * (1f - t);
            return transform.position + new Vector3(x, y, 0f);
        }

        /// <summary>Valores padrão sensatos para cada tipo (chamado pela paleta ao criar).</summary>
        public void ApplyDefaults()
        {
            switch (type)
            {
                case HiveMarkerType.BeatPlatform: size = new Vector2(3f, 0.5f); pattern = "1100"; offset = 0; tier = 0; break;
                case HiveMarkerType.MovingPlatform: size = new Vector2(3f, 0.5f); travel = new Vector2(5f, 0f); beats = 4; break;
                case HiveMarkerType.CrumblePlatform: size = new Vector2(2.5f, 0.5f); beats = 2; count = 2; break;
                case HiveMarkerType.HoneyPool: size = new Vector2(4f, 0.8f); break;
                case HiveMarkerType.HoneyDrip: beats = 4; offset = 0; break;
                case HiveMarkerType.KillZone: size = new Vector2(8f, 1.5f); break;
                case HiveMarkerType.WaxThorns: size = new Vector2(2f, 0.625f); break;
                case HiveMarkerType.WorkerBee: travel = new Vector2(6f, 0f); value = 0f; break;
                case HiveMarkerType.HornetGuard: travel = new Vector2(6f, 0f); value = 0f; break;
                case HiveMarkerType.BossArena: size = new Vector2(22f, 10f); value = 0f; break;
                case HiveMarkerType.VinylRow: count = 5; spacing = 1f; arc = 1.5f; break;
                case HiveMarkerType.BlockoutBlock: size = new Vector2(10f, 2f); tier = 0; break;
                case HiveMarkerType.MusicCue: size = new Vector2(2f, 16f); tier = 2; break;
            }
        }

        public static string Describe(HiveMarkerType t)
        {
            switch (t)
            {
                case HiveMarkerType.PlayerSpawn: return "Onde o Player nasce. Ponha 1 em cima do chão, no começo da fase.";
                case HiveMarkerType.Checkpoint: return "Lanterna de checkpoint. Posição = pé da lanterna (em cima do chão).";
                case HiveMarkerType.VinylRow: return "Fileira de vinis. count, spacing e arc (altura do arco).";
                case HiveMarkerType.HealNote: return "Nota que cura +15 HP (fixa na fase).";
                case HiveMarkerType.BeatPlatform: return "Plataforma que liga/desliga no ritmo. pattern: 1 = sólida, 0 = some (1 char = 1 batida).";
                case HiveMarkerType.MovingPlatform: return "Plataforma que vai e volta sincronizada à música. travel = ponto B; beats = batidas por trecho.";
                case HiveMarkerType.CrumblePlatform: return "Treme ao ser pisada e cai após 'beats' batidas; volta depois de 'count' compassos.";
                case HiveMarkerType.HoneyPool: return "Poça de mel: limita velocidade e pulo do Player. Posição = centro da zona.";
                case HiveMarkerType.HoneyDrip: return "Goteira no teto: solta gota de mel (15 de dano) a cada 'beats' batidas. Ponha COLADA no teto.";
                case HiveMarkerType.KillZone: return "Abismo: leva ao último checkpoint com 15 de dano. Cubra o buraco inteiro.";
                case HiveMarkerType.WaxThorns: return "Espinhos de cera (15 de dano). size.x = largura. Posição = centro, em cima do chão.";
                case HiveMarkerType.WorkerBee: return "Abelha voadora (20 HP, 15 dano). Telegrafa na batida 1, mergulha na batida 3.";
                case HiveMarkerType.HornetGuard: return "Vespa-guarda terrestre (60 HP, 30 dano). Patrulha e investe no ritmo. Ponha em cima do chão.";
                case HiveMarkerType.BossArena: return "Arena da Rainha Abelha. Posição = centro. O chão da arena é o Y do marcador MENOS size.y/2.";
                case HiveMarkerType.LevelExit: return "Portal de saída (só abre depois da Rainha, se houver arena).";
                case HiveMarkerType.BlockoutBlock: return "Bloco sólido de teste (substitui tiles temporariamente).";
                case HiveMarkerType.MusicCue: return "Troca a música quando o Player passa. tier 1 = LOOP PRINCIPAL, tier 2 = COMBATE. O boss troca sozinho ao entrar na arena. Faça a zona alta (16) para não dar pra desviar por cima.";
            }
            return "";
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 p = transform.position;
            Color c = HivePalette.Honey;
            string label = type.ToString();

            switch (type)
            {
                case HiveMarkerType.PlayerSpawn:
                    c = Color.green;
                    Gizmos.color = c;
                    Gizmos.DrawWireSphere(p, 0.4f);
                    Gizmos.DrawLine(p, p + Vector3.up * 1.2f);
                    break;
                case HiveMarkerType.Checkpoint:
                    c = HivePalette.Pink;
                    Gizmos.color = c;
                    Gizmos.DrawLine(p, p + Vector3.up * 1.5f);
                    Gizmos.DrawWireSphere(p + Vector3.up * 1.1f, 0.3f);
                    break;
                case HiveMarkerType.VinylRow:
                    c = Color.white;
                    Gizmos.color = c;
                    for (int i = 0; i < Mathf.Max(1, count); i++)
                        Gizmos.DrawWireSphere(VinylPos(i), 0.22f);
                    label += " x" + count;
                    break;
                case HiveMarkerType.HealNote:
                    c = HivePalette.Pink;
                    Gizmos.color = c;
                    Gizmos.DrawWireSphere(p, 0.3f);
                    break;
                case HiveMarkerType.BeatPlatform:
                    c = tier == 0 ? HivePalette.Gold : HivePalette.Cyan;
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    label += " " + pattern;
                    break;
                case HiveMarkerType.MovingPlatform:
                    c = HivePalette.Honey;
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    Gizmos.DrawWireCube(TravelPoint, new Vector3(size.x, size.y, 0.1f));
                    Gizmos.DrawLine(p, TravelPoint);
                    break;
                case HiveMarkerType.CrumblePlatform:
                    c = HivePalette.Amber;
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    break;
                case HiveMarkerType.HoneyPool:
                    c = new Color(1f, 0.7f, 0f, 0.9f);
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    break;
                case HiveMarkerType.HoneyDrip:
                    c = HivePalette.Honey;
                    Gizmos.color = c;
                    Gizmos.DrawWireSphere(p, 0.2f);
                    Gizmos.DrawLine(p, p + Vector3.down * 3f);
                    label += " /" + beats;
                    break;
                case HiveMarkerType.KillZone:
                    c = Color.red;
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    break;
                case HiveMarkerType.WaxThorns:
                    c = HivePalette.Danger;
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, 0.625f, 0.1f));
                    break;
                case HiveMarkerType.WorkerBee:
                case HiveMarkerType.HornetGuard:
                    c = type == HiveMarkerType.WorkerBee ? HivePalette.Gold : HivePalette.Danger;
                    Gizmos.color = c;
                    Gizmos.DrawWireSphere(p, 0.5f);
                    Gizmos.DrawLine(p - new Vector3(travel.x * 0.5f, 0f, 0f), p + new Vector3(travel.x * 0.5f, 0f, 0f));
                    break;
                case HiveMarkerType.BossArena:
                    c = HivePalette.Pink;
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    Gizmos.color = new Color(c.r, c.g, c.b, 0.5f);
                    Gizmos.DrawLine(p + new Vector3(-size.x * 0.5f, -size.y * 0.5f, 0f), p + new Vector3(size.x * 0.5f, -size.y * 0.5f, 0f));
                    label += " (chão = base do retângulo)";
                    break;
                case HiveMarkerType.LevelExit:
                    c = HivePalette.Gold;
                    Gizmos.color = c;
                    Gizmos.DrawWireSphere(p, 1f);
                    break;
                case HiveMarkerType.BlockoutBlock:
                    c = new Color(0.6f, 0.4f, 0.1f, 1f);
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    break;
                case HiveMarkerType.MusicCue:
                    c = tier == 1 ? HivePalette.Cyan : new Color(0.7f, 0.4f, 1f, 1f);
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(p, new Vector3(size.x, size.y, 0.1f));
                    label += tier == 1 ? " → LOOP" : " → COMBATE";
                    break;
            }

            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = c;
            style.fontSize = 10;
            Handles.Label(p + Vector3.up * 0.6f, label, style);
        }

        private Vector3 VinylPos(int i)
        {
            return VinylPoint(i);
        }
#endif
    }
}
