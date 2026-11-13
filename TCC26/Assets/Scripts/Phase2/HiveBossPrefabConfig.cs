
using UnityEngine;

namespace PopAdventure.Hive
{
    [DefaultExecutionOrder(-100)]
    public class HiveBossPrefabConfig : MonoBehaviour
    {
        public static HiveBossPrefabConfig Instance { get; private set; }

        [Header("Prefab da Rainha")]
        [Tooltip("Prefab visual completo da Rainha Abelha.")]
        public GameObject bossPrefab;

        [Tooltip("Escala aplicada ao prefab.")]
        public Vector3 bossPrefabScale = Vector3.one;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
