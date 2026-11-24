#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// POP ADVENTURE — Queen Bee Animation Builder
///
/// COMO USAR:
/// 1. Coloque este arquivo em qualquer pasta Editor/ do seu projeto Unity
///    (ex: Assets/Editor/QueenBeeAnimationBuilder.cs)
/// 2. No menu superior do Unity: Tools > Queen Bee > Build All Animations
/// 3. Os clips serão criados em Assets/Animations/QueenBee/
/// 4. Arraste os clips para o Animator Controller da Queen Bee
///
/// NOMES DOS CHILD GAMEOBJECTS esperados no prefab:
///   Wings_Back, Wings_Front, LeftLeg, RightLeg, Torso,
///   LeftArmUpper, LeftForearm, RightArmUpper, RightForearm,
///   Head, Hair
/// </summary>
public static class QueenBeeAnimationBuilder
{
    private const float FPS = 12f;
    private const string OUTPUT_PATH = "Assets/Animations/QueenBee";

    // ── Menu entry ────────────────────────────────────────────────────────
    [MenuItem("Tools/Queen Bee/Build All Animations")]
    public static void BuildAll()
    {
        EnsureFolder(OUTPUT_PATH);

        BuildIdle();
        BuildHover();
        BuildAttackStomp();
        BuildAttackBee();
        BuildAttackDive();
        BuildHurt();
        BuildDeath();
        BuildIntro();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[QueenBeeAnimationBuilder] ✅ Todas as animações criadas em " + OUTPUT_PATH);
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════

    static float T(int frame) => frame / FPS;

    static AnimationClip NewClip(string name, bool loop)
    {
        var clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = FPS;
        if (loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
        return clip;
    }

    static void SaveClip(AnimationClip clip)
    {
        string path = OUTPUT_PATH + "/" + clip.name + ".anim";
        // Sobrescreve se já existir
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    // Cria uma curva com keyframes simples (linear entre pontos)
    static AnimationCurve Curve(params (int frame, float value)[] keys)
    {
        var curve = new AnimationCurve();
        foreach (var k in keys)
            curve.AddKey(new Keyframe(T(k.frame), k.value));
        // Suaviza tangentes
        for (int i = 0; i < curve.length; i++)
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        return curve;
    }

    // Adiciona curva ao clip
    static void Set(AnimationClip clip, string path, string prop, AnimationCurve curve, string type = "UnityEngine.Transform")
    {
        clip.SetCurve(path, System.Type.GetType(type + ", UnityEngine"), prop, curve);
    }

    // Atalho para rotação Z (a mais usada)
    static void RotZ(AnimationClip clip, string childPath, params (int frame, float deg)[] keys)
    {
        Set(clip, childPath, "localEulerAngles.z", Curve(keys));
    }

    // Atalho para posição Y da raiz (string vazia = objeto raiz)
    static void PosY(AnimationClip clip, string childPath, params (int frame, float val)[] keys)
    {
        Set(clip, childPath, "localPosition.y", Curve(keys));
    }

    static void PosX(AnimationClip clip, string childPath, params (int frame, float val)[] keys)
    {
        Set(clip, childPath, "localPosition.x", Curve(keys));
    }

    static void ScaleY(AnimationClip clip, string childPath, params (int frame, float val)[] keys)
    {
        Set(clip, childPath, "localScale.y", Curve(keys));
    }

    static void ScaleX(AnimationClip clip, string childPath, params (int frame, float val)[] keys)
    {
        Set(clip, childPath, "localScale.x", Curve(keys));
    }

    // Adiciona Animation Event (método sem parâmetros)
    
    static void AddEvent(AnimationClip clip, int frame, string method)
    {
        var existing = AnimationUtility.GetAnimationEvents(clip);

        var list = new System.Collections.Generic.List<AnimationEvent>(existing);

        var ev = new AnimationEvent();
        ev.time = T(frame);
        ev.functionName = method;

        list.Add(ev);

        AnimationUtility.SetAnimationEvents(clip, list.ToArray());
    }


    // Versão mais simples para adicionar evento
    static void Evt(AnimationClip clip, int frame, string method)
    {
        var existing = AnimationUtility.GetAnimationEvents(clip);
        var list = new System.Collections.Generic.List<AnimationEvent>(existing);
        var ev = new AnimationEvent { time = T(frame), functionName = method };
        list.Add(ev);
        AnimationUtility.SetAnimationEvents(clip, list.ToArray());
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Idle — 48 frames, loop
    // ══════════════════════════════════════════════════════════════════════
    static void BuildIdle()
    {
        var clip = NewClip("QB_Idle", loop: true);

        // Torso sobe/desce (respiração)
        PosY(clip, "Torso",
            (0, 0f), (12, 0.05f), (24, 0f), (36, -0.03f), (48, 0f));

        // Asas batem suavemente
        RotZ(clip, "Wings_Back",
            (0, 0f), (12, -8f), (24, 0f), (36, 8f), (48, 0f));
        RotZ(clip, "Wings_Front",
            (0, 0f), (12, -8f), (24, 0f), (36, 8f), (48, 0f));

        // Cabeça balanço suave
        RotZ(clip, "Torso/Head",
            (0, 0f), (14, -2f), (28, 0f), (42, 2f), (48, 0f));

        // Cabelo balança com delay
        RotZ(clip, "Torso/Head/Hair",
            (0, 0f), (16, -3f), (30, 0f), (44, 3f), (48, 0f));

        SaveClip(clip);
        Debug.Log("[QB] QB_Idle criado");
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Hover — 24 frames, loop (usado durante movimento lateral)
    // ══════════════════════════════════════════════════════════════════════
    static void BuildHover()
    {
        var clip = NewClip("QB_Hover", loop: true);

        // Bob vertical do corpo inteiro
        PosY(clip, "",
            (0, 0f), (6, 0.08f), (12, 0f), (18, -0.04f), (24, 0f));

        // Asas batem mais forte
        RotZ(clip, "Wings_Back",
            (0, 0f), (6, -15f), (12, 0f), (18, 12f), (24, 0f));
        RotZ(clip, "Wings_Front",
            (0, 0f), (6, -15f), (12, 0f), (18, 12f), (24, 0f));

        // Pernas inclinam levemente para trás
        RotZ(clip, "RightLeg",
            (0, 0f), (6, 5f), (12, 0f), (18, -3f), (24, 0f));
        RotZ(clip, "LeftLeg",
            (0, 0f), (6, -5f), (12, 0f), (18, 3f), (24, 0f));

        SaveClip(clip);
        Debug.Log("[QB] QB_Hover criado");
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Attack_Stomp — 30 frames, sem loop
    // ══════════════════════════════════════════════════════════════════════
    static void BuildAttackStomp()
    {
        var clip = NewClip("QB_Attack_Stomp", loop: false);

        // Raiz sobe e desce
        PosY(clip, "",
            (0, 0f), (6, 1.5f), (12, 1.5f), (18, 0f), (22, -0.1f), (30, 0f));

        // Pernas dobram no ar e esticam no impacto
        RotZ(clip, "RightLeg",
            (0, 0f), (6, 10f), (12, 45f), (18, 10f), (22, 0f), (30, 0f));
        RotZ(clip, "LeftLeg",
            (0, 0f), (6, -10f), (12, -45f), (18, -10f), (22, 0f), (30, 0f));

        // Asas se abrem no pulo
        RotZ(clip, "Wings_Back",
            (0, 0f), (6, -25f), (12, -20f), (18, -10f), (22, 5f), (30, 0f));
        RotZ(clip, "Wings_Front",
            (0, 0f), (6, 25f), (12, 20f), (18, 10f), (22, -5f), (30, 0f));

        // Torso comprime no impacto
        ScaleY(clip, "Torso",
            (0, 1f), (20, 1f), (22, 0.85f), (26, 1.05f), (30, 1f));

        // Braços sobem no pulo
        RotZ(clip, "Torso/LeftArmUpper",
            (0, 0f), (8, -30f), (14, -20f), (22, 0f), (30, 0f));
        RotZ(clip, "Torso/RightArmUpper",
            (0, 0f), (8, 30f), (14, 20f), (22, 0f), (30, 0f));

        // Animation Event no impacto
        Evt(clip, 22, "OnStompLand");

        SaveClip(clip);
        Debug.Log("[QB] QB_Attack_Stomp criado");
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Attack_Bee — 36 frames, sem loop
    // ══════════════════════════════════════════════════════════════════════
    static void BuildAttackBee()
    {
        var clip = NewClip("QB_Attack_Bee", loop: false);

        // Braços sobem
        RotZ(clip, "Torso/LeftArmUpper",
            (0, 0f), (8, -60f), (14, -65f), (18, -60f), (28, -20f), (36, 0f));
        RotZ(clip, "Torso/RightArmUpper",
            (0, 0f), (8, 60f), (14, 65f), (18, 60f), (28, 20f), (36, 0f));

        // Antebraços curvam levemente
        RotZ(clip, "Torso/LeftArmUpper/LeftForearm",
            (0, 0f), (8, -30f), (18, -25f), (36, 0f));
        RotZ(clip, "Torso/RightArmUpper/RightForearm",
            (0, 0f), (8, 30f), (18, 25f), (36, 0f));

        // Cabeça olha para cima
        RotZ(clip, "Torso/Head",
            (0, 0f), (10, -15f), (18, -18f), (28, -5f), (36, 0f));

        // Asas pulsam no momento do spawn
        ScaleX(clip, "Wings_Back",
            (0, 1f), (16, 1f), (18, 1.1f), (20, 0.95f), (22, 1f), (36, 1f));

        // Torso leve recuo para trás ao erguer os braços
        PosY(clip, "Torso",
            (0, 0f), (10, 0.05f), (20, 0.03f), (36, 0f));

        Evt(clip, 18, "OnSpawnBees");

        SaveClip(clip);
        Debug.Log("[QB] QB_Attack_Bee criado");
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Attack_Dive — 20 frames, sem loop
    // ══════════════════════════════════════════════════════════════════════
    static void BuildAttackDive()
    {
        var clip = NewClip("QB_Attack_Dive", loop: false);

        // Inclina o personagem inteiro para frente (diagonal)
        RotZ(clip, "",
            (0, 0f), (4, -30f), (8, -32f), (16, -10f), (20, 0f));

        // Pernas esticam para trás
        RotZ(clip, "RightLeg",
            (0, 0f), (6, 25f), (8, 25f), (16, 10f), (20, 0f));
        RotZ(clip, "LeftLeg",
            (0, 0f), (6, 20f), (8, 20f), (16, 8f), (20, 0f));

        // Braços recuam (aerodinâmico)
        RotZ(clip, "Torso/LeftArmUpper",
            (0, 0f), (6, 40f), (8, 42f), (16, 15f), (20, 0f));
        RotZ(clip, "Torso/RightArmUpper",
            (0, 0f), (6, -40f), (8, -42f), (16, -15f), (20, 0f));

        // Asas spread no mergulho
        RotZ(clip, "Wings_Back",
            (0, 0f), (4, -30f), (8, -32f), (16, -10f), (20, 0f));
        RotZ(clip, "Wings_Front",
            (0, 0f), (4, 30f), (8, 32f), (16, 10f), (20, 0f));

        Evt(clip, 8, "OnDiveStart");

        SaveClip(clip);
        Debug.Log("[QB] QB_Attack_Dive criado");
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Hurt — 12 frames, sem loop
    // ══════════════════════════════════════════════════════════════════════
    static void BuildHurt()
    {
        var clip = NewClip("QB_Hurt", loop: false);

        // Raiz recua
        PosX(clip, "",
            (0, 0f), (2, -0.3f), (4, -0.5f), (8, -0.1f), (12, 0f));

        // Cabeça joga para trás
        RotZ(clip, "Torso/Head",
            (0, 0f), (2, 20f), (5, 15f), (10, 5f), (12, 0f));

        // Asas se abrem abruptamente
        RotZ(clip, "Wings_Back",
            (0, 0f), (2, -20f), (6, -10f), (12, 0f));
        RotZ(clip, "Wings_Front",
            (0, 0f), (2, 20f), (6, 10f), (12, 0f));

        // Compressão horizontal leve
        ScaleX(clip, "Torso",
            (0, 1f), (3, 0.9f), (7, 1.0f), (12, 1f));

        // Cabelo balança no impacto
        RotZ(clip, "Torso/Head/Hair",
            (0, 0f), (3, 15f), (8, 5f), (12, 0f));

        Evt(clip, 12, "OnHurtAnimationEnd");

        SaveClip(clip);
        Debug.Log("[QB] QB_Hurt criado");
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Death — 60 frames, sem loop
    // ══════════════════════════════════════════════════════════════════════
    static void BuildDeath()
    {
        var clip = NewClip("QB_Death", loop: false);

        // Corpo cai girando
        RotZ(clip, "",
            (0, 0f), (8, -15f), (16, -40f), (24, -70f), (32, -90f), (60, -90f));

        // Desce até o chão (ajuste o valor -2f conforme a posição do boss na cena)
        PosY(clip, "",
            (0, 0f), (8, 0f), (20, -0.5f), (32, -2f), (60, -2f));

        // Asas param e caem junto
        RotZ(clip, "Wings_Back",
            (0, 0f), (8, -5f), (60, -5f));
        RotZ(clip, "Wings_Front",
            (0, 0f), (8, 5f), (60, 5f));

        // Braços pendurados
        RotZ(clip, "Torso/LeftArmUpper",
            (0, 0f), (12, -20f), (24, -50f), (60, -50f));
        RotZ(clip, "Torso/RightArmUpper",
            (0, 0f), (12, 20f), (24, 50f), (60, 50f));

        // Pernas afrouxam
        RotZ(clip, "LeftLeg",
            (0, 0f), (10, -10f), (20, -20f), (60, -25f));
        RotZ(clip, "RightLeg",
            (0, 0f), (10, 15f), (20, 30f), (60, 35f));

        // Fade out de todos os SpriteRenderers
        // Unity anima color via "m_Color.a" em SpriteRenderer
        string[] parts = {
            "Wings_Back", "Wings_Front", "LeftLeg", "RightLeg",
            "Torso", "Torso/LeftArmUpper", "Torso/LeftArmUpper/LeftForearm",
            "Torso/RightArmUpper", "Torso/RightArmUpper/RightForearm",
            "Torso/Head", "Torso/Head/Hair"
        };
        foreach (var part in parts)
        {
            clip.SetCurve(part, typeof(UnityEngine.SpriteRenderer), "m_Color.a",
                Curve((40, 1f), (60, 0f)));
        }

        Evt(clip, 60, "OnDeathAnimationEnd");

        SaveClip(clip);
        Debug.Log("[QB] QB_Death criado");
    }

    // ══════════════════════════════════════════════════════════════════════
    // QB_Intro — 48 frames, sem loop
    // ══════════════════════════════════════════════════════════════════════
    static void BuildIntro()
    {
        var clip = NewClip("QB_Intro", loop: false);

        // Desce de cima da tela
        PosY(clip, "",
            (0, 6f), (12, 3f), (24, 0.5f), (30, 0f), (48, 0f));

        // Asas batem rápido na entrada
        RotZ(clip, "Wings_Back",
            (0, -20f), (3, 20f), (6, -20f), (9, 20f), (12, -20f),
            (15, 20f), (18, -20f), (21, 20f), (24, -20f), (27, 20f),
            (30, 0f),   // pousa
            (36, -15f), (42, -25f), (48, -20f)); // pose heroica
        RotZ(clip, "Wings_Front",
            (0, 20f), (3, -20f), (6, 20f), (9, -20f), (12, 20f),
            (15, -20f), (18, 20f), (21, -20f), (24, 20f), (27, -20f),
            (30, 0f),
            (36, 15f), (42, 25f), (48, 20f));

        // Pouso: torso comprime e volta
        ScaleY(clip, "Torso",
            (0, 1f), (29, 1f), (30, 0.9f), (33, 1.05f), (36, 1f), (48, 1f));

        // Braços abrem em pose heroica
        RotZ(clip, "Torso/LeftArmUpper",
            (0, 0f), (30, 0f), (36, -30f), (42, -45f), (48, -40f));
        RotZ(clip, "Torso/RightArmUpper",
            (0, 0f), (30, 0f), (36, 30f), (42, 45f), (48, 40f));

        // Cabeça ergue na pose
        RotZ(clip, "Torso/Head",
            (0, 0f), (38, 0f), (42, -10f), (48, -8f));

        // Cabelo flui na descida
        RotZ(clip, "Torso/Head/Hair",
            (0, 10f), (15, 15f), (30, 5f), (42, 0f), (48, 0f));

        Evt(clip, 48, "OnIntroEnd");

        SaveClip(clip);
        Debug.Log("[QB] QB_Intro criado");
    }
}
#endif
