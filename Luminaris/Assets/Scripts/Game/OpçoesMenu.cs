using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections.Generic;

public class OpcoesMenu : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Dropdown resolucaoDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider volumeSlider;

    private Resolution[] todasResolucoes;
    private List<Resolution> resolucoesFiltradas = new List<Resolution>();
    private int resolucaoAtualIndex;

    private SaveData saveData;

    private void Start()
    {
        saveData = SaveSystem.HasSave() ? SaveSystem.LoadGame() : new SaveData();

        InicializarVolume();
        InicializarResolucoes();
        InicializarFullscreen();
        AplicarConfiguracoesSalvas();
    }

    private void InicializarVolume()
    {
        float volume = saveData != null ? saveData.volume : 0.5f;
        volumeSlider.value = volume;
        DefinirVolume(volume);
        volumeSlider.onValueChanged.AddListener(DefinirVolume);
    }

    private void InicializarFullscreen()
    {
        bool fullscreen = saveData != null ? saveData.fullscreen : true;
        fullscreenToggle.isOn = fullscreen;
        fullscreenToggle.onValueChanged.AddListener(DefinirFullscreen);
    }

    private void InicializarResolucoes()
    {
        todasResolucoes = Screen.resolutions;
        resolucaoDropdown.ClearOptions();
        resolucoesFiltradas.Clear();

        HashSet<string> ids = new HashSet<string>();
        List<string> opcoes = new List<string>();

        Resolution atual = Screen.currentResolution;
        float aspectRatioAtual = (float)atual.width / atual.height;

        for (int i = 0; i < todasResolucoes.Length; i++)
        {
            Resolution r = todasResolucoes[i];
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

        resolucaoDropdown.AddOptions(opcoes);
        resolucaoDropdown.onValueChanged.AddListener(DefinirResolucao);
    }

    private void AplicarConfiguracoesSalvas()
    {
        if (saveData == null) return;

        int resolucaoIndex = saveData.resolucaoIndex >= 0 ? saveData.resolucaoIndex : resolucaoAtualIndex;
        bool fullscreen = saveData.fullscreen;

        resolucaoIndex = Mathf.Clamp(resolucaoIndex, 0, resolucoesFiltradas.Count - 1);

        resolucaoDropdown.value = resolucaoIndex;
        resolucaoDropdown.RefreshShownValue();
        fullscreenToggle.isOn = fullscreen;

        AplicarResolucao(resolucaoIndex, fullscreen);
    }

    public void DefinirVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f);

        if (saveData == null) saveData = new SaveData();
        saveData.volume = volume;
        SaveSystem.SaveGame(saveData);
    }

    public void DefinirResolucao(int index)
    {
        bool fullscreen = fullscreenToggle.isOn;
        AplicarResolucao(index, fullscreen);

        if (saveData == null) saveData = new SaveData();
        saveData.resolucaoIndex = index;
        SaveSystem.SaveGame(saveData);
    }

    public void DefinirFullscreen(bool fullscreen)
    {
        int index = resolucaoDropdown.value;
        AplicarResolucao(index, fullscreen);

        if (saveData == null) saveData = new SaveData();
        saveData.fullscreen = fullscreen;
        SaveSystem.SaveGame(saveData);
    }

    private void AplicarResolucao(int index, bool fullscreen)
    {
        if (index < 0 || index >= resolucoesFiltradas.Count) return;

        Resolution res = resolucoesFiltradas[index];
        Screen.SetResolution(res.width, res.height, fullscreen);

        Debug.Log($"Aplicando resolução {res.width}x{res.height}, Fullscreen={fullscreen}, Index={index}");
    }
}
