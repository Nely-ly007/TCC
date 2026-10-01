using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Audio;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class SettingsUIToolkit : MonoBehaviour
{
    [Header("Audio Mixer (opcional)")]
    [SerializeField] private AudioMixer audioMixer;

    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";

    private UIDocument uiDocument;
    private VisualElement root;

    private Slider sliderMaster;
    private Slider sliderMusic;
    private Slider sliderSFX;

    private Label labelMaster;
    private Label labelMusic;
    private Label labelSFX;

    private Toggle toggleFullscreen;
    private DropdownField dropdownResolution;
    private Button btnClose;

    private List<string> resolutionOptions = new();
    private Resolution[] resolutions;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("SettingsUIToolkit: UIDocument não encontrado!");
            return;
        }
    }

    private void OnEnable()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("SettingsUIToolkit: UIDocument não encontrado!");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError(
                "SettingsUIToolkit: rootVisualElement está NULL. " +
                "Verifique se o UIDocument possui um Source Asset."
            );
            return;
        }

        Debug.Log("SettingsUIToolkit: UI inicializada!");

        // Procura os elementos pelo NAME do UXML
        sliderMaster = root.Q<Slider>("slider-master");
        sliderMusic = root.Q<Slider>("slider-music");
        sliderSFX = root.Q<Slider>("slider-sfx");

        labelMaster = root.Q<Label>("label-master");
        labelMusic = root.Q<Label>("label-music");
        labelSFX = root.Q<Label>("label-sfx");

        toggleFullscreen = root.Q<Toggle>("toggle-fullscreen");

        dropdownResolution =
            root.Q<DropdownField>("dropdown-resolution");

        btnClose =
            root.Q<Button>("btn-close-settings");

        // Eventos
        if (sliderMaster != null)
            sliderMaster.RegisterValueChangedCallback(
                e => OnMasterChanged(e.newValue)
            );

        if (sliderMusic != null)
            sliderMusic.RegisterValueChangedCallback(
                e => OnMusicChanged(e.newValue)
            );

        if (sliderSFX != null)
            sliderSFX.RegisterValueChangedCallback(
                e => OnSFXChanged(e.newValue)
            );

        if (toggleFullscreen != null)
            toggleFullscreen.RegisterValueChangedCallback(
                e => OnFullscreenChanged(e.newValue)
            );

        if (dropdownResolution != null)
            dropdownResolution.RegisterValueChangedCallback(
                e => OnResolutionChanged(e.newValue)
            );

        if (btnClose != null)
            btnClose.RegisterCallback<ClickEvent>(
                _ => Hide()
            );

        SetupResolutions();
        LoadPreferences();

        Hide();
    }

    // =========================================================
    // SHOW / HIDE
    // =========================================================

    public void Show()
    {
        Debug.Log("SettingsUIToolkit.Show() chamado!");

        if (root == null)
        {
            Debug.LogWarning(
                "SettingsUIToolkit: root estava NULL. Tentando inicializar..."
            );

            InitializeUI();
        }

        if (root == null)
        {
            Debug.LogError(
                "SettingsUIToolkit: NÃO FOI POSSÍVEL INICIALIZAR O ROOT."
            );
            return;
        }

        IsOpen = true;

        root.style.display = DisplayStyle.Flex;

        LoadPreferences();

        Debug.Log("SettingsUIToolkit: painel aberto!");
    }

    public void Hide()
    {
        IsOpen = false;

        if (root == null)
            return;

        root.style.display = DisplayStyle.None;
    }

    public void Toggle()
    {
        if (IsOpen)
            Hide();
        else
            Show();
    }

    // =========================================================
    // RESOLUÇÕES
    // =========================================================

    private void SetupResolutions()
    {
        if (dropdownResolution == null)
            return;

        resolutions = Screen.resolutions;

        resolutionOptions.Clear();

        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution r = resolutions[i];

            resolutionOptions.Add(
                $"{r.width} x {r.height}"
            );

            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        dropdownResolution.choices = resolutionOptions;

        if (resolutionOptions.Count > 0)
            dropdownResolution.index = currentIndex;
    }

    // =========================================================
    // ÁUDIO
    // =========================================================

    private void OnMasterChanged(float value)
    {
        if (labelMaster != null)
            labelMaster.text =
                $"{Mathf.RoundToInt(value * 100)}%";

        if (audioMixer != null)
        {
            audioMixer.SetFloat(
                MASTER_PARAM,
                LinearToDecibel(value)
            );
        }
        else
        {
            AudioListener.volume = value;
        }

        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void OnMusicChanged(float value)
    {
        if (labelMusic != null)
            labelMusic.text =
                $"{Mathf.RoundToInt(value * 100)}%";

        if (audioMixer != null)
        {
            audioMixer.SetFloat(
                MUSIC_PARAM,
                LinearToDecibel(value)
            );
        }

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private void OnSFXChanged(float value)
    {
        if (labelSFX != null)
            labelSFX.text =
                $"{Mathf.RoundToInt(value * 100)}%";

        if (audioMixer != null)
        {
            audioMixer.SetFloat(
                SFX_PARAM,
                LinearToDecibel(value)
            );
        }

        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    // =========================================================
    // TELA
    // =========================================================

    private void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;

        PlayerPrefs.SetInt(
            "Fullscreen",
            value ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    private void OnResolutionChanged(string value)
    {
        if (dropdownResolution == null ||
            resolutions == null)
            return;

        int idx = dropdownResolution.index;

        if (idx >= 0 && idx < resolutions.Length)
        {
            Resolution r = resolutions[idx];

            Screen.SetResolution(
                r.width,
                r.height,
                Screen.fullScreen
            );

            PlayerPrefs.SetInt(
                "ResolutionIndex",
                idx
            );

            PlayerPrefs.Save();
        }
    }

    // =========================================================
    // PREFERÊNCIAS
    // =========================================================

    private void LoadPreferences()
    {
        float master =
            PlayerPrefs.GetFloat("MasterVolume", 0.8f);

        float music =
            PlayerPrefs.GetFloat("MusicVolume", 1f);

        float sfx =
            PlayerPrefs.GetFloat("SFXVolume", 1f);

        bool fullscreen =
            PlayerPrefs.GetInt("Fullscreen", 0) == 1;

        int resIdx =
            PlayerPrefs.GetInt("ResolutionIndex", 0);

        if (sliderMaster != null)
        {
            sliderMaster.SetValueWithoutNotify(master);
            OnMasterChanged(master);
        }

        if (sliderMusic != null)
        {
            sliderMusic.SetValueWithoutNotify(music);
            OnMusicChanged(music);
        }

        if (sliderSFX != null)
        {
            sliderSFX.SetValueWithoutNotify(sfx);
            OnSFXChanged(sfx);
        }

        if (toggleFullscreen != null)
        {
            toggleFullscreen.SetValueWithoutNotify(fullscreen);
        }

        if (dropdownResolution != null &&
            resolutions != null &&
            resolutions.Length > 0)
        {
            resIdx = Mathf.Clamp(
                resIdx,
                0,
                resolutions.Length - 1
            );

            dropdownResolution.SetValueWithoutNotify(
                dropdownResolution.choices[resIdx]
            );
        }
    }

    // =========================================================
    // UTIL
    // =========================================================

    private float LinearToDecibel(float linear)
    {
        return linear > 0.001f
            ? Mathf.Log10(linear) * 20f
            : -80f;
    }
}
