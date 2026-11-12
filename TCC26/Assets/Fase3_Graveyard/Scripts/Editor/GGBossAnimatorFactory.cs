using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>
    /// Cria o AnimatorController do boss (parâmetros + estados + transições) com clips VAZIOS prontos para você preencher
    /// na janela Animation. Se o controller já existe, NÃO é recriado (para não apagar o seu trabalho).
    /// </summary>
    public static class GGBossAnimatorFactory
    {
        public const string ControllerPath = GGPaths.Animation + "/BossAnimator.controller";
        const string ClipsFolder = GGPaths.Animation + "/BossClips";

        static readonly string[] TriggerNames =
        {
            "Intro", "AttackDash", "AttackHands", "AttackSpin", "AttackOrbs", "AttackSummon", "Hurt", "PhaseChange", "Die"
        };

        public static AnimatorController GetOrCreate()
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            GGEditorUtil.EnsureFolder(GGPaths.Animation);
            GGEditorUtil.EnsureFolder(ClipsFolder);

            AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            c.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            for (int i = 0; i < TriggerNames.Length; i++)
                c.AddParameter(TriggerNames[i], AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = c.layers[0].stateMachine;

            AnimatorState idle = NewState(sm, "Idle", new Vector3(260f, 0f, 0f), true);
            AnimatorState walk = NewState(sm, "Walk", new Vector3(260f, 80f, 0f), true);
            sm.defaultState = idle;

            AnimatorStateTransition t = idle.AddTransition(walk);
            t.hasExitTime = false; t.duration = 0f;
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

            t = walk.AddTransition(idle);
            t.hasExitTime = false; t.duration = 0f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            string[,] actions =
            {
                { "Intro", "Intro" },
                { "AttackDash", "AttackDash" },
                { "AttackHands", "AttackHands" },
                { "AttackSpin", "AttackSpin" },
                { "AttackOrbs", "AttackOrbs" },
                { "AttackSummon", "AttackSummon" },
                { "Hurt", "Hurt" },
                { "PhaseChange", "PhaseChange" },
                { "Die", "Die" },
            };

            for (int i = 0; i < actions.GetLength(0); i++)
            {
                string stateName = actions[i, 0];
                string trigger = actions[i, 1];
                bool isDie = stateName == "Die";
                AnimatorState st = NewState(sm, stateName, new Vector3(560f, i * 60f - 120f, 0f), false);

                AnimatorStateTransition any = sm.AddAnyStateTransition(st);
                any.hasExitTime = false;
                any.duration = 0f;
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.If, 0f, trigger);

                if (!isDie)
                {
                    AnimatorStateTransition back = st.AddTransition(idle);
                    back.hasExitTime = true;
                    back.exitTime = 1f;
                    back.duration = 0.05f;
                    back.hasFixedDuration = true;
                }
            }

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        static AnimatorState NewState(AnimatorStateMachine sm, string name, Vector3 pos, bool loop)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = "Boss_" + name;
            clip.frameRate = 12f;
            string clipPath = ClipsFolder + "/Boss_" + name + ".anim";
            AssetDatabase.CreateAsset(clip, clipPath);

            AnimationClipSettings cs = AnimationUtility.GetAnimationClipSettings(clip);
            cs.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, cs);
            EditorUtility.SetDirty(clip);

            AnimatorState st = sm.AddState(name, pos);
            st.motion = clip;
            return st;
        }
    }
}
