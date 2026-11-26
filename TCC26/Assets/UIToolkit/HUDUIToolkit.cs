
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using PopAdventure.Hive;
using PopAdventure.Fase3;

/// <summary>
/// POP ADVENTURE - HUDUIToolkit
/// HUD de vida, vinis, fragmentos, microfone e indicadores de batida.
///
/// Relógios rítmicos:
/// Fase 1: RhythmManager
/// Fase 2: HiveBeatDriver
/// Fase 3: GGBeatClock
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HUDUIToolkit : MonoBehaviour
{
    public static HUDUIToolkit Instance { get; private set; }

    [Header("Sprites")]
    [SerializeField] private Sprite microphoneSprite;

    // =========================================================
    // ELEMENTOS DO HUD
    // =========================================================

    private VisualElement healthBarFill;
    private Label healthText;
    private Label vinylCount;

    private VisualElement microphoneRow;
    private VisualElement microphoneIcon;

    private List<VisualElement> fragmentSlots = new();
    private List<VisualElement> beatDots = new();

    // =========================================================
    // ESTADO
    // =========================================================

    private int lastActiveDot = -1;
    private bool microphoneActive = false;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // ON ENABLE
    // =========================================================

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // -----------------------------------------------------
        // REFERÊNCIAS DO HUD
        // -----------------------------------------------------

        healthBarFill = root.Q<VisualElement>("health-bar-fill");
        healthText = root.Q<Label>("health-text");
        vinylCount = root.Q<Label>("vinyl-count");

        microphoneRow = root.Q<VisualElement>("microphone-row");
        microphoneIcon = root.Q<VisualElement>("microphone-icon");

        // -----------------------------------------------------
        // FRAGMENTOS
        // -----------------------------------------------------

        fragmentSlots.Clear();

        for (int i = 0; i < 4; i++)
        {
            var slot = root.Q<VisualElement>($"fragment-{i}");

            if (slot != null)
                fragmentSlots.Add(slot);
        }

        // -----------------------------------------------------
        // BEAT DOTS
        // -----------------------------------------------------

        beatDots.Clear();

        for (int i = 0; i < 4; i++)
        {
            var dot = root.Q<VisualElement>($"beat-dot-{i}");

            if (dot != null)
                beatDots.Add(dot);
        }

        lastActiveDot = -1;

        // -----------------------------------------------------
        // MICROFONE - SPRITE
        // -----------------------------------------------------

        if (microphoneSprite != null && microphoneIcon != null)
        {
            microphoneIcon.style.backgroundImage =
                new StyleBackground(microphoneSprite);
        }

        // -----------------------------------------------------
        // EVENTOS DO PLAYER
        // -----------------------------------------------------

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHealthChanged += UpdateHealth;
            PlayerController.Instance.OnPlayerRevived += OnMicrophoneUsed;
        }

        // -----------------------------------------------------
        // EVENTOS DO GAMEMANAGER
        // -----------------------------------------------------

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged += UpdateVinyls;
            GameManager.Instance.OnFragmentCollected += UpdateFragments;
        }

        // =====================================================
        // RELÓGIOS RÍTMICOS
        // =====================================================

        // FASE 1
        RhythmManager.OnBeatNumberStatic += OnRhythmBeat;

        // FASE 2 - THE HIVE
        HiveBeatDriver.OnBeat += OnHiveBeat;

        // FASE 3
        GGBeatClock.OnBeat += OnGGBeat;

        // -----------------------------------------------------
        // ESTADO INICIAL DO HUD
        // -----------------------------------------------------

        UpdateHealth(
            PlayerController.Instance?.TotalMaxHP ?? 100,
            PlayerController.Instance?.TotalMaxHP ?? 100
        );

        UpdateVinyls(GameManager.Instance?.GetVinyls() ?? 0);

        UpdateFragments(GameManager.Instance?.FragmentsCollected ?? 0);

        SetMicrophoneVisible(false);
    }

    // =========================================================
    // ON DISABLE
    // =========================================================

    private void OnDisable()
    {
        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHealthChanged -= UpdateHealth;
            PlayerController.Instance.OnPlayerRevived -= OnMicrophoneUsed;
        }

        // -----------------------------------------------------
        // GAMEMANAGER
        // -----------------------------------------------------

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged -= UpdateVinyls;
            GameManager.Instance.OnFragmentCollected -= UpdateFragments;
        }

        // -----------------------------------------------------
        // RELÓGIOS RÍTMICOS
        // -----------------------------------------------------

        RhythmManager.OnBeatNumberStatic -= OnRhythmBeat;

        HiveBeatDriver.OnBeat -= OnHiveBeat;

        GGBeatClock.OnBeat -= OnGGBeat;
    }

    // =========================================================
    // VIDA
    // =========================================================

    public void UpdateHealth(int current, int max)
    {
        if (healthBarFill == null)
            return;

        float pct = max > 0 ? (float)current / max : 0f;

        healthBarFill.style.width = Length.Percent(pct * 100f);

        if (healthText != null)
            healthText.text = $"{current}/{max}";

        healthBarFill.RemoveFromClassList("health-bar-fill--mid");
        healthBarFill.RemoveFromClassList("health-bar-fill--low");

        if (pct <= 0.25f)
        {
            healthBarFill.AddToClassList("health-bar-fill--low");
        }
        else if (pct <= 0.50f)
        {
            healthBarFill.AddToClassList("health-bar-fill--mid");
        }
    }

    // =========================================================
    // VINIS
    // =========================================================

    public void UpdateVinyls(int amount)
    {
        if (vinylCount != null)
            vinylCount.text = amount.ToString();
    }

    // =========================================================
    // FRAGMENTOS
    // =========================================================

    public void UpdateFragments(int collected)
    {
        for (int i = 0; i < fragmentSlots.Count; i++)
        {
            if (i < collected)
            {
                if (!fragmentSlots[i].ClassListContains(
                    "fragment-slot--collected"))
                {
                    fragmentSlots[i].AddToClassList(
                        "fragment-slot--collected");

                    StartCoroutine(FragmentBounce(fragmentSlots[i]));
                }
            }
            else
            {
                fragmentSlots[i].RemoveFromClassList(
                    "fragment-slot--collected");
            }
        }
    }

    // =========================================================
    // MICROFONE - VIDA EXTRA
    // =========================================================

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
    /// Chamado automaticamente quando a vida extra é utilizada.
    /// </summary>
    private void OnMicrophoneUsed()
    {
        microphoneActive = false;

        SetMicrophoneVisible(false);
    }

    private void SetMicrophoneVisible(bool visible)
    {
        if (microphoneRow == null)
            return;

        if (visible)
        {
            microphoneRow.AddToClassList("microphone-row--visible");
        }
        else
        {
            microphoneRow.RemoveFromClassList("microphone-row--visible");
        }
    }

    private IEnumerator MicrophonePulse()
    {
        if (microphoneRow == null)
            yield break;

        // Pulsa 3 vezes ao aparecer
        for (int i = 0; i < 3; i++)
        {
            microphoneRow.AddToClassList("microphone-row--pulse");

            yield return new WaitForSeconds(0.15f);

            microphoneRow.RemoveFromClassList("microphone-row--pulse");

            yield return new WaitForSeconds(0.15f);
        }
    }

    // =========================================================
    // BEAT - RHYTHMMANAGER
    // FASE 1 / FALLBACK
    // =========================================================

    private void OnRhythmBeat(int beatNumber)
    {
        // A Fase 2 tem prioridade quando seu relógio estiver ativo.
        if (HiveBeatDriver.Running)
            return;

        // A Fase 3 tem prioridade quando seu relógio estiver ativo.
        if (GGBeatClock.IsRunning)
            return;

        UpdateBeatDot(beatNumber);
    }

    // =========================================================
    // BEAT - HIVEBEATDRIVER
    // FASE 2 - THE HIVE
    // =========================================================

    private void OnHiveBeat(int totalBeat, int beatInBar)
    {
        // Se o relógio da Fase 3 estiver ativo, não interfere.
        if (GGBeatClock.IsRunning)
            return;

        // beatInBar: 0, 1, 2, 3
        UpdateBeatDot(beatInBar);
    }

    // =========================================================
    // BEAT - GGBEATCLOCK
    // FASE 3
    // =========================================================

    private void OnGGBeat(int absoluteBeat)
    {
        if (beatDots == null || beatDots.Count == 0)
            return;

        // GGBeatClock envia batidas absolutas:
        // 0, 1, 2, 3, 4, 5...
        //
        // Converte para o índice dos 4 dots:
        // 0, 1, 2, 3, 0, 1, 2, 3...

        int beatInBar = absoluteBeat % beatDots.Count;

        UpdateBeatDot(beatInBar);
    }

    // =========================================================
    // ATUALIZAÇÃO VISUAL DOS BEAT DOTS
    // =========================================================

    private void UpdateBeatDot(int beatNumber)
    {
        if (beatDots == null || beatDots.Count == 0)
            return;

        // Remove destaque do dot anterior
        if (lastActiveDot >= 0 && lastActiveDot < beatDots.Count)
        {
            beatDots[lastActiveDot].RemoveFromClassList(
                "beat-dot--active");

            beatDots[lastActiveDot].RemoveFromClassList(
                "beat-dot--downbeat");
        }

        // Proteção contra índice inválido
        if (beatNumber < 0 || beatNumber >= beatDots.Count)
            return;

        // Beat 1 do compasso = downbeat
        if (beatNumber == 0)
        {
            beatDots[beatNumber].AddToClassList(
                "beat-dot--downbeat");
        }
        else
        {
            beatDots[beatNumber].AddToClassList(
                "beat-dot--active");
        }

        lastActiveDot = beatNumber;
    }

    // =========================================================
    // ANIMAÇÃO DOS FRAGMENTOS
    // =========================================================

    private IEnumerator FragmentBounce(VisualElement slot)
    {
        slot.style.scale = new Scale(new Vector2(1.4f, 1.4f));

        yield return new WaitForSeconds(0.15f);

        slot.style.scale = StyleKeyword.Null;
    }

    // =========================================================
    // API - VISIBILIDADE DO HUD
    // =========================================================

    public void SetVisible(bool visible)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        root.style.display = visible
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    // =========================================================
    // DESTRUIÇÃO
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
