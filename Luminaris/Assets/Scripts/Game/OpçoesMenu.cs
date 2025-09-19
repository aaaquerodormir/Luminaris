using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class OpcoesMenu : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private TMP_Dropdown resolucaoDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider volumeSlider;

    private Resolution[] todasResolucoes;
    private List<Resolution> resolucoesFiltradas = new();
    private int resolucaoAtualIndex;

    private const string VOLUME_KEY = "volume";
    private const string FULLSCREEN_KEY = "fullscreen";
    private const string RESOLUCAO_KEY = "resolucaoIndex";

    private void Awake()
    {
        InicializarResolucoes();
    }

    private void OnEnable()
    {
        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener(DefinirVolume);

        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.AddListener(DefinirFullscreen);

        resolucaoDropdown.onValueChanged.RemoveAllListeners();
        resolucaoDropdown.onValueChanged.AddListener(DefinirResolucao);

        bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;
        fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        if (resolucoesFiltradas.Count > 0)
        {
            int resolucaoIndex = PlayerPrefs.GetInt(RESOLUCAO_KEY, resolucaoAtualIndex);
            resolucaoIndex = Mathf.Clamp(resolucaoIndex, 0, resolucoesFiltradas.Count - 1);
            resolucaoDropdown.SetValueWithoutNotify(resolucaoIndex);
            resolucaoDropdown.RefreshShownValue();

            AplicarResolucao(resolucaoIndex, fullscreen);
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;

        float volume = PlayerPrefs.GetFloat(VOLUME_KEY, 0.5f);
        volumeSlider.SetValueWithoutNotify(volume);
        ApplyVolumeToMixer(volume);

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(volumeSlider.GetComponent<RectTransform>());
    }

    private void ApplyVolumeToMixer(float volume)
    {
        float db = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(masterParam, db);
    }

    public void DefinirVolume(float volume)
    {
        ApplyVolumeToMixer(volume);
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    public void DefinirFullscreen(bool fullscreen)
    {
        AplicarResolucao(resolucaoDropdown.value, fullscreen);
        PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void DefinirResolucao(int index)
    {
        AplicarResolucao(index, fullscreenToggle.isOn);
        PlayerPrefs.SetInt(RESOLUCAO_KEY, index);
        PlayerPrefs.Save();
    }

    public void DiminuirMasterDb(float deltaDb)
    {
        if (audioMixer.GetFloat(masterParam, out float currentDb))
        {
            float novoDb = currentDb - deltaDb;
            audioMixer.SetFloat(masterParam, novoDb);
        }
    }

    private void InicializarResolucoes()
    {
        todasResolucoes = Screen.resolutions;
        resolucaoDropdown.ClearOptions();
        resolucoesFiltradas.Clear();

        HashSet<string> ids = new();
        List<string> opcoes = new();

        Resolution atual = Screen.currentResolution;
        float aspectRatioAtual = (float)atual.width / atual.height;

        foreach (var r in todasResolucoes)
        {
            float aspectRatio = (float)r.width / r.height;
            if (!Mathf.Approximately(aspectRatio, aspectRatioAtual)) continue;

            string id = $"{r.width}x{r.height}";
            if (ids.Contains(id)) continue;

            ids.Add(id);
            resolucoesFiltradas.Add(r);
            opcoes.Add($"{r.width} x {r.height}");
        }

        resolucoesFiltradas.Sort((a, b) => b.width.CompareTo(a.width));
        opcoes.Sort((a, b) =>
        {
            int aw = int.Parse(a.Split('x')[0]);
            int bw = int.Parse(b.Split('x')[0]);
            return bw.CompareTo(aw);
        });

        for (int i = 0; i < resolucoesFiltradas.Count; i++)
        {
            if (resolucoesFiltradas[i].width == atual.width &&
                resolucoesFiltradas[i].height == atual.height)
            {
                Resolution nativa = resolucoesFiltradas[i];
                string opcaoNativa = opcoes[i];

                resolucoesFiltradas.RemoveAt(i);
                opcoes.RemoveAt(i);

                resolucoesFiltradas.Insert(0, nativa);
                opcoes.Insert(0, opcaoNativa);

                resolucaoAtualIndex = 0;
                break;
            }
        }

        int limite = Mathf.Min(5, resolucoesFiltradas.Count);
        resolucoesFiltradas = resolucoesFiltradas.GetRange(0, limite);
        opcoes = opcoes.GetRange(0, limite);

        List<TMP_Dropdown.OptionData> dadosTMP = new();
        foreach (string opcao in opcoes)
            dadosTMP.Add(new TMP_Dropdown.OptionData(opcao));

        resolucaoDropdown.AddOptions(dadosTMP);
    }

    private void AplicarResolucao(int index, bool fullscreen)
    {
        if (index < 0 || index >= resolucoesFiltradas.Count) return;
        Resolution res = resolucoesFiltradas[index];
        Screen.SetResolution(res.width, res.height, fullscreen);
    }
}
