using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Barra de vida do boss (UI Toolkit). Criada pelo MTBossController ao começar a luta.
    /// Usa o PanelSettings do seu HUD; se não houver, cai para uma barra simples presa à câmera.
    /// </summary>
    public class MTBossHealthBar : MonoBehaviour
    {
        MTBossController boss;
        PanelSettings panelSettings;
        string bossName = "BOSS";

        UIDocument doc;
        VisualElement fill;
        VisualElement ghost;
        float shown = 1f;

        // fallback no mundo
        Transform worldFill;
        const float WorldWidth = 12f;

        public static MTBossHealthBar Create(MTBossController boss, PanelSettings ps, string name)
        {
            GameObject go = new GameObject("MT_BossHealthBar");
            go.SetActive(false);
            MTBossHealthBar bar = go.AddComponent<MTBossHealthBar>();
            bar.boss = boss;
            bar.panelSettings = ps;
            bar.bossName = name;
            go.SetActive(true);
            return bar;
        }

        IEnumerator Start()
        {
            if (panelSettings == null)
            {
                BuildWorldBar();
                yield break;
            }

            doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            int guard = 0;
            while (doc.rootVisualElement == null && guard++ < 30) yield return null;
            if (doc.rootVisualElement == null)
            {
                BuildWorldBar();
                yield break;
            }
            BuildUi(doc.rootVisualElement);
        }

        void BuildUi(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;

            Color gold = new Color(1f, 0.82f, 0.25f, 1f);

            VisualElement wrap = new VisualElement();
            wrap.pickingMode = PickingMode.Ignore;
            wrap.style.position = Position.Absolute;
            wrap.style.top = 24f;
            wrap.style.left = Length.Percent(20f);
            wrap.style.width = Length.Percent(60f);
            root.Add(wrap);

            Label label = new Label(bossName);
            label.pickingMode = PickingMode.Ignore;
            label.style.color = gold;
            label.style.fontSize = 22f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            wrap.Add(label);

            VisualElement frame = new VisualElement();
            frame.pickingMode = PickingMode.Ignore;
            frame.style.height = 22f;
            frame.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
            frame.style.borderTopWidth = 2f;
            frame.style.borderBottomWidth = 2f;
            frame.style.borderLeftWidth = 2f;
            frame.style.borderRightWidth = 2f;
            frame.style.borderTopColor = gold;
            frame.style.borderBottomColor = gold;
            frame.style.borderLeftColor = gold;
            frame.style.borderRightColor = gold;
            wrap.Add(frame);

            ghost = new VisualElement();
            ghost.pickingMode = PickingMode.Ignore;
            ghost.style.position = Position.Absolute;
            ghost.style.top = 0f;
            ghost.style.bottom = 0f;
            ghost.style.left = 0f;
            ghost.style.width = Length.Percent(100f);
            ghost.style.backgroundColor = new Color(1f, 1f, 1f, 0.55f);
            frame.Add(ghost);

            fill = new VisualElement();
            fill.pickingMode = PickingMode.Ignore;
            fill.style.position = Position.Absolute;
            fill.style.top = 0f;
            fill.style.bottom = 0f;
            fill.style.left = 0f;
            fill.style.width = Length.Percent(100f);
            fill.style.backgroundColor = new Color(0.85f, 0.05f, 0.15f, 1f);
            frame.Add(fill);

            MakeTick(frame, boss != null ? boss.form2At : 0.66f, gold);
            MakeTick(frame, boss != null ? boss.form3At : 0.33f, gold);
        }

        static VisualElement MakeTick(VisualElement parent, float at, Color c)
        {
            VisualElement t = new VisualElement();
            t.pickingMode = PickingMode.Ignore;
            t.style.position = Position.Absolute;
            t.style.top = 0f;
            t.style.bottom = 0f;
            t.style.left = Length.Percent(at * 100f);
            t.style.width = 2f;
            t.style.backgroundColor = c;
            parent.Add(t);
            return t;
        }

        void BuildWorldBar()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Transform root = new GameObject("MT_BossBarWorld").transform;
            root.SetParent(transform, false);
            root.SetParent(cam.transform, false);
            root.localPosition = new Vector3(0f, cam.orthographic ? cam.orthographicSize - 0.9f : 3f, 10f);
            root.localRotation = Quaternion.identity;

            MakeWorldQuad(root, "Bg", new Color(0f, 0f, 0f, 0.8f), new Vector2(WorldWidth + 0.2f, 0.6f), 100);
            worldFill = MakeWorldQuad(root, "Fill", new Color(0.85f, 0.05f, 0.15f, 1f), new Vector2(WorldWidth, 0.4f), 101);
        }

        static Transform MakeWorldQuad(Transform parent, string n, Color c, Vector2 size, int order)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MTUtil.WhiteSprite;
            sr.color = c;
            sr.sortingOrder = order;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return go.transform;
        }

        void Update()
        {
            if (boss == null) { Destroy(gameObject); return; }

            float h = boss.Health01;
            shown = Mathf.MoveTowards(shown, h, 0.35f * Time.deltaTime);

            if (fill != null)
            {
                fill.style.width = Length.Percent(h * 100f);
                ghost.style.width = Length.Percent(shown * 100f);
            }
            if (worldFill != null)
            {
                Vector3 s = worldFill.localScale;
                s.x = WorldWidth * h;
                worldFill.localScale = s;
                Vector3 p = worldFill.localPosition;
                p.x = -WorldWidth * 0.5f + s.x * 0.5f;
                worldFill.localPosition = p;
            }

            if (boss.IsDead) Destroy(gameObject, 3f);
        }
    }
}
