using UnityEngine;

/// <summary>
/// POP ADVENTURE - RhythmicPlatform
/// Plataforma sincronizada com o BPM.
/// Pode aparecer/desaparecer ou mover-se em batidas específicas.
/// </summary>
public class RhythmicPlatform : MonoBehaviour
{
    public enum PlatformMode
    {
        AppearOnBeat1,       // Aparece no beat 1, some no beat 3
        AppearOnBeat3,       // Aparece no beat 3, some no beat 1
        MoveHorizontal,      // Vai e vem horizontalmente a cada beat
        MoveVertical,        // Vai e vem verticalmente a cada beat
        FadeInOut,           // Fade alpha no ritmo
    }

    [Header("Modo")]
    [SerializeField] private PlatformMode mode = PlatformMode.AppearOnBeat1;

    [Header("Movimento")]
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveDuration = 0.4f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Antecipação Visual")]
    [SerializeField] private bool showWarning = true;  // pisca antes de aparecer

    private Vector3 startPosition;
    private bool isActive = true;
    private bool movingForward = true;
    private float moveTimer = 0f;
    private Vector3 targetPosition;

    void Awake()
    {
        startPosition = transform.position;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (platformCollider == null) platformCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        RhythmManager.OnBeatNumberStatic += HandleBeat;
    }

    void OnDisable()
    {
        RhythmManager.OnBeatNumberStatic -= HandleBeat;
    }

    private void HandleBeat(int beatNumber)
    {
        switch (mode)
        {
            case PlatformMode.AppearOnBeat1:
                SetActive(beatNumber == 0);
                break;

            case PlatformMode.AppearOnBeat3:
                SetActive(beatNumber == 2);
                break;

            case PlatformMode.MoveHorizontal:
                StartHorizontalMove();
                break;

            case PlatformMode.MoveVertical:
                StartVerticalMove();
                break;

            case PlatformMode.FadeInOut:
                ToggleFade();
                break;
        }
    }

    private void SetActive(bool active)
    {
        isActive = active;
        platformCollider.enabled = active;
        spriteRenderer.color = active ? activeColor : inactiveColor;
    }

    private void StartHorizontalMove()
    {
        movingForward = !movingForward;
        targetPosition = startPosition + (movingForward ?
            Vector3.right * moveDistance : Vector3.left * moveDistance);
        moveTimer = 0f;
    }

    private void StartVerticalMove()
    {
        movingForward = !movingForward;
        targetPosition = startPosition + (movingForward ?
            Vector3.up * moveDistance : Vector3.down * moveDistance);
        moveTimer = 0f;
    }

    private void ToggleFade()
    {
        isActive = !isActive;
        platformCollider.enabled = isActive;
        // O fade acontece no Update
    }

    void Update()
    {
        if (mode == PlatformMode.MoveHorizontal || mode == PlatformMode.MoveVertical)
        {
            moveTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, moveTimer / moveDuration);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        }

        if (mode == PlatformMode.FadeInOut)
        {
            Color target = isActive ? activeColor : inactiveColor;
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, target, Time.deltaTime * 8f);
        }
    }
}
