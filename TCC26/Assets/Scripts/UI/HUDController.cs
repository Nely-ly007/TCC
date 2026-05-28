using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// POP ADVENTURE - HUDController
/// HUD minimalista: barra de vida estilizada + contador de vinis + pulso de beat.
/// Inspirado no estilo de Gris e Inside (discreto, imersivo).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Barra de Vida")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private Gradient healthGradient; // verde → amarelo → vermelho
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private CanvasGroup hudGroup;

    [Header("Contador de Vinis")]
    [SerializeField] private TextMeshProUGUI vinylCountText;
    [SerializeField] private Image vinylIcon;

    [Header("Fragmentos do Disco")]
    [SerializeField] private Image[] fragmentSlots; // 4 slots

    [Header("Beat Pulse (UI)")]
    [SerializeField] private Image beatIndicator;
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseSpeed = 8f;

    [Header("Indicador de Microfone (vida extra)")]
    [SerializeField] private Image microphoneIndicator;

    // Animações de UI
    private Vector3 vinylOriginalScale;
    private Vector3 beatOriginalScale;

    void Start()
    {
        // Subscreve eventos do player
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnHealthChanged += UpdateHealthBar;

        // Subscreve GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged += UpdateVinylCount;
            GameManager.Instance.OnFragmentCollected += UpdateFragments;
        }

        // Beat pulse
        RhythmManager.OnBeatStatic += OnBeat;

        if (vinylIcon != null) vinylOriginalScale = vinylIcon.transform.localScale;
        if (beatIndicator != null) beatOriginalScale = beatIndicator.transform.localScale;

        // Estado inicial
        UpdateVinylCount(GameManager.Instance?.GetVinyls() ?? 0);
        UpdateFragments(GameManager.Instance?.FragmentsCollected ?? 0);
    }

    void OnDestroy()
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnHealthChanged -= UpdateHealthBar;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged -= UpdateVinylCount;
            GameManager.Instance.OnFragmentCollected -= UpdateFragments;
        }
        RhythmManager.OnBeatStatic -= OnBeat;
    }

    // ── BARRA DE VIDA ────────────────────────────────────────────
    private void UpdateHealthBar(int current, int max)
    {
        float normalizedHP = (float)current / max;

        if (healthSlider != null)
            healthSlider.value = normalizedHP;

        if (healthFill != null && healthGradient != null)
            healthFill.color = healthGradient.Evaluate(normalizedHP);

        if (healthText != null)
            healthText.text = $"{current}/{max}";

        // Pulsa a barra de vida quando HP está baixo
        if (normalizedHP <= 0.25f)
            StartCoroutine(LowHPPulse());
    }

    private IEnumerator LowHPPulse()
    {
        if (healthFill == null) yield break;
        Color original = healthFill.color;
        healthFill.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        healthFill.color = original;
    }

    // ── VINIS ────────────────────────────────────────────────────
    private void UpdateVinylCount(int count)
    {
        if (vinylCountText != null)
            vinylCountText.text = count.ToString();

        // Animação bounce no ícone
        if (vinylIcon != null)
            StartCoroutine(ScaleBounce(vinylIcon.transform, vinylOriginalScale));
    }

    // ── FRAGMENTOS ───────────────────────────────────────────────
    private void UpdateFragments(int count)
    {
        for (int i = 0; i < fragmentSlots.Length; i++)
        {
            if (fragmentSlots[i] == null) continue;
            // Slot ativado = fragmento coletado
            fragmentSlots[i].color = i < count ?
                new Color(1f, 0.85f, 0f) :    // dourado = coletado
                new Color(0.3f, 0.3f, 0.3f);  // cinza = pendente
        }
    }

    // ── BEAT PULSE ───────────────────────────────────────────────
    private void OnBeat()
    {
        if (beatIndicator != null)
            StartCoroutine(ScaleBounce(beatIndicator.transform, beatOriginalScale));
    }

    private IEnumerator ScaleBounce(Transform target, Vector3 originalScale)
    {
        float t = 0;
        Vector3 bigScale = originalScale * pulseScale;

        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float lerp = 1f - (t / 0.15f);
            target.localScale = Vector3.Lerp(originalScale, bigScale, lerp);
            yield return null;
        }
        target.localScale = originalScale;
    }

    // ── DANO FLASH (tela) ────────────────────────────────────────
    public void ShowDamageFlash()
    {
        StartCoroutine(DamageScreenFlash());
    }

    private IEnumerator DamageScreenFlash()
    {
        if (hudGroup == null) yield break;
        hudGroup.alpha = 0.7f;
        yield return new WaitForSeconds(0.08f);
        hudGroup.alpha = 1f;
    }

    // ── MICROFONE ────────────────────────────────────────────────
    public void SetMicrophoneActive(bool active)
    {
        if (microphoneIndicator != null)
            microphoneIndicator.enabled = active;
    }
}
