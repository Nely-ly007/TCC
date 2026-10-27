using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Marca projéteis/ameaças temporárias para poderem ser limpas (reset da luta, morte do boss).</summary>
    public class HiveHostile : MonoBehaviour
    {
        private static readonly List<HiveHostile> all = new List<HiveHostile>();

        private void OnEnable() { all.Add(this); }
        private void OnDisable() { all.Remove(this); }

        public static void ClearAll()
        {
            HiveHostile[] copy = all.ToArray();
            for (int i = 0; i < copy.Length; i++)
                if (copy[i] != null) Destroy(copy[i].gameObject);
        }
    }
}
