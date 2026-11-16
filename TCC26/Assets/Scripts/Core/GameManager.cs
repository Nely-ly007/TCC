
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// POP ADVENTURE - GameManager
/// Gerencia estado global: vinis, fragmentos, upgrades e cenas.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Debug / Testes Editor")]
    [SerializeField] private bool clearSaveOnStartInEditor = false;

    [Header("Próxima Cena")]
    public string NextScene { get; set; } = "MainMenu";

    [Header("Vinis (Moeda)")]
    [SerializeField] private int startingVinyls = 0;
    private int currentVinyls;

    private bool[] discFragments = new bool[5];
    private bool[] phasesUnlocked = new bool[5];

    public int FragmentsCollected
    {
        get
        {
            int count = 0;

            for (int i = 1; i <= 4; i++)
            {
                if (discFragments[i])
                    count++;
            }

            return count;
        }
    }

    [Header("Upgrades")]
    public bool HasDamageUpgrade { get; private set; }
    public bool HasJumpUpgrade { get; private set; }
    public bool HasVitalityUpgrade { get; private set; }

    public const int DAMAGE_UPGRADE_COST = 25;
    public const int JUMP_UPGRADE_COST = 30;
    public const int VITALITY_UPGRADE_COST = 40;

    public System.Action<int> OnVinylCountChanged;
    public System.Action<int> OnFragmentCollected;
    public System.Action OnGameComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        if (clearSaveOnStartInEditor)
        {
            ResetSaveData();
        }
#endif

        currentVinyls = startingVinyls;
        phasesUnlocked[1] = true;

        LoadGame();
    }

    // ── ÁUDIO / TRANSIÇÃO DE CENAS ─────────────────────

    private void StopMusicBeforeSceneChange()
    {
        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.StopMusic();

            Debug.Log("[GameManager] Música interrompida antes da transição.");
        }
    }

    // ── VINIS ──────────────────────────────────────────

    public void AddVinyls(int amount)
    {
        currentVinyls += amount;
        OnVinylCountChanged?.Invoke(currentVinyls);
    }

    public bool SpendVinyls(int amount)
    {
        if (currentVinyls < amount)
            return false;

        currentVinyls -= amount;
        OnVinylCountChanged?.Invoke(currentVinyls);

        SaveGame();

        return true;
    }

    public int GetVinyls()
    {
        return currentVinyls;
    }

    // ── FRAGMENTOS E FASES ─────────────────────────────

    public void CollectFragment(int phaseIndex)
    {
        if (phaseIndex < 1 || phaseIndex > 4)
            return;

        if (discFragments[phaseIndex])
            return;

        discFragments[phaseIndex] = true;

        int nextPhase = phaseIndex + 1;

        if (nextPhase <= 4)
            phasesUnlocked[nextPhase] = true;

        SaveGame();

        OnFragmentCollected?.Invoke(phaseIndex);

        if (FragmentsCollected >= 4)
            OnGameComplete?.Invoke();
    }

    public bool HasFragment(int phaseIndex)
    {
        if (phaseIndex < 1 || phaseIndex > 4)
            return false;

        return discFragments[phaseIndex];
    }

    public bool IsPhaseUnlocked(int phaseIndex)
    {
        if (phaseIndex < 1 || phaseIndex > 4)
            return false;

        return phasesUnlocked[phaseIndex];
    }

    // ── UPGRADES ───────────────────────────────────────

    public bool BuyDamageUpgrade()
    {
        if (HasDamageUpgrade || !SpendVinyls(DAMAGE_UPGRADE_COST))
            return false;

        HasDamageUpgrade = true;

        PlayerController.Instance?.ApplyDamageUpgrade(2);

        SaveGame();

        return true;
    }

    public bool BuyJumpUpgrade()
    {
        if (HasJumpUpgrade || !SpendVinyls(JUMP_UPGRADE_COST))
            return false;

        HasJumpUpgrade = true;

        PlayerController.Instance?.ApplyJumpUpgrade(0.1f);

        SaveGame();

        return true;
    }

    public bool BuyVitalityUpgrade()
    {
        if (HasVitalityUpgrade || !SpendVinyls(VITALITY_UPGRADE_COST))
            return false;

        HasVitalityUpgrade = true;

        PlayerController.Instance?.ApplyVitalityUpgrade(20);

        SaveGame();

        return true;
    }

    // ── CENAS ──────────────────────────────────────────

    public void LoadHub()
    {
        StopMusicBeforeSceneChange();

        NextScene = "Hub";

        SceneManager.LoadScene("LoadingScene");
    }

    public void LoadPhase(int phaseNumber)
    {
        if (!IsPhaseUnlocked(phaseNumber))
        {
            Debug.LogWarning("[GameManager] Fase bloqueada: " + phaseNumber);
            return;
        }

        StopMusicBeforeSceneChange();

        NextScene = $"Fase {phaseNumber}";

        Debug.Log("[GameManager] Carregando " + NextScene);

        SceneManager.LoadScene("LoadingScene");
    }

    public void LoadMainMenu()
    {
        StopMusicBeforeSceneChange();

        NextScene = "MainMenu";

        SceneManager.LoadScene("LoadingScene");
    }

    // ── SAVE / LOAD ────────────────────────────────────

    public void SaveGame()
    {
        PlayerPrefs.SetInt("Vinyls", currentVinyls);

        for (int i = 1; i <= 4; i++)
        {
            PlayerPrefs.SetInt($"Fragment{i}", discFragments[i] ? 1 : 0);
            PlayerPrefs.SetInt($"PhaseUnlocked{i}", phasesUnlocked[i] ? 1 : 0);
        }

        PlayerPrefs.SetInt("UpgDamage", HasDamageUpgrade ? 1 : 0);
        PlayerPrefs.SetInt("UpgJump", HasJumpUpgrade ? 1 : 0);
        PlayerPrefs.SetInt("UpgVitality", HasVitalityUpgrade ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        currentVinyls = PlayerPrefs.GetInt("Vinyls", startingVinyls);

        phasesUnlocked[1] = true;

        for (int i = 1; i <= 4; i++)
        {
            discFragments[i] = PlayerPrefs.GetInt($"Fragment{i}", 0) == 1;

            if (PlayerPrefs.GetInt($"PhaseUnlocked{i}", 0) == 1)
                phasesUnlocked[i] = true;
        }

        HasDamageUpgrade = PlayerPrefs.GetInt("UpgDamage", 0) == 1;
        HasJumpUpgrade = PlayerPrefs.GetInt("UpgJump", 0) == 1;
        HasVitalityUpgrade = PlayerPrefs.GetInt("UpgVitality", 0) == 1;

        OnVinylCountChanged?.Invoke(currentVinyls);
    }

    [ContextMenu("Reset Save Data")]
    public void ResetSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        for (int i = 0; i < discFragments.Length; i++)
        {
            discFragments[i] = false;
            phasesUnlocked[i] = false;
        }

        phasesUnlocked[1] = true;

        HasDamageUpgrade = false;
        HasJumpUpgrade = false;
        HasVitalityUpgrade = false;

        currentVinyls = startingVinyls;
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
