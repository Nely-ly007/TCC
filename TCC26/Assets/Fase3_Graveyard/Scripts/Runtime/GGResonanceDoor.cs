using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Abre o portão quando TODOS os alvos de ressonância estão acesos ao mesmo tempo.</summary>
    public class GGResonanceDoor : MonoBehaviour
    {
        [SerializeField] GGResonanceTarget[] targets;
        [SerializeField] GGGate gate;
        [Tooltip("Depois de aberta, a porta não fecha mais.")]
        [SerializeField] bool latch = true;

        bool opened;

        public void Setup(GGResonanceTarget[] newTargets, GGGate newGate)
        {
            targets = newTargets;
            gate = newGate;
        }

        void Update()
        {
            if (gate == null || targets == null || targets.Length == 0) return;
            if (opened && latch) return;

            bool all = true;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || !targets[i].IsLit) { all = false; break; }
            }

            if (all != opened)
            {
                opened = all;
                gate.SetClosed(!all);
                if (all) GGCameraFollow.Shake(0.2f, 0.4f);
            }
        }
    }
}
