using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// POP ADVENTURE - BossHPUIToolkit v4
/// Fix: phaseDot2 escondido por padrão; só aparece quando ActivatePhase2() é chamado (boss a 50% HP).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class BossHPUIToolkit : MonoBehaviour
{
    public static BossHPUIToolkit Instance { get; private set; }

    [Header("Dados do Boss")]
    [SerializeField] private string bossDisplayName = "DONNA";

    private UIDocument    uiDoc;
    private VisualElement root;
    private VisualElement barFill;
    private Label         hpText;
    private Label         bossNameLabel;
    private VisualElement phaseIndicator;
    private VisualElement phaseDot2;
    private Label         phaseLabel;

    private int  maxHP;
    private bool isPhase2 = false;
    private bool rootReady = false;
    private Coroutine damageFlashCoroutine;

    // Guarda chamadas que chegaram antes do root estar pronto
    private bool  pendingShow   = false;
    private int   pendingHP     = 0;
    private int   pendingMax    = 0;

    public bool IsVisible { get; private set; } = false;

    // ── INIT ─────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        uiDoc = GetComponent<UIDocument>();
    }

    void Start()
    {
        StartCoroutine(WaitForRoot());
    }

    private IEnumerator WaitForRoot()
    {
        while (uiDoc.rootVisualElement == null ||
               uiDoc.rootVisualElement.childCount == 0)
        {
            yield return null;
        }

        root = uiDoc.rootVisualElement;

        barFill        = root.Q<VisualElement>("boss-bar-fill");
        hpText         = root.Q<Label>("boss-hp-text");
        bossNameLabel  = root.Q<Label>("boss-name");
        phaseIndicator = root.Q<VisualElement>("phase-indicator");
        phaseDot2      = root.Q<VisualElement>("phase-dot-2");
        phaseLabel     = root.Q<Label>("phase-label");

        if (bossNameLabel != null)
            bossNameLabel.text = bossDisplayName;

        // FIX: esconde o segundo ponto — só aparece ao entrar na Fase 2
        if (phaseDot2 != null)
            phaseDot2.style.display = DisplayStyle.None;

        // Esconde label de fase também
        if (phaseLabel != null)
            phaseLabel.style.display = DisplayStyle.None;

        rootReady = true;

        // Esconde HUD por padrão
        root.style.display = DisplayStyle.None;

        // Aplica Show pendente
        if (pendingShow)
        {
            pendingShow = false;
            Show(pendingHP, pendingMax);
        }

        Debug.Log($"[BossHPUIToolkit] Root pronto. " +
                  $"barFill={barFill != null}, hpText={hpText != null}, " +
                  $"bossName={bossNameLabel != null}");
    }

    // ── SHOW / HIDE ───────────────────────────────────────────────
    public void Show(int currentHP, int max)
    {
        if (!rootReady)
        {
            pendingShow = true;
            pendingHP   = currentHP;
            pendingMax  = max;
            return;
        }

        maxHP     = max;
        IsVisible = true;

        root.style.display = DisplayStyle.Flex;

        if (phaseIndicator != null)
            phaseIndicator.style.display = DisplayStyle.Flex;

        UpdateHP(currentHP, max);
        StartCoroutine(EntranceAnimation());
    }

    public void Hide()
    {
        IsVisible = false;
        if (!rootReady || root == null) return;
        root.style.display = DisplayStyle.None;
    }

    // ── ATUALIZAR HP ─────────────────────────────────────────────
    public void UpdateHP(int current, int max)
    {
        if (!rootReady || barFill == null) return;

        maxHP = max;
        float pct = max > 0 ? (float)current / max : 0f;

        barFill.style.width = Length.Percent(pct * 100f);

        if (hpText != null)
            hpText.text = $"{current} / {max}";

        if (pct > 0.5f)
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.20f, 0.31f));
        else if (pct > 0.25f)
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.55f, 0.10f));
        else
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.10f, 0.10f));

        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlash());
    }

    // ── FASE 2 ────────────────────────────────────────────────────
    public void ActivatePhase2()
    {
        if (isPhase2 || !rootReady) return;
        isPhase2 = true;

        // Revela o segundo ponto
        if (phaseDot2 != null)
        {
            phaseDot2.style.display = DisplayStyle.Flex;
            phaseDot2.style.backgroundColor = new StyleColor(new Color(1f, 0.31f, 0.31f));
        }

        if (phaseLabel != null)
            phaseLabel.style.display = DisplayStyle.Flex;

        if (bossNameLabel != null)
            bossNameLabel.style.color = new StyleColor(new Color(1f, 0.31f, 0.31f));

        StartCoroutine(Phase2Flash());
    }

    // ── ANIMAÇÕES ─────────────────────────────────────────────────
    private IEnumerator EntranceAnimation()
    {
        if (barFill == null) yield break;

        barFill.style.width = Length.Percent(0f);
        yield return new WaitForSeconds(0.1f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            barFill.style.width = Length.Percent(Mathf.SmoothStep(0f, 1f, t / 0.6f) * 100f);
            yield return null;
        }
        barFill.style.width = Length.Percent(100f);
    }

    private IEnumerator DamageFlash()
    {
        if (barFill == null) yield break;

        var saved = barFill.style.backgroundColor;
        barFill.style.backgroundColor = new StyleColor(Color.white);
        yield return new WaitForSeconds(0.06f);
        barFill.style.backgroundColor = saved;
    }

    private IEnumerator Phase2Flash()
    {
        if (barFill == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            barFill.style.backgroundColor = new StyleColor(Color.white);
            yield return new WaitForSeconds(0.1f);
            barFill.style.backgroundColor = new StyleColor(new Color(1f, 0.31f, 0.31f));
            yield return new WaitForSeconds(0.1f);
        }
    }
}
