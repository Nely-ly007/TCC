using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - RhythmActivatable
/// Elemento de cenário que é ativado pelo ataque do player (Ressonância).
/// Ex: acende luzes disco na fase 1, destrava portas.
/// </summary>
public class RhythmActivatable : MonoBehaviour
{
    public enum ActivationType { Light, Door, Platform, Secret }

    [Header("Tipo")]
    [SerializeField] private ActivationType type = ActivationType.Light;
    [SerializeField] private bool requiresRhythmHit = true; // requer hit no beat?
    [SerializeField] private float rhythmTolerance = 0.15f;

    [Header("Estado")]
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool isPermanent = false; // permanece ativo ou volta ao normal?

    [Header("Visual")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private ParticleSystem activationEffect;

    [Header("Porta (se tipo = Door)")]
    [SerializeField] private Collider2D doorCollider;

    [Header("Áudio")]
    [SerializeField] private AudioClip activateSFX;
    [SerializeField] private AudioClip deactivateSFX;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    /// <summary>
    /// Chamado quando o projétil musical atinge este objeto.
    /// </summary>
    public void OnHitByMusicNote()
    {
        if (requiresRhythmHit)
        {
            // Verifica se o hit foi no ritmo
            bool onBeat = RhythmManager.Instance?.IsOnBeat(rhythmTolerance) ?? true;
            if (!onBeat)
            {
                // Hit fora do ritmo: feedback negativo
                StartCoroutine(WrongBeatFlash());
                return;
            }
        }

        Activate();
    }

    private void Activate()
    {
        isActivated = !isActivated; // Toggle

        if (!isPermanent && !isActivated)
        {
            Deactivate();
            return;
        }

        isActivated = true;
        UpdateVisual();
        ApplyEffect();

        if (activateSFX != null) audioSource?.PlayOneShot(activateSFX);
        if (activationEffect != null) activationEffect.Play();
    }

    private void Deactivate()
    {
        isActivated = false;
        UpdateVisual();
        ReverseEffect();
        if (deactivateSFX != null) audioSource?.PlayOneShot(deactivateSFX);
    }

    private void ApplyEffect()
    {
        switch (type)
        {
            case ActivationType.Door:
                if (doorCollider != null) doorCollider.enabled = false; // abre
                break;
            case ActivationType.Platform:
                GetComponent<Collider2D>().enabled = true;
                break;
        }
    }

    private void ReverseEffect()
    {
        switch (type)
        {
            case ActivationType.Door:
                if (doorCollider != null) doorCollider.enabled = true;
                break;
            case ActivationType.Platform:
                GetComponent<Collider2D>().enabled = false;
                break;
        }
    }

    private void UpdateVisual()
    {
        if (targetRenderer != null)
            targetRenderer.color = isActivated ? activeColor : inactiveColor;
    }

    private IEnumerator WrongBeatFlash()
    {
        if (targetRenderer == null) yield break;
        Color original = targetRenderer.color;
        targetRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        targetRenderer.color = original;
    }

    // Este componente precisa de um Collider2D com IsTrigger=true
    void OnTriggerEnter2D(Collider2D other)
    {
        MusicProjectile proj = other.GetComponent<MusicProjectile>();
        if (proj != null)
            OnHitByMusicNote();
    }
}

/// <summary>
/// POP ADVENTURE - DiscoLight
/// Luz piscante sincronizada com o beat (feedback visual da Fase 1).
/// </summary>
public class DiscoLight : MonoBehaviour
{
    [SerializeField] private Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D light2D;
    [SerializeField] private float pulseIntensity = 2f;
    [SerializeField] private float baseIntensity = 0.5f;

    private int colorIndex;

    void OnEnable() => RhythmManager.OnBeatStatic += OnBeat;
    void OnDisable() => RhythmManager.OnBeatStatic -= OnBeat;

    private void OnBeat()
    {
        if (light2D == null) return;

        // Muda cor no beat
        colorIndex = (colorIndex + 1) % colors.Length;
        light2D.color = colors[colorIndex];

        StopAllCoroutines();
        StartCoroutine(PulseLight());
    }

    private IEnumerator PulseLight()
    {
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float intensity = Mathf.Lerp(pulseIntensity, baseIntensity, t / 0.3f);
            if (light2D != null) light2D.intensity = intensity;
            yield return null;
        }
        if (light2D != null) light2D.intensity = baseIntensity;
    }
}
