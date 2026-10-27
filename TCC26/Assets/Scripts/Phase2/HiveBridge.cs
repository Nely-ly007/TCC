using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PopAdventure.Hive
{
    /// <summary>
    /// ÚNICO ponto de contato entre a Fase 2 e os scripts da Fase 1
    /// (PlayerController, GameManager, SceneController, CameraShake, MusicProjectile).
    /// Usa reflexão com listas de nomes comuns: o pacote compila mesmo que os nomes sejam outros.
    /// Se algo não for encontrado, aparece um aviso no Console dizendo qual lista ajustar abaixo.
    /// </summary>
    public static class HiveBridge
    {
        // ===================== AJUSTE AQUI se algum nome da Fase 1 for diferente =====================
        private const string PlayerTypeName = "PlayerController";
        private const string GameManagerTypeName = "GameManager";
        private const string SceneControllerTypeName = "SceneController";
        private const string CameraShakeTypeName = "CameraShake";
        private const string ProjectileTypeName = "MusicProjectile";

        private static readonly string[] PlayerDamageMethods = { "TakeDamage", "ReceiveDamage", "ApplyDamage", "Damage", "Hurt" };
        private static readonly string[] PlayerHealMethods = { "Heal", "AddHealth", "RestoreHealth", "RestoreHP", "AddHP" };
        private static readonly string[] VinylMethods = { "AddVinyls", "AddVinyl", "AddVinils", "AddVinil", "CollectVinyl", "AddCurrency" };
        private static readonly string[] FragmentMethods = { "CollectFragment", "AddFragment", "UnlockFragment", "CollectDiscFragment" };
        private static readonly string[] ExtraLifeMethods = { "AddExtraLife", "AddLife", "AddLives", "GainLife" };
        private static readonly string[] SceneLoadMethods = { "LoadScene", "LoadSceneAsync", "GoToScene", "LoadLevel" };
        private static readonly string[] AttackDamageMembers = { "AttackDamage", "attackDamage", "PlayerDamage", "NoteDamage", "CurrentDamage" };
        private static readonly string[] ProjectileDamageMembers = { "damage", "Damage", "attackDamage", "AttackDamage" };
        private static readonly string[] HealthMembers = { "CurrentHealth", "currentHealth", "Health", "health", "CurrentHP", "currentHP", "HP", "hp" };
        private static readonly string[] MaxHealthMembers = { "MaxHealth", "maxHealth", "MaxHP", "maxHP" };
        private static readonly string[] ReviveMethods = { "Revive", "Respawn", "ResetHealth", "FullHeal", "ResetPlayer" };
        // ============================================================================================

        private const BindingFlags All =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        private static readonly HashSet<string> warned = new HashSet<string>();
        /// <summary>Flash vermelho de tela ao tomar dano da Fase 2. Desligue se o seu Player já faz isso.</summary>
        public static bool HurtFlash = true;

        private static GameObject playerCache;
        private static float nextPlayerHit;

        // ---------------------------------------------------------------- Player

        public static GameObject Player
        {
            get
            {
                if (playerCache != null) return playerCache;
                try { playerCache = GameObject.FindWithTag("Player"); } catch (Exception) { playerCache = null; }
                if (playerCache == null)
                {
                    Type t = FindType(PlayerTypeName);
                    if (t != null && typeof(Component).IsAssignableFrom(t))
                    {
                        var c = UnityEngine.Object.FindFirstObjectByType(t) as Component;
                        if (c != null) playerCache = c.gameObject;
                    }
                }
                return playerCache;
            }
        }

        public static Rigidbody2D PlayerRb
        {
            get
            {
                var p = Player;
                if (p == null) return null;
                var rb = p.GetComponent<Rigidbody2D>();
                if (rb == null) rb = p.GetComponentInChildren<Rigidbody2D>();
                if (rb == null) rb = p.GetComponentInParent<Rigidbody2D>();
                return rb;
            }
        }

        public static bool IsPlayer(Collider2D c)
        {
            if (c == null) return false;
            if (c.CompareTag("Player")) return true;
            var p = Player;
            return p != null && c.transform.root == p.transform.root;
        }

        /// <summary>True se o collider pertence a um projétil de nota do jogador (MusicProjectile).</summary>
        public static bool TryGetPlayerProjectile(Collider2D c, out GameObject projectile)
        {
            projectile = null;
            if (c == null) return false;
            Type t = FindType(ProjectileTypeName);
            if (t != null && typeof(Component).IsAssignableFrom(t))
            {
                Component comp = c.GetComponentInParent(t);
                if (comp == null) return false;
                projectile = comp.gameObject;
                return true;
            }
            string n = c.name.ToLowerInvariant();
            if (n.Contains("projectile") || n.Contains("musicnote"))
            {
                projectile = c.gameObject;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- Ações

        /// <summary>Causa dano ao jogador (com i-frames globais curtos e knockback).</summary>
        public static bool DamagePlayer(int amount, Vector2 sourcePos, float knockback = 7f, bool ignoreIFrames = false)
        {
            var p = Player;
            if (p == null) return false;
            if (!ignoreIFrames && Time.time < nextPlayerHit) return false;
            nextPlayerHit = Time.time + 0.6f;

            bool applied = false;
            foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
            {
                if (TryInvoke(mb, PlayerDamageMethods, amount, sourcePos)) { applied = true; break; }
            }
            if (!applied)
            {
                foreach (var mb in p.GetComponentsInParent<MonoBehaviour>())
                {
                    if (TryInvoke(mb, PlayerDamageMethods, amount, sourcePos)) { applied = true; break; }
                }
            }
            if (!applied) WarnOnce("dano", "Nenhum método de dano encontrado no Player. Ajuste PlayerDamageMethods em HiveBridge.cs.");

            var rb = PlayerRb;
            if (rb != null && knockback > 0f)
            {
                Vector2 d = rb.position - sourcePos;
                d.y = 0f;
                d = d.sqrMagnitude < 0.01f ? Vector2.right : d.normalized;
                rb.AddForce(new Vector2(d.x * knockback, knockback * 0.6f), ForceMode2D.Impulse);
            }
            ShakeCamera(0.15f, 0.15f);
            if (HurtFlash) HiveOverlay.Flash(HivePalette.Danger, 0.28f, 0.25f);
            return applied;
        }

        public static void HealPlayer(int amount)
        {
            var p = Player;
            if (p == null) return;
            foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
            {
                if (TryInvoke(mb, PlayerHealMethods, amount)) return;
            }
            foreach (var mb in p.GetComponentsInParent<MonoBehaviour>())
            {
                if (TryInvoke(mb, PlayerHealMethods, amount)) return;
            }
            WarnOnce("cura", "Nenhum método de cura encontrado no Player. Ajuste PlayerHealMethods em HiveBridge.cs.");
        }

        public static void AddVinyls(int amount)
        {
            if (!TryInvoke(GetSingleton(GameManagerTypeName), VinylMethods, amount))
                WarnOnce("vinil", "Nenhum método de vinil no GameManager. Ajuste VinylMethods em HiveBridge.cs.");
        }

        public static void AddExtraLife()
        {
            if (!TryInvoke(GetSingleton(GameManagerTypeName), ExtraLifeMethods, 1))
            {
                var p = Player;
                if (p != null)
                {
                    foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
                        if (TryInvoke(mb, ExtraLifeMethods, 1)) return;
                }
                WarnOnce("vida", "Nenhum método de vida extra. Ajuste ExtraLifeMethods em HiveBridge.cs.");
            }
        }

        public static void CollectFragment(int phaseNumber)
        {
            if (!TryInvoke(GetSingleton(GameManagerTypeName), FragmentMethods, phaseNumber))
                WarnOnce("fragmento", "Nenhum método de fragmento no GameManager. Ajuste FragmentMethods em HiveBridge.cs.");
        }

        public static void LoadScene(string sceneName)
        {
            if (TryInvoke(GetSingleton(SceneControllerTypeName), SceneLoadMethods, sceneName)) return;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>Dano base do ataque do jogador (lê upgrades do GameManager; padrão 10).</summary>
        public static float PlayerAttackDamage
        {
            get
            {
                float v;
                if (TryGetNumber(GetSingleton(GameManagerTypeName), AttackDamageMembers, out v) && v > 0f) return v;
                var p = Player;
                if (p != null)
                {
                    foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
                        if (TryGetNumber(mb, AttackDamageMembers, out v) && v > 0f) return v;
                }
                return 10f;
            }
        }

        /// <summary>Dano de um projétil de nota do jogador (campo "damage" do MusicProjectile, se existir).</summary>
        public static float GetProjectileDamage(GameObject projectile)
        {
            float v;
            if (projectile != null)
            {
                foreach (var mb in projectile.GetComponentsInChildren<MonoBehaviour>())
                    if (TryGetNumber(mb, ProjectileDamageMembers, out v) && v > 0f) return v;
            }
            return PlayerAttackDamage;
        }

        /// <summary>Lê vida atual/máxima do Player (reflexão). False se não achou.</summary>
        public static bool TryGetPlayerHealth(out float hp, out float max)
        {
            hp = 0f; max = 0f;
            var p = Player;
            if (p == null) return false;
            foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
            {
                float h;
                if (!TryGetNumber(mb, HealthMembers, out h)) continue;
                hp = h;
                float m;
                if (!TryGetNumber(mb, MaxHealthMembers, out m)) m = 100f;
                max = m;
                return true;
            }
            return false;
        }

        /// <summary>Revive o jogador depois de morrer (tenta Revive/Respawn..., senão cura total).</summary>
        public static void RevivePlayer()
        {
            var p = Player;
            if (p == null) return;
            foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
                if (TryInvoke(mb, ReviveMethods)) return;
            HealPlayer(9999);
        }

        public static Vector2 GetVelocity(Rigidbody2D rb)
        {
            if (rb == null) return Vector2.zero;
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void ShakeCamera(float magnitude, float duration)
        {
            object inst = GetSingleton(CameraShakeTypeName);
            if (inst == null) return;
            foreach (var m in inst.GetType().GetMethods(All))
            {
                if (m.Name != "Shake" && m.Name != "ShakeCamera" && m.Name != "StartShake") continue;
                var ps = m.GetParameters();
                var args = new object[ps.Length];
                bool ok = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].ParameterType != typeof(float)) { ok = false; break; }
                    string pn = ps[i].Name.ToLowerInvariant();
                    args[i] = (pn.Contains("dur") || pn.Contains("time")) ? duration : magnitude;
                }
                if (!ok) continue;
                try
                {
                    object r = m.Invoke(inst, args);
                    var e = r as IEnumerator;
                    var mb = inst as MonoBehaviour;
                    if (e != null && mb != null) mb.StartCoroutine(e);
                    return;
                }
                catch (Exception) { /* tenta o próximo overload */ }
            }
        }

        // ---------------------------------------------------------------- Física (compatível Unity 2022/6)

        public static void SetVelocity(Rigidbody2D rb, Vector2 v)
        {
            if (rb == null) return;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        // ---------------------------------------------------------------- Reflexão

        public static Type FindType(string name)
        {
            Type cached;
            if (typeCache.TryGetValue(name, out cached)) return cached;

            Type found = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string an = asm.GetName().Name;
                if (an.StartsWith("Unity") || an.StartsWith("System") || an.StartsWith("mscorlib") ||
                    an.StartsWith("Mono") || an.StartsWith("netstandard") || an.StartsWith("Microsoft") ||
                    an.StartsWith("Cinemachine") || an.StartsWith("nunit") || an.StartsWith("Bee."))
                    continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch (Exception) { continue; }

                foreach (var t in types)
                {
                    if (t != null && t.Name == name) { found = t; break; }
                }
                if (found != null) break;
            }
            typeCache[name] = found;
            return found;
        }

        private static object GetSingleton(string typeName)
        {
            Type t = FindType(typeName);
            if (t == null) return null;

            PropertyInfo p = t.GetProperty("Instance", All);
            if (p != null && p.GetMethod != null && p.GetMethod.IsStatic)
            {
                object v = p.GetValue(null);
                if (v != null) return v;
            }
            FieldInfo f = t.GetField("Instance", All);
            if (f != null && f.IsStatic)
            {
                object v = f.GetValue(null);
                if (v != null) return v;
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                var o = UnityEngine.Object.FindFirstObjectByType(t);
                if (o != null) return o;
            }
            return null;
        }

        private static bool TryInvoke(object target, string[] names, params object[] args)
        {
            if (target == null) return false;
            MethodInfo[] methods = target.GetType().GetMethods(All);

            foreach (string name in names)
            {
                MethodInfo best = null;
                object[] bestArgs = null;
                foreach (var m in methods)
                {
                    if (m.Name != name) continue;
                    var ps = m.GetParameters();
                    if (ps.Length > args.Length) continue;

                    var call = new object[ps.Length];
                    bool ok = true;
                    for (int i = 0; i < ps.Length && ok; i++)
                        ok = TryConvert(args[i], ps[i].ParameterType, out call[i]);
                    if (!ok) continue;

                    if (best == null || ps.Length > bestArgs.Length) { best = m; bestArgs = call; }
                }

                if (best == null) continue;
                try
                {
                    best.Invoke(best.IsStatic ? null : target, bestArgs);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Hive] Falha ao chamar " + best.Name + ": " + e.Message);
                    return false;
                }
            }
            return false;
        }

        private static bool TryConvert(object arg, Type to, out object result)
        {
            result = null;
            if (arg == null) return !to.IsValueType;
            Type from = arg.GetType();
            if (to.IsAssignableFrom(from)) { result = arg; return true; }
            if (IsNumeric(from) && IsNumeric(to))
            {
                result = Convert.ChangeType(arg, to);
                return true;
            }
            return false;
        }

        private static bool IsNumeric(Type t)
        {
            return t == typeof(int) || t == typeof(float) || t == typeof(double) ||
                   t == typeof(long) || t == typeof(short) || t == typeof(byte);
        }

        private static bool TryGetNumber(object target, string[] names, out float value)
        {
            value = 0f;
            if (target == null) return false;
            Type t = target.GetType();
            foreach (string n in names)
            {
                PropertyInfo p = t.GetProperty(n, All);
                if (p != null && p.CanRead && p.GetIndexParameters().Length == 0 && IsNumeric(p.PropertyType))
                {
                    value = Convert.ToSingle(p.GetValue(target));
                    return true;
                }
                FieldInfo f = t.GetField(n, All);
                if (f != null && IsNumeric(f.FieldType))
                {
                    value = Convert.ToSingle(f.GetValue(target));
                    return true;
                }
            }
            return false;
        }

        private static void WarnOnce(string key, string message)
        {
            if (warned.Add(key)) Debug.LogWarning("[Hive] " + message);
        }
    }
}
