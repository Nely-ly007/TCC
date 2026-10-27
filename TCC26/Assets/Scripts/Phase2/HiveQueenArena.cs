using System.Collections;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Arena da Rainha Abelha. Criada pelo Bootstrap a partir do marcador BossArena.
    /// - Trigger interno detecta o Player entrando → fecha os portões, mostra a barra e solta a Rainha.
    /// - Morte do Player durante a luta → ResetFight() (portões abrem, boss some, curas resetam).
    /// - Rainha derrotada → cai o Fragmento do Disco Dourado; pegar o fragmento conclui a fase.
    /// O chão da arena é a base do retângulo do marcador (centro.y - size.y/2).
    /// </summary>
    public class HiveQueenArena : MonoBehaviour
    {
        private Vector2 size;
        private float bossHp;
        private float minX, maxX, floorY, ceilY;
        private Vector2 center;

        private HiveQueenBoss boss;
        private GameObject gateL, gateR;
        private bool fighting;
        private bool defeated;
        private int healsDropped;

        public bool Defeated { get { return defeated; } }
        public bool Fighting { get { return fighting; } }

        public void Init(Vector2 arenaSize, float hp)
        {
            size = arenaSize;
            bossHp = hp;
            center = transform.position;
            minX = center.x - size.x * 0.5f;
            maxX = center.x + size.x * 0.5f;
            floorY = center.y - size.y * 0.5f;
            ceilY = center.y + size.y * 0.5f;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(Mathf.Max(2f, size.x - 4f), size.y);
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (fighting || defeated || !HiveBridge.IsPlayer(o)) return;
            StartFight();
        }

        // ============================================================ Luta

        private void StartFight()
        {
            fighting = true;
            healsDropped = 0;
            HiveLevelBootstrap.BossActive = true;

            gateL = MakeGate(minX + 0.5f);
            gateR = MakeGate(maxX - 0.5f);

            HiveSfx.Play(HiveSfxId.Gate, 0.8f, 1f);
            HiveMusic.Request(HiveMusicSection.Boss);   // troca no próximo compasso
            HiveBridge.ShakeCamera(0.25f, 0.4f);
            HiveOverlay.Banner("QUEEN BEE", "Fase 2 · The Hive", 2.2f);
            HiveOverlay.ShowBoss("QUEEN BEE");
            HiveOverlay.SetBossHealth(1f);

            var go = new GameObject("QueenBoss");
            go.transform.SetParent(transform, true);
            boss = go.AddComponent<HiveQueenBoss>();
            boss.Init(this, minX + 1.5f, maxX - 1.5f, floorY, ceilY, bossHp);
        }

        /// <summary>Chamado pelo Bootstrap quando o Player morre/volta ao checkpoint durante a luta.</summary>
        public void ResetFight()
        {
            if (!fighting || defeated) return;
            fighting = false;
            if (boss != null) Destroy(boss.gameObject);
            boss = null;
            HiveHostile.ClearAll();
            ClearMinions();
            DestroyPickupsOfType();
            if (gateL != null) Destroy(gateL);
            if (gateR != null) Destroy(gateR);
            HiveLevelBootstrap.BossActive = false;
            HiveOverlay.HideBoss();
        }

        private void DestroyPickupsOfType()
        {
            foreach (var p in GetComponentsInChildren<HivePickup>())
                if (p != null) Destroy(p.gameObject);
        }

        public void OnBossDefeated()
        {
            defeated = true;
            HiveOverlay.HideBoss();
        }

        public void SpawnFragment(Vector2 at)
        {
            HiveLevelBootstrap.BossActive = false;
            HivePickup.Create(HivePickupType.Fragment, at, 1, true, transform);
            HiveOverlay.Banner("QUEEN DEFEATED", "Pegue o fragmento do Disco Dourado", 2.6f);
            if (gateL != null) StartCoroutine(OpenGate(gateL));
            if (gateR != null) StartCoroutine(OpenGate(gateR));

            var b = HiveLevelBootstrap.Instance;
            if (b != null) b.OnBossDefeated();
        }

        // ============================================================ Serviços para o boss

        public void SetBossBar(float normalized)
        {
            HiveOverlay.SetBossHealth(normalized);
        }

        public int AliveBees()
        {
            return GetComponentsInChildren<HiveWorkerBee>().Length;
        }

        public void SpawnBee(Vector2 at)
        {
            var go = new GameObject("MinionBee");
            go.transform.SetParent(transform, true);
            go.transform.position = at;
            var bee = go.AddComponent<HiveWorkerBee>();
            bee.Init(5f, 20f);
        }

        public void ClearMinions()
        {
            foreach (var bee in GetComponentsInChildren<HiveWorkerBee>())
                if (bee != null) Destroy(bee.gameObject);
        }

        public void DropHeal()
        {
            if (healsDropped >= 2) return;
            healsDropped++;
            float x = Mathf.Clamp(center.x + Random.Range(-3f, 3f), minX + 2f, maxX - 2f);
            HivePickup.Create(HivePickupType.Heal, new Vector2(x, ceilY - 1.5f), 15, true, transform);
            HiveOverlay.Toast("NOTA DE CURA!", HivePalette.Pink, 1.4f);
            HiveSfx.Play(HiveSfxId.Appear, 0.5f, 1.4f);
        }

        // ============================================================ Portões

        private GameObject MakeGate(float x)
        {
            var g = new GameObject("Gate");
            g.transform.SetParent(transform, true);
            g.transform.position = new Vector3(x, center.y, 0f);
            g.transform.localScale = new Vector3(1f, 0.01f, 1f);

            var col = g.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, size.y);

            var v = new GameObject("Visual");
            v.transform.SetParent(g.transform, false);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Block(1f, Mathf.Max(1f, Mathf.Round(size.y)), HiveBlockStyle.Gold);
            v.transform.localScale = new Vector3(1f, size.y / Mathf.Max(1f, Mathf.Round(size.y)), 1f);
            sr.sortingOrder = 6;

            StartCoroutine(CloseGate(g));
            return g;
        }

        private IEnumerator CloseGate(GameObject g)
        {
            float t = 0f;
            while (g != null && t < 0.5f)
            {
                t += Time.deltaTime;
                g.transform.localScale = new Vector3(1f, Mathf.Max(0.01f, HiveMath.SmoothStep01(t / 0.5f)), 1f);
                yield return null;
            }
            if (g != null) g.transform.localScale = Vector3.one;
        }

        private IEnumerator OpenGate(GameObject g)
        {
            float t = 0f;
            while (g != null && t < 0.6f)
            {
                t += Time.deltaTime;
                g.transform.localScale = new Vector3(1f, Mathf.Max(0.01f, 1f - HiveMath.SmoothStep01(t / 0.6f)), 1f);
                yield return null;
            }
            if (g != null) Destroy(g);
        }
    }
}
