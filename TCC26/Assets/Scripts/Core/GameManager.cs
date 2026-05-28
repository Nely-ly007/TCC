using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// POP ADVENTURE - GameManager
/// Gerencia estado global: vinis, fragmentos, upgrades, cenas.
/// Persiste entre cenas via DontDestroyOnLoad.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── MOEDA ────────────────────────────────────────────────────
    [Header("Vinis (Moeda)")]
    [SerializeField] private int startingVinyls = 0;
    private int currentVinyls;

    // ── FRAGMENTOS DO DISCO ───────────────────────────────────────
    private bool[] discFragments = new bool[4]; // false = não coletado
    public int FragmentsCollected => System.Array.FindAll(discFragments, f => f).Length;

    // ── UPGRADES (GDD) ───────────────────────────────────────────
    // Amplificador: +2 dano (25 vinis) | Salto Harmônico: +10% pulo (30) | Vitalidade: +20 HP (40)
    public bool HasDamageUpgrade { get; private set; }
    public bool HasJumpUpgrade { get; private set; }
    public bool HasVitalityUpgrade { get; private set; }

    // Custo dos upgrades
    public const int DAMAGE_UPGRADE_COST = 25;
    public const int JUMP_UPGRADE_COST = 30;
    public const int VITALITY_UPGRADE_COST = 40;

    // ── ESTADO DE FASES ──────────────────────────────────────────
    private bool[] phasesUnlocked = new bool[5]; // 0=Hub, 1-4=fases

    // ── EVENTS ───────────────────────────────────────────────────
    public System.Action<int> OnVinylCountChanged;
    public System.Action<int> OnFragmentCollected;
    public System.Action OnGameComplete;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentVinyls = startingVinyls;
        phasesUnlocked[0] = true; // Hub sempre desbloqueado
        phasesUnlocked[1] = true; // Fase 1 desbloqueada no início
    }

    // ── VINIS ────────────────────────────────────────────────────
    public void AddVinyls(int amount)
    {
        currentVinyls += amount;
        OnVinylCountChanged?.Invoke(currentVinyls);
    }

    public bool SpendVinyls(int amount)
    {
        if (currentVinyls < amount) return false;
        currentVinyls -= amount;
        OnVinylCountChanged?.Invoke(currentVinyls);
        return true;
    }

    public int GetVinyls() => currentVinyls;

    // ── FRAGMENTOS ───────────────────────────────────────────────
    public void CollectFragment(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= discFragments.Length) return;
        if (discFragments[phaseIndex]) return; // já coletado

        discFragments[phaseIndex] = true;
        OnFragmentCollected?.Invoke(FragmentsCollected);

        // Desbloqueia próxima fase
        if (phaseIndex + 1 < phasesUnlocked.Length)
            phasesUnlocked[phaseIndex + 1] = true;

        // Verifica vitória
        if (FragmentsCollected >= 4)
            OnGameComplete?.Invoke();
    }

    public bool HasFragment(int phaseIndex) => discFragments[phaseIndex];

    // ── UPGRADES ─────────────────────────────────────────────────
    public bool BuyDamageUpgrade()
    {
        if (HasDamageUpgrade || !SpendVinyls(DAMAGE_UPGRADE_COST)) return false;
        HasDamageUpgrade = true;
        PlayerController.Instance?.ApplyDamageUpgrade(2); // GDD: +2 dano
        return true;
    }

    public bool BuyJumpUpgrade()
    {
        if (HasJumpUpgrade || !SpendVinyls(JUMP_UPGRADE_COST)) return false;
        HasJumpUpgrade = true;
        PlayerController.Instance?.ApplyJumpUpgrade(0.1f); // GDD: +10%
        return true;
    }

    public bool BuyVitalityUpgrade()
    {
        if (HasVitalityUpgrade || !SpendVinyls(VITALITY_UPGRADE_COST)) return false;
        HasVitalityUpgrade = true;
        PlayerController.Instance?.ApplyVitalityUpgrade(20); // GDD: +20 HP
        return true;
    }

    // ── CENAS ────────────────────────────────────────────────────
    public void LoadHub() => SceneManager.LoadScene("Hub");
    public void LoadPhase(int phaseNumber)
    {
        if (!phasesUnlocked[phaseNumber]) return;
        SceneManager.LoadScene($"Phase{phaseNumber}");
    }

    public bool IsPhaseUnlocked(int phaseIndex) =>
        phaseIndex >= 0 && phaseIndex < phasesUnlocked.Length && phasesUnlocked[phaseIndex];

    // ── SAVE/LOAD (PlayerPrefs básico) ────────────────────────────
    public void SaveGame()
    {
        PlayerPrefs.SetInt("Vinyls", currentVinyls);
        for (int i = 0; i < discFragments.Length; i++)
            PlayerPrefs.SetInt($"Fragment{i}", discFragments[i] ? 1 : 0);
        PlayerPrefs.SetInt("UpgDamage", HasDamageUpgrade ? 1 : 0);
        PlayerPrefs.SetInt("UpgJump", HasJumpUpgrade ? 1 : 0);
        PlayerPrefs.SetInt("UpgVitality", HasVitalityUpgrade ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        currentVinyls = PlayerPrefs.GetInt("Vinyls", 0);
        for (int i = 0; i < discFragments.Length; i++)
            discFragments[i] = PlayerPrefs.GetInt($"Fragment{i}", 0) == 1;
        HasDamageUpgrade = PlayerPrefs.GetInt("UpgDamage", 0) == 1;
        HasJumpUpgrade = PlayerPrefs.GetInt("UpgJump", 0) == 1;
        HasVitalityUpgrade = PlayerPrefs.GetInt("UpgVitality", 0) == 1;

        // Reaplica upgrades ao player
        if (HasDamageUpgrade) PlayerController.Instance?.ApplyDamageUpgrade(2);
        if (HasJumpUpgrade) PlayerController.Instance?.ApplyJumpUpgrade(0.1f);
        if (HasVitalityUpgrade) PlayerController.Instance?.ApplyVitalityUpgrade(20);

        OnVinylCountChanged?.Invoke(currentVinyls);
    }
}
