using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Camada de UI da Fase 2 feita 100% em código (UI Toolkit): banner de título, barra do boss, toasts,
    /// flash de tela, contador de vinis da fase, marcador de batidas e tela de vitória.
    /// REUTILIZA o PanelSettings do HUD da Fase 1 (acha o primeiro UIDocument da cena), então a escala
    /// e o tema são os mesmos. O HUD (vida, vinis) continua sendo o da Fase 1 — aqui só entra o que é novo.
    /// </summary>
    public class HiveOverlay : MonoBehaviour
    {
        public static HiveOverlay Instance { get; private set; }

        private UIDocument doc;
        private VisualElement root;
        private bool built;
        private readonly List<Action> pending = new List<Action>();

        // banner
        private VisualElement bannerBox;
        private Label bannerTitle, bannerShadow, bannerSub;
        private float bannerT, bannerDur;

        // toast
        private VisualElement toastBox;
        private Label toastLabel;
        private float toastT, toastDur;

        // flash
        private VisualElement flashEl;
        private float flashT, flashDur, flashAlpha;
        private Color flashColor = Color.white;

        // boss
        private VisualElement bossBox, bossFill, bossGhost;
        private Label bossName;
        private float bossTarget = 100f, bossGhostPct = 100f;

        // beats
        private VisualElement[] beatDots;

        // vinis
        private Label vinylLabel;

        // vitória
        private VisualElement victoryBox;
        private Label victoryTitle, victoryLines, victoryHint;
        private Action onVictoryContinue;
        private bool victoryOpen;
        private float victoryReadyAt;

        // ============================================================ Criação

        public static HiveOverlay Create()
        {
            if (Instance != null) return Instance;

            int maxOrder = 0;
            PanelSettings ps = null;
            foreach (var d in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (d == null) continue;
                if (ps == null && d.panelSettings != null) ps = d.panelSettings;
                if (d.sortingOrder > maxOrder) maxOrder = Mathf.RoundToInt(d.sortingOrder);
            }
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                ps.referenceResolution = new Vector2Int(1920, 1080);
                ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                ps.match = 0.5f;
            }

            var go = new GameObject("HiveOverlay");
            go.SetActive(false);
            var ov = go.AddComponent<HiveOverlay>();
            ov.doc = go.AddComponent<UIDocument>();
            ov.doc.panelSettings = ps;
            ov.doc.sortingOrder = maxOrder + 1;
            go.SetActive(true);
            return ov;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() { HiveBeatDriver.OnBeat += HandleBeat; }
        private void OnDisable() { HiveBeatDriver.OnBeat -= HandleBeat; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            Build();
        }

        // ============================================================ Helpers de estilo

        private static VisualElement Box()
        {
            var v = new VisualElement();
            v.pickingMode = PickingMode.Ignore;
            return v;
        }

        private static void Fill(VisualElement v)
        {
            v.style.position = Position.Absolute;
            v.style.left = 0f;
            v.style.right = 0f;
            v.style.top = 0f;
            v.style.bottom = 0f;
        }

        private static Label Txt(string text, int size, Color color)
        {
            var l = new Label(text);
            l.pickingMode = PickingMode.Ignore;
            l.style.fontSize = size;
            l.style.color = color;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.whiteSpace = WhiteSpace.NoWrap;
            return l;
        }

        private static void Border(VisualElement v, float w, Color c)
        {
            v.style.borderTopWidth = w; v.style.borderBottomWidth = w;
            v.style.borderLeftWidth = w; v.style.borderRightWidth = w;
            v.style.borderTopColor = c; v.style.borderBottomColor = c;
            v.style.borderLeftColor = c; v.style.borderRightColor = c;
        }

        private static void Round(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = r; v.style.borderTopRightRadius = r;
            v.style.borderBottomLeftRadius = r; v.style.borderBottomRightRadius = r;
        }

        private static Color A(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        // ============================================================ Construção da UI

        private void Build()
        {
            if (built) return;
            root = doc.rootVisualElement;
            if (root == null) return;
            root.pickingMode = PickingMode.Ignore;

            // ---------- banner
            bannerBox = Box();
            bannerBox.style.position = Position.Absolute;
            bannerBox.style.left = 0f;
            bannerBox.style.right = 0f;
            bannerBox.style.top = Length.Percent(20);
            bannerBox.style.alignItems = Align.Center;
            bannerBox.style.opacity = 0f;
            bannerBox.style.display = DisplayStyle.None;

            var titleWrap = Box();
            bannerShadow = Txt("", 84, HivePalette.Black);
            bannerShadow.style.position = Position.Absolute;
            bannerShadow.style.left = 6;
            bannerShadow.style.top = 6;
            bannerTitle = Txt("", 84, HivePalette.Gold);
            titleWrap.Add(bannerShadow);
            titleWrap.Add(bannerTitle);
            bannerSub = Txt("", 30, HivePalette.Cream);
            bannerSub.style.marginTop = 6;
            bannerBox.Add(titleWrap);
            bannerBox.Add(bannerSub);
            root.Add(bannerBox);

            // ---------- barra do boss
            bossBox = Box();
            bossBox.style.position = Position.Absolute;
            bossBox.style.left = 0f;
            bossBox.style.right = 0f;
            bossBox.style.top = 34;
            bossBox.style.alignItems = Align.Center;
            bossBox.style.display = DisplayStyle.None;

            bossName = Txt("QUEEN BEE", 30, HivePalette.Gold);
            bossName.style.marginBottom = 4;
            var track = Box();
            track.style.width = 860;
            track.style.height = 30;
            track.style.backgroundColor = A(HivePalette.Black, 0.8f);
            Border(track, 3, HivePalette.Gold);
            Round(track, 15);
            track.style.overflow = Overflow.Hidden;

            bossGhost = Box();
            bossGhost.style.position = Position.Absolute;
            bossGhost.style.left = 0f;
            bossGhost.style.top = 0f;
            bossGhost.style.bottom = 0f;
            bossGhost.style.width = Length.Percent(100);
            bossGhost.style.backgroundColor = HivePalette.Honey;

            bossFill = Box();
            bossFill.style.position = Position.Absolute;
            bossFill.style.left = 0f;
            bossFill.style.top = 0f;
            bossFill.style.bottom = 0f;
            bossFill.style.width = Length.Percent(100);
            bossFill.style.backgroundColor = HivePalette.Pink;

            track.Add(bossGhost);
            track.Add(bossFill);
            bossBox.Add(bossName);
            bossBox.Add(track);
            root.Add(bossBox);

            // ---------- toast
            toastBox = Box();
            toastBox.style.position = Position.Absolute;
            toastBox.style.left = 0f;
            toastBox.style.right = 0f;
            toastBox.style.bottom = Length.Percent(24);
            toastBox.style.alignItems = Align.Center;
            toastBox.style.opacity = 0f;
            toastBox.style.display = DisplayStyle.None;
            toastLabel = Txt("", 40, Color.white);
            toastLabel.style.paddingLeft = 28;
            toastLabel.style.paddingRight = 28;
            toastLabel.style.paddingTop = 8;
            toastLabel.style.paddingBottom = 8;
            toastLabel.style.backgroundColor = A(HivePalette.Black, 0.72f);
            Round(toastLabel, 22);
            toastBox.Add(toastLabel);
            root.Add(toastBox);

            // ---------- marcador de batidas (4 pontos)
            var beatRow = Box();
            beatRow.style.position = Position.Absolute;
            beatRow.style.left = 0f;
            beatRow.style.right = 0f;
            beatRow.style.bottom = 26;
            beatRow.style.flexDirection = FlexDirection.Row;
            beatRow.style.justifyContent = Justify.Center;
            beatRow.style.alignItems = Align.Center;
            beatDots = new VisualElement[4];
            for (int i = 0; i < beatDots.Length; i++)
            {
                var d = Box();
                d.style.width = 20;
                d.style.height = 20;
                d.style.marginLeft = 7;
                d.style.marginRight = 7;
                d.style.backgroundColor = A(Color.white, 0.2f);
                Round(d, 10);
                beatDots[i] = d;
                beatRow.Add(d);
            }
            root.Add(beatRow);

            // ---------- contador de vinis da fase
            vinylLabel = Txt("", 26, Color.white);
            vinylLabel.style.position = Position.Absolute;
            vinylLabel.style.left = 28;
            vinylLabel.style.bottom = 22;
            vinylLabel.style.paddingLeft = 16;
            vinylLabel.style.paddingRight = 16;
            vinylLabel.style.paddingTop = 4;
            vinylLabel.style.paddingBottom = 4;
            vinylLabel.style.backgroundColor = A(HivePalette.Black, 0.6f);
            Round(vinylLabel, 16);
            vinylLabel.style.display = DisplayStyle.None;
            root.Add(vinylLabel);

            // ---------- flash
            flashEl = Box();
            Fill(flashEl);
            flashEl.style.backgroundColor = A(Color.white, 0f);
            root.Add(flashEl);

            // ---------- vitória
            victoryBox = Box();
            Fill(victoryBox);
            victoryBox.style.backgroundColor = A(HivePalette.Black, 0.82f);
            victoryBox.style.alignItems = Align.Center;
            victoryBox.style.justifyContent = Justify.Center;
            victoryBox.style.display = DisplayStyle.None;
            victoryTitle = Txt("PHASE CLEAR", 104, HivePalette.Gold);
            victoryLines = Txt("", 38, HivePalette.Cream);
            victoryLines.style.marginTop = 26;
            victoryLines.style.whiteSpace = WhiteSpace.Normal;
            victoryHint = Txt("ENTER / E / ESPAÇO para continuar", 30, HivePalette.Pink);
            victoryHint.style.marginTop = 46;
            victoryBox.Add(victoryTitle);
            victoryBox.Add(victoryLines);
            victoryBox.Add(victoryHint);
            root.Add(victoryBox);

            built = true;
            var q = pending.ToArray();
            pending.Clear();
            for (int i = 0; i < q.Length; i++) q[i]();
        }

        private void Run(Action a)
        {
            if (built) a(); else pending.Add(a);
        }

        // ============================================================ API estática (segura sem Overlay)

        public static void Banner(string title, string subtitle, float seconds)
        {
            if (Instance != null) Instance.Run(() => Instance.DoBanner(title, subtitle, seconds));
        }

        public static void Toast(string text, Color color, float seconds = 1.5f)
        {
            if (Instance != null) Instance.Run(() => Instance.DoToast(text, color, seconds));
        }

        public static void Flash(Color color, float alpha = 0.5f, float seconds = 0.35f)
        {
            if (Instance != null) Instance.Run(() => Instance.DoFlash(color, alpha, seconds));
        }

        public static void ShowBoss(string name)
        {
            if (Instance != null) Instance.Run(() => Instance.DoShowBoss(name));
        }

        public static void HideBoss()
        {
            if (Instance != null) Instance.Run(() => Instance.bossBox.style.display = DisplayStyle.None);
        }

        public static void SetBossHealth(float normalized)
        {
            if (Instance != null) Instance.Run(() => Instance.bossTarget = Mathf.Clamp01(normalized) * 100f);
        }

        public static void SetVinyls(int got, int total)
        {
            if (Instance != null) Instance.Run(() => Instance.DoVinyls(got, total));
        }

        public static void ShowVictory(string[] lines, Action onContinue)
        {
            if (Instance != null) Instance.Run(() => Instance.DoVictory(lines, onContinue));
            else if (onContinue != null) onContinue();
        }

        // ============================================================ Implementações

        private void DoBanner(string title, string sub, float seconds)
        {
            bannerTitle.text = title;
            bannerShadow.text = title;
            bannerSub.text = sub;
            bannerSub.style.display = string.IsNullOrEmpty(sub) ? DisplayStyle.None : DisplayStyle.Flex;
            bannerDur = Mathf.Max(0.6f, seconds);
            bannerT = bannerDur;
            bannerBox.style.display = DisplayStyle.Flex;
        }

        private void DoToast(string text, Color color, float seconds)
        {
            toastLabel.text = text;
            toastLabel.style.color = color;
            toastDur = Mathf.Max(0.4f, seconds);
            toastT = toastDur;
            toastBox.style.display = DisplayStyle.Flex;
        }

        private void DoFlash(Color color, float alpha, float seconds)
        {
            flashColor = color;
            flashAlpha = alpha;
            flashDur = Mathf.Max(0.05f, seconds);
            flashT = flashDur;
        }

        private void DoShowBoss(string name)
        {
            bossName.text = name;
            bossTarget = 100f;
            bossGhostPct = 100f;
            bossFill.style.width = Length.Percent(100);
            bossGhost.style.width = Length.Percent(100);
            bossBox.style.display = DisplayStyle.Flex;
        }

        private void DoVinyls(int got, int total)
        {
            vinylLabel.text = "VINIS  " + got + " / " + total;
            vinylLabel.style.display = total > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void DoVictory(string[] lines, Action onContinue)
        {
            victoryLines.text = lines != null ? string.Join("\n", lines) : "";
            onVictoryContinue = onContinue;
            victoryOpen = true;
            victoryReadyAt = Time.unscaledTime + 0.9f;
            victoryBox.style.display = DisplayStyle.Flex;
        }

        private void HandleBeat(int beat, int inBar)
        {
            if (!built || beatDots == null) return;
            for (int i = 0; i < beatDots.Length; i++)
            {
                bool on = i == inBar;
                float s = on ? (i == 0 ? 30f : 24f) : 20f;
                var d = beatDots[i];
                d.style.width = s;
                d.style.height = s;
                Round(d, s * 0.5f);
                d.style.backgroundColor = on ? (i == 0 ? HivePalette.Pink : HivePalette.Gold) : A(Color.white, 0.2f);
            }
        }

        private void Update()
        {
            if (!built) return;
            float dt = Time.unscaledDeltaTime;

            if (bannerT > 0f)
            {
                bannerT -= dt;
                float el = bannerDur - bannerT;
                float a = Mathf.Clamp01(Mathf.Min(el, bannerT) / 0.3f);
                bannerBox.style.opacity = a;
                if (bannerT <= 0f) bannerBox.style.display = DisplayStyle.None;
            }

            if (toastT > 0f)
            {
                toastT -= dt;
                float el = toastDur - toastT;
                float a = Mathf.Clamp01(Mathf.Min(el, toastT) / 0.2f);
                toastBox.style.opacity = a;
                if (toastT <= 0f) toastBox.style.display = DisplayStyle.None;
            }

            if (flashT > 0f)
            {
                flashT -= dt;
                float k = Mathf.Clamp01(flashT / flashDur);
                flashEl.style.backgroundColor = A(flashColor, flashAlpha * k);
            }

            if (bossBox.style.display == DisplayStyle.Flex)
            {
                bossFill.style.width = Length.Percent(bossTarget);
                bossGhostPct = Mathf.MoveTowards(bossGhostPct, bossTarget, 28f * dt);
                bossGhost.style.width = Length.Percent(bossGhostPct);
            }

            if (victoryOpen)
            {
                float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
                victoryHint.style.opacity = blink;
                if (Time.unscaledTime >= victoryReadyAt && HiveInput.ConfirmPressed())
                {
                    victoryOpen = false;
                    victoryBox.style.display = DisplayStyle.None;
                    var cb = onVictoryContinue;
                    onVictoryContinue = null;
                    if (cb != null) cb();
                }
            }
        }
    }
}
