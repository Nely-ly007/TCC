using UnityEngine;

/// <summary>
/// POP ADVENTURE - MainMenuBackground v4
/// </summary>
public class MainMenuBackground : MonoBehaviour
{
    [Header("Fundo")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
    [SerializeField] private Vector2 backgroundSize = new Vector2(20f, 12f);

    [Header("Notas Musicais (Partículas)")]
    [SerializeField] private Sprite musicNoteSprite;
    [SerializeField] private int emissionRate = 8;
    [SerializeField] private float spawnWidth = 12f;
    [SerializeField] [Range(0f, 1f)] private float noteAlpha = 0.35f;

    [Header("Disco Decorativo")]
    [SerializeField] private Sprite discSprite;
    [SerializeField] private float discRotateSpeed = 20f;
    [SerializeField] private Vector3 discPosition = new Vector3(4f, -1f, 1f);
    [SerializeField] private float discScale = 3f;
    [SerializeField] [Range(0f, 1f)] private float discAlpha = 0.08f;

    private GameObject discObject;
    private ParticleSystem ps;

    void Start()
    {
        // Destrói filhos antigos antes de criar novos
        // (evita duplicatas ao dar Stop + Play no Editor)
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // Aguarda um frame para garantir que os destroys processaram
        StartCoroutine(InitDelayed());
    }

    private System.Collections.IEnumerator InitDelayed()
    {
        yield return null; // espera 1 frame

        CreateBackground();
        CreateNoteParticles();
        if (discSprite != null) CreateDecorativeDisc();
    }

    void Update()
    {
        if (discObject != null)
            discObject.transform.Rotate(Vector3.forward, discRotateSpeed * Time.deltaTime);
    }

    void OnEnable()  => RhythmManager.OnBeatStatic += OnBeat;
    void OnDisable() => RhythmManager.OnBeatStatic -= OnBeat;

    private void OnBeat()
    {
        // Checa null antes de acessar — evita MissingReferenceException
        if (ps != null && ps.gameObject != null && ps.isPlaying)
            ps.Emit(2);
    }

    private void CreateBackground()
    {
        GameObject bg = new GameObject("BG_Solid");
        bg.transform.SetParent(transform);
        bg.transform.localPosition = new Vector3(0f, 0f, 2f);

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite(backgroundColor);
        sr.sortingOrder = -10;
        bg.transform.localScale = new Vector3(backgroundSize.x, backgroundSize.y, 1f);
    }

    private void CreateNoteParticles()
    {
        GameObject go = new GameObject("MusicNoteParticles");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, -6f, 0f);

        ps = go.AddComponent<ParticleSystem>();

        // Para o sistema antes de configurar (evita erro de "still playing")
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop            = true;
        main.duration        = 5f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startRotation   = new ParticleSystem.MinMaxCurve(
                                   -30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                   new Color(1f, 1f, 1f, 0.25f),
                                   new Color(1f, 1f, 1f, 0.47f));
        main.gravityModifier = 0f;
        main.maxParticles    = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius    = spawnWidth;

        // Todos os eixos no mesmo modo (MinMaxCurve com dois floats)
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.8f,  1.5f);
        vel.z       = new ParticleSystem.MinMaxCurve(0f,    0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g  = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,        0f),
                new GradientAlphaKey(noteAlpha, 0.2f),
                new GradientAlphaKey(noteAlpha, 0.8f),
                new GradientAlphaKey(0f,        1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.6f));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z       = new ParticleSystem.MinMaxCurve(
                          -15f * Mathf.Deg2Rad,
                           15f * Mathf.Deg2Rad);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode   = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = -5;

// URP: usa Particles/Standard Unlit em vez de Sprites/Default
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
        {
            // fallback se o shader não encontrar
            mat = new Material(Shader.Find("Sprites/Default"));
        }

        if (musicNoteSprite != null)
            mat.mainTexture = musicNoteSprite.texture;

        rend.material = mat;

        ps.Play();
    }

    private void CreateDecorativeDisc()
    {
        discObject = new GameObject("DiscDecoration");
        discObject.transform.SetParent(transform);
        discObject.transform.localPosition = discPosition;
        discObject.transform.localScale    = Vector3.one * discScale;

        SpriteRenderer sr = discObject.AddComponent<SpriteRenderer>();
        sr.sprite       = discSprite;
        sr.color        = new Color(1f, 1f, 1f, discAlpha);
        sr.sortingOrder = -8;
    }

    private Sprite CreateSolidSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}