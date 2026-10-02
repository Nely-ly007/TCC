using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// POP ADVENTURE - BossHPUIToolkit
/// Barra de HP do boss em UI Toolkit.
///
/// Setup:
/// 1. Crie um GameObject "BossHPUI" na cena da fase
/// 2. Add Component: UIDocument → Source: BossHPPanel.uxml
/// 3. Add Component: BossHPUIToolkit
/// 4. No BossDonna.cs, substitua as referências uGUI por este script:
///    BossHPUIToolkit.Instance?.Show()
///    BossHPUIToolkit.Instance?.UpdateHP(current, max)
///    BossHPUIToolkit.Instance?.Hide()
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class BossHPUIToolkit : MonoBehaviour
{
    public static BossHPUIToolkit Instance { get; private set; }

    [Header("Dados do Boss")]
    [SerializeField] private string bossDisplayName = "DONNA";

    // Elementos
    private VisualElement root;
    private VisualElement barFill;
    private Label         hpText;
    private Label         bossNameLabel;
    private VisualElement phaseIndicator;
    private VisualElement phaseDot2;
    private Label         phaseLabel;

    // Estado
    private int   maxHP;
    private bool  isPhase2 = false;
    private Coroutine damageFlashCoroutine;

    public bool IsVisible { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        root           = GetComponent<UIDocument>().rootVisualElement;
        barFill        = root.Q<VisualElement>("boss-bar-fill");
        hpText         = root.Q<Label>("boss-hp-text");
        bossNameLabel  = root.Q<Label>("boss-name");
        phaseIndicator = root.Q<VisualElement>("phase-indicator");
        phaseDot2      = root.Q<VisualElement>("phase-dot-2");
        phaseLabel     = root.Q<Label>("phase-label");

        if (bossNameLabel != null)
            bossNameLabel.text = bossDisplayName;

        Hide();
    }

    // ── SHOW / HIDE ───────────────────────────────────────────────
    public void Show(int currentHP, int max)
    {
        if (root == null) root = GetComponent<UIDocument>().rootVisualElement;
        maxHP = max;
        IsVisible = true;
        root.style.display = DisplayStyle.Flex;

        // Mostra o indicador de fase
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
        if (barFill == null) return;

        maxHP = max;
        float pct = max > 0 ? (float)current / max : 0f;

        // Largura da barra com transição suave
        barFill.style.width = Length.Percent(pct * 100f);

        // Texto
        if (hpText != null)
            hpText.text = $"{current} / {max}";

        // Cor da barra conforme HP
        if (pct > 0.5f)
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.20f, 0.31f)); // vermelho
        else if (pct > 0.25f)
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.55f, 0.10f)); // laranja
        else
            barFill.style.backgroundColor = new StyleColor(new Color(0.86f, 0.10f, 0.10f)); // vermelho escuro

        // Flash de dano
        if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlash());
    }

    // ── FASE 2 ───────────────────────────────────────────────────
    public void ActivatePhase2()
    {
        if (isPhase2) return;
        isPhase2 = true;

        // Acende o segundo ponto do indicador de fase
        if (phaseDot2 != null)
            phaseDot2.style.backgroundColor =
                new StyleColor(new Color(1f, 0.31f, 0.31f)); // vermelho

        // Mostra label "FASE 2"
        if (phaseLabel != null)
            phaseLabel.style.display = DisplayStyle.Flex;

        // Nome do boss fica vermelho
        if (bossNameLabel != null)
            bossNameLabel.style.color = new StyleColor(new Color(1f, 0.31f, 0.31f));

        StartCoroutine(Phase2Flash());
    }

    // ── ANIMAÇÕES ─────────────────────────────────────────────────
    private IEnumerator EntranceAnimation()
    {
        // Barra aparece da esquerda
        if (barFill == null) yield break;
        barFill.style.width = Length.Percent(0f);
        yield return new WaitForSeconds(0.1f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0f, 1f, t / 0.6f) * 100f;
            barFill.style.width = Length.Percent(pct);
            yield return null;
        }
        barFill.style.width = Length.Percent(100f);
    }

    private IEnumerator DamageFlash()
    {
        if (barFill == null) yield break;

        // Flash branco rápido
        var originalColor = barFill.style.backgroundColor;
        barFill.style.backgroundColor = new StyleColor(Color.white);
        yield return new WaitForSeconds(0.06f);
        barFill.style.backgroundColor = originalColor;
    }

    private IEnumerator Phase2Flash()
    {
        if (root == null) yield break;

        // Pisca a barra 3 vezes em vermelho
        for (int i = 0; i < 3; i++)
        {
            if (barFill != null)
                barFill.style.backgroundColor = new StyleColor(Color.white);
            yield return new WaitForSeconds(0.1f);
            if (barFill != null)
                barFill.style.backgroundColor =
                    new StyleColor(new Color(1f, 0.31f, 0.31f));
            yield return new WaitForSeconds(0.1f);
        }
    }
}
