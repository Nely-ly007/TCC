using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// POP ADVENTURE - BossHPUIToolkit v2
/// Fix: root pode estar null quando Show() é chamado antes de OnEnable().
/// Adicionado InitRoot() chamado em Awake + OnEnable para garantir referências.
///
/// Setup:
/// 1. Crie um GameObject "BossHPUI" na cena da fase
/// 2. Add Component: UIDocument → Source: BossHPPanel.uxml
/// 3. Add Component: BossHPUIToolkit
/// 4. Vincule no Inspector do BossDonna (campo bossHPUI)
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class BossHPUIToolkit : MonoBehaviour
{
    public static BossHPUIToolkit Instance { get; private set; }

    [Header("Dados do Boss")]
    [SerializeField] private string bossDisplayName = "DONNA";

    private UIDocument  uiDoc;
    private VisualElement root;
    private VisualElement barFill;
    private Label         hpText;
    private Label         bossNameLabel;
    private VisualElement phaseIndicator;
    private VisualElement phaseDot2;
    private Label         phaseLabel;

    private int  maxHP;
    private bool isPhase2 = false;
    private Coroutine damageFlashCoroutine;

    public bool IsVisible { get; private set; } = false;

    // ── INIT ─────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        uiDoc = GetComponent<UIDocument>();
        InitRoot(); // tenta inicializar já no Awake
    }

    void OnEnable()
    {
        InitRoot(); // garante referências se Awake não conseguiu
    }

    /// <summary>
    /// Busca e guarda todos os elementos visuais do UXML.
    /// Seguro chamar múltiplas vezes.
    /// </summary>
    private void InitRoot()
    {
        if (uiDoc == null) uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;

        var r = uiDoc.rootVisualElement;
        if (r == null) return;

        root = r;

        barFill        = root.Q<VisualElement>("boss-bar-fill");
        hpText         = root.Q<Label>("boss-hp-text");
        bossNameLabel  = root.Q<Label>("boss-name");
        phaseIndicator = root.Q<VisualElement>("phase-indicator");
        phaseDot2      = root.Q<VisualElement>("phase-dot-2");
        phaseLabel     = root.Q<Label>("phase-label");

        if (bossNameLabel != null)
            bossNameLabel.text = bossDisplayName;

        // Começa escondido
        root.style.display = DisplayStyle.None;
    }

    // ── SHOW / HIDE ───────────────────────────────────────────────
    public void Show(int currentHP, int max)
    {
        // Garante que root está inicializado (pode ser chamado antes de OnEnable)
        if (root == null) InitRoot();
        if (root == null)
        {
            Debug.LogWarning("[BossHPUIToolkit] Show() chamado mas root ainda é null. " +
                             "Verifique se o UIDocument tem o UXML vinculado.");
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
        if (root == null) return;
        root.style.display = DisplayStyle.None;
    }

    // ── ATUALIZAR HP ─────────────────────────────────────────────
    public void UpdateHP(int current, int max)
    {
        if (root == null) InitRoot();
        if (barFill == null) return;

        maxHP = max;
        float pct = max > 0 ? (float)current / max : 0f;

        barFill.style.width = Length.Percent(pct * 100f);

        if (hpText != null)
            hpText.text = $"{current} / {max}";

        // Cor da barra por % de vida
        if (pct > 0.5f)
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.20f, 0.31f));
        else if (pct > 0.25f)
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.55f, 0.10f));
        else
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.10f, 0.10f));

        // Flash da barra ao tomar dano
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlash());
    }

    // ── FASE 2 ────────────────────────────────────────────────────
    public void ActivatePhase2()
    {
        if (isPhase2) return;
        isPhase2 = true;

        if (phaseDot2 != null)
            phaseDot2.style.backgroundColor = new StyleColor(new Color(1f, 0.31f, 0.31f));

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