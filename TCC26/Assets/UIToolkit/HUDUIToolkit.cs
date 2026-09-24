using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// POP ADVENTURE - HUDUIToolkit v2
/// Adicionado: ícone de microfone (vida extra)
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HUDUIToolkit : MonoBehaviour
{
    public static HUDUIToolkit Instance { get; private set; }

    [Header("Sprites (arraste no Inspector)")]
    [SerializeField] private Sprite microphoneSprite;

    // Elementos
    private VisualElement   healthBarFill;
    private Label           healthText;
    private Label           vinylCount;
    private VisualElement   microphoneRow;
    private VisualElement   microphoneIcon;
    private List<VisualElement> fragmentSlots = new();
    private List<VisualElement> beatDots      = new();

    // Estado
    private int  lastActiveDot = -1;
    private bool microphoneActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        healthBarFill  = root.Q<VisualElement>("health-bar-fill");
        healthText     = root.Q<Label>("health-text");
        vinylCount     = root.Q<Label>("vinyl-count");
        microphoneRow  = root.Q<VisualElement>("microphone-row");
        microphoneIcon = root.Q<VisualElement>("microphone-icon");

        fragmentSlots.Clear();
        for (int i = 0; i < 4; i++)
        {
            var slot = root.Q<VisualElement>($"fragment-{i}");
            if (slot != null) fragmentSlots.Add(slot);
        }

        beatDots.Clear();
        for (int i = 0; i < 4; i++)
        {
            var dot = root.Q<VisualElement>($"beat-dot-{i}");
            if (dot != null) beatDots.Add(dot);
        }

        // Sprite do microfone via script
        if (microphoneSprite != null && microphoneIcon != null)
            microphoneIcon.style.backgroundImage = new StyleBackground(microphoneSprite);

        // Subscreve eventos
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHealthChanged   += UpdateHealth;
            PlayerController.Instance.OnPlayerRevived   += OnMicrophoneUsed;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged += UpdateVinyls;
            GameManager.Instance.OnFragmentCollected += UpdateFragments;
        }

        RhythmManager.OnBeatNumberStatic += OnBeat;

        // Estado inicial
        UpdateHealth(
            PlayerController.Instance?.TotalMaxHP ?? 100,
            PlayerController.Instance?.TotalMaxHP ?? 100);
        UpdateVinyls(GameManager.Instance?.GetVinyls() ?? 0);
        UpdateFragments(GameManager.Instance?.FragmentsCollected ?? 0);
        SetMicrophoneVisible(false);
    }

    void OnDisable()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHealthChanged -= UpdateHealth;
            PlayerController.Instance.OnPlayerRevived -= OnMicrophoneUsed;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged -= UpdateVinyls;
            GameManager.Instance.OnFragmentCollected -= UpdateFragments;
        }

        RhythmManager.OnBeatNumberStatic -= OnBeat;
    }

    // ── VIDA ─────────────────────────────────────────────────────
    public void UpdateHealth(int current, int max)
    {
        if (healthBarFill == null) return;

        float pct = max > 0 ? (float)current / max : 0f;
        healthBarFill.style.width = Length.Percent(pct * 100f);

        if (healthText != null)
            healthText.text = $"{current}/{max}";

        healthBarFill.RemoveFromClassList("health-bar-fill--mid");
        healthBarFill.RemoveFromClassList("health-bar-fill--low");

        if      (pct <= 0.25f) healthBarFill.AddToClassList("health-bar-fill--low");
        else if (pct <= 0.50f) healthBarFill.AddToClassList("health-bar-fill--mid");
    }

    // ── VINIS ────────────────────────────────────────────────────
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
                    StartCoroutine(FragmentBounce(fragmentSlots[i]));
                }
            }
            else
            {
                fragmentSlots[i].RemoveFromClassList("fragment-slot--collected");
            }
        }
    }

    // ── MICROFONE (VIDA EXTRA) ────────────────────────────────────
    /// <summary>
    /// Chame quando o CollectibleMicrophone for coletado.
    /// </summary>
    public void ShowMicrophone()
    {
        microphoneActive = true;
        SetMicrophoneVisible(true);
        StartCoroutine(MicrophonePulse());
    }

    /// <summary>
    /// Chamado automaticamente via OnPlayerRevived quando a vida extra é usada.
    /// </summary>
    private void OnMicrophoneUsed()
    {
        microphoneActive = false;
        SetMicrophoneVisible(false);
    }

    private void SetMicrophoneVisible(bool visible)
    {
        if (microphoneRow == null) return;
        if (visible)
            microphoneRow.AddToClassList("microphone-row--visible");
        else
            microphoneRow.RemoveFromClassList("microphone-row--visible");
    }

    private IEnumerator MicrophonePulse()
    {
        if (microphoneRow == null) yield break;
        // Pulsa 3 vezes ao aparecer
        for (int i = 0; i < 3; i++)
        {
            microphoneRow.AddToClassList("microphone-row--pulse");
            yield return new WaitForSeconds(0.15f);
            microphoneRow.RemoveFromClassList("microphone-row--pulse");
            yield return new WaitForSeconds(0.15f);
        }
    }

    // ── BEAT ─────────────────────────────────────────────────────
    private void OnBeat(int beatNumber)
    {
        if (lastActiveDot >= 0 && lastActiveDot < beatDots.Count)
        {
            beatDots[lastActiveDot].RemoveFromClassList("beat-dot--active");
            beatDots[lastActiveDot].RemoveFromClassList("beat-dot--downbeat");
        }

        if (beatNumber < beatDots.Count)
        {
            beatDots[beatNumber].AddToClassList(
                beatNumber == 0 ? "beat-dot--downbeat" : "beat-dot--active");
            lastActiveDot = beatNumber;
        }
    }

    // ── ANIMAÇÕES ─────────────────────────────────────────────────
    private IEnumerator FragmentBounce(VisualElement slot)
    {
        slot.style.scale = new Scale(new Vector2(1.4f, 1.4f));
        yield return new WaitForSeconds(0.15f);
        slot.style.scale = StyleKeyword.Null;
    }

    // ── API ───────────────────────────────────────────────────────
    public void SetVisible(bool visible)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
