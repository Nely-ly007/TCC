using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// POP ADVENTURE - HUDUIToolkit
/// Controla a HUD via UI Toolkit.
///
/// Setup:
/// 1. Crie um GameObject "HUD_UI" em cada cena de fase e no Hub
/// 2. Add Component: UIDocument
///    - Source Asset: HUD.uxml
///    - Style Sheets: HUD.uss
///    - Sort Order: 1 (acima do jogo, abaixo de painéis)
/// 3. Add Component: HUDUIToolkit (este script)
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HUDUIToolkit : MonoBehaviour
{
    public static HUDUIToolkit Instance { get; private set; }

    // Elementos da UI
    private VisualElement healthBarFill;
    private Label         healthText;
    private Label         vinylCount;
    private List<VisualElement> fragmentSlots = new();
    private List<VisualElement> beatDots      = new();

    // Estado
    private int   currentBeat     = 0;
    private int   lastActiveDot   = -1;
    private Coroutine dotResetCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Busca elementos
        healthBarFill = root.Q<VisualElement>("health-bar-fill");
        healthText    = root.Q<Label>("health-text");
        vinylCount    = root.Q<Label>("vinyl-count");

        // Fragmentos
        fragmentSlots.Clear();
        for (int i = 0; i < 4; i++)
        {
            var slot = root.Q<VisualElement>($"fragment-{i}");
            if (slot != null) fragmentSlots.Add(slot);
        }

        // Beat dots
        beatDots.Clear();
        for (int i = 0; i < 4; i++)
        {
            var dot = root.Q<VisualElement>($"beat-dot-{i}");
            if (dot != null) beatDots.Add(dot);
        }

        // Subscreve eventos
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnHealthChanged += UpdateHealth;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged  += UpdateVinyls;
            GameManager.Instance.OnFragmentCollected  += UpdateFragments;
        }

        RhythmManager.OnBeatNumberStatic += OnBeat;

        // Estado inicial
        UpdateHealth(
            PlayerController.Instance != null ? PlayerController.Instance.TotalMaxHP : 100,
            PlayerController.Instance != null ? PlayerController.Instance.TotalMaxHP : 100);

        UpdateVinyls(GameManager.Instance?.GetVinyls() ?? 0);
        UpdateFragments(GameManager.Instance?.FragmentsCollected ?? 0);
    }

    void OnDisable()
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnHealthChanged -= UpdateHealth;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged  -= UpdateVinyls;
            GameManager.Instance.OnFragmentCollected  -= UpdateFragments;
        }

        RhythmManager.OnBeatNumberStatic -= OnBeat;
    }

    // ── VIDA ─────────────────────────────────────────────────────
    public void UpdateHealth(int current, int max)
    {
        if (healthBarFill == null) return;

        float pct = max > 0 ? (float)current / max : 0f;

        // Largura da barra
        healthBarFill.style.width = Length.Percent(pct * 100f);

        // Texto
        if (healthText != null)
            healthText.text = $"{current}/{max}";

        // Cor da barra conforme HP
        healthBarFill.RemoveFromClassList("health-bar-fill--mid");
        healthBarFill.RemoveFromClassList("health-bar-fill--low");

        if (pct <= 0.25f)
            healthBarFill.AddToClassList("health-bar-fill--low");
        else if (pct <= 0.5f)
            healthBarFill.AddToClassList("health-bar-fill--mid");
    }

    // ── VINIS ─────────────────────────────────────────────────────
    public void UpdateVinyls(int amount)
    {
        if (vinylCount != null)
            vinylCount.text = amount.ToString();
    }

    // ── FRAGMENTOS ───────────────────────────────────────────────
    public void UpdateFragments(int collected)
    {
        for (int i = 0; i < fragmentSlots.Count; i++)
        {
            if (i < collected)
            {
                if (!fragmentSlots[i].ClassListContains("fragment-slot--collected"))
                {
                    fragmentSlots[i].AddToClassList("fragment-slot--collected");
                    // Animação de bounce ao coletar
                    StartCoroutine(FragmentCollectBounce(fragmentSlots[i]));
                }
            }
            else
            {
                fragmentSlots[i].RemoveFromClassList("fragment-slot--collected");
            }
        }
    }

    // ── BEAT INDICATOR ────────────────────────────────────────────
    private void OnBeat(int beatNumber)
    {
        currentBeat = beatNumber;

        // Reseta dot anterior
        if (lastActiveDot >= 0 && lastActiveDot < beatDots.Count)
        {
            beatDots[lastActiveDot].RemoveFromClassList("beat-dot--active");
            beatDots[lastActiveDot].RemoveFromClassList("beat-dot--downbeat");
        }

        // Ativa dot atual
        if (beatNumber < beatDots.Count)
        {
            if (beatNumber == 0)
                beatDots[beatNumber].AddToClassList("beat-dot--downbeat");
            else
                beatDots[beatNumber].AddToClassList("beat-dot--active");

            lastActiveDot = beatNumber;
        }
    }

    // ── ANIMAÇÕES ─────────────────────────────────────────────────
    private IEnumerator FragmentCollectBounce(VisualElement slot)
    {
        // Scale up
        slot.style.scale = new Scale(new Vector2(1.4f, 1.4f));
        yield return new WaitForSeconds(0.15f);
        // Volta ao normal (o CSS transition cuida da animação suave)
        slot.style.scale = StyleKeyword.Null;
    }

    // ── API PÚBLICA ───────────────────────────────────────────────
    /// <summary>Mostra ou esconde a HUD inteira (ex: durante cutscenes).</summary>
    public void SetVisible(bool visible)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
