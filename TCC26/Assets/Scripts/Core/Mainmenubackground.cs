using UnityEngine;

/// <summary>
/// POP ADVENTURE - MainMenuBackground
/// Cria e configura o fundo da MainMenu inteiramente via código:
///   1. Sprite de fundo cinza escuro
///   2. ParticleSystem de notas musicais flutuando
///   3. Disco decorativo girando
///
/// Adicione em um GameObject vazio chamado "Background" na cena MainMenu.
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
    [SerializeField] [Range(0f,1f)] private float noteAlpha = 0.35f;

    [Header("Disco Decorativo")]
    [SerializeField] private Sprite discSprite;
    [SerializeField] private float discRotateSpeed = 20f;
    [SerializeField] private Vector3 discPosition = new Vector3(4f, -1f, 1f);
    [SerializeField] private float discScale = 3f;
    [SerializeField] [Range(0f,1f)] private float discAlpha = 0.08f;

    private GameObject bgObject;
    private GameObject particleObject;
    private GameObject discObject;
    private ParticleSystem ps;

    void Start()
    {
        CreateBackground();
        CreateNoteParticles();
        if (discSprite != null) CreateDecorativeDisc();
    }

    void Update()
    {
        if (discObject != null)
            discObject.transform.Rotate(Vector3.forward, discRotateSpeed * Time.deltaTime);
    }

    private void CreateBackground()
    {
        bgObject = new GameObject("BG_Solid");
        bgObject.transform.SetParent(transform);
        bgObject.transform.localPosition = new Vector3(0f, 0f, 2f);

        SpriteRenderer sr = bgObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite(backgroundColor);
        sr.sortingOrder = -10;
        bgObject.transform.localScale = new Vector3(backgroundSize.x, backgroundSize.y, 1f);
    }

    private void CreateNoteParticles()
    {
        particleObject = new GameObject("MusicNoteParticles");
        particleObject.transform.SetParent(transform);
        particleObject.transform.localPosition = new Vector3(0f, -6f, 0f);

        ps = particleObject.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(
            -30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.25f), new Color(1f, 1f, 1f, 0.47f));
        main.gravityModifier = 0f;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = spawnWidth;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        AnimationCurve upCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(1f, 1.5f));
        vel.y = new ParticleSystem.MinMaxCurve(1f, upCurve);
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,       0f),
                new GradientAlphaKey(noteAlpha, 0.25f),
                new GradientAlphaKey(noteAlpha, 0.75f),
                new GradientAlphaKey(0f,       1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve shrink = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0.6f));
        size.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(
            -15f * Mathf.Deg2Rad, 15f * Mathf.Deg2Rad);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = -5;

        if (musicNoteSprite != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = musicNoteSprite.texture;
            rend.material = mat;
        }
        else
        {
            rend.material = new Material(Shader.Find("Particles/Standard Unlit"));
            Debug.LogWarning("[MainMenuBackground] Atribua um sprite de nota no Inspector!");
        }

        ps.Play();
    }

    private void CreateDecorativeDisc()
    {
        discObject = new GameObject("DiscDecoration");
        discObject.transform.SetParent(transform);
        discObject.transform.localPosition = discPosition;
        discObject.transform.localScale = Vector3.one * discScale;

        SpriteRenderer sr = discObject.AddComponent<SpriteRenderer>();
        sr.sprite = discSprite;
        sr.color = new Color(1f, 1f, 1f, discAlpha);
        sr.sortingOrder = -8;
    }

    private Sprite CreateSolidSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void OnEnable()  => RhythmManager.OnBeatStatic += OnBeat;
    void OnDisable() => RhythmManager.OnBeatStatic -= OnBeat;

    private void OnBeat()
    {
        if (ps != null && ps.isPlaying) ps.Emit(2);
    }
}