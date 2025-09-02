using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections.Generic;

public class OpcoesMenu : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Dropdown resolucaoDropdown;
    [SerializeField] private Dropdown modoTelaDropdown;
    [SerializeField] private Slider volumeSlider;

    private Resolution[] todasResolucoes;
    private List<Resolution> resolucoesFiltradas = new List<Resolution>();
    private int resolucaoAtualIndex;

    private void Start()
    {
        InicializarVolume();
        InicializarModoTela();
        InicializarResolucoes();
        AplicarConfiguracoesSalvas();
    }

    private void InicializarVolume()
    {
        float volumeSalvo = PlayerPrefs.GetFloat("volume", 0.5f);
        volumeSlider.value = volumeSalvo;
        DefinirVolume(volumeSalvo);
    }

    private void InicializarModoTela()
    {
        modoTelaDropdown.ClearOptions();
        modoTelaDropdown.AddOptions(new List<string> { "Janela", "Tela Cheia com Janela", "Tela Cheia" });

        int modoSalvo = PlayerPrefs.GetInt("tela_modo", 1);
        modoTelaDropdown.value = modoSalvo;
        modoTelaDropdown.onValueChanged.AddListener(DefinirModoTela);
        modoTelaDropdown.RefreshShownValue();
    }

    private void InicializarResolucoes()
    {
        todasResolucoes = Screen.resolutions;
        resolucaoDropdown.ClearOptions();
        resolucoesFiltradas.Clear();

        HashSet<string> ids = new HashSet<string>();
        List<string> opcoes = new List<string>();

        Resolution atual = Screen.currentResolution;

        for (int i = 0; i < todasResolucoes.Length; i++)
        {
            Resolution r = todasResolucoes[i];

            // Filtra duplicatas e refresh abaixo de 60 Hz
            string id = $"{r.width}x{r.height}@{r.refreshRateRatio.value:F2}";
            if (ids.Contains(id)) continue;
            if (r.refreshRateRatio.value < 60f) continue;

            ids.Add(id);
            resolucoesFiltradas.Add(r);

            opcoes.Add($"{r.width} x {r.height} @ {r.refreshRateRatio.value:F0}Hz");

            if (r.width == atual.width && r.height == atual.height &&
                Mathf.Approximately((float)r.refreshRateRatio.value, (float)atual.refreshRateRatio.value))
            {
                resolucaoAtualIndex = resolucoesFiltradas.Count - 1;
            }
        }

        resolucaoDropdown.AddOptions(opcoes);

        int indexSalvo = PlayerPrefs.GetInt("resolucao_index", -1);
        if (indexSalvo == -1)
        {
            indexSalvo = resolucaoAtualIndex;
            PlayerPrefs.SetInt("resolucao_index", indexSalvo);
            PlayerPrefs.Save();
        }

        resolucaoDropdown.value = indexSalvo;
        resolucaoDropdown.onValueChanged.AddListener(DefinirResolucao);
        resolucaoDropdown.RefreshShownValue();
    }

    private void AplicarConfiguracoesSalvas()
    {
        int resolucaoIndex = PlayerPrefs.GetInt("resolucao_index", resolucaoAtualIndex);
        int modoTelaIndex = PlayerPrefs.GetInt("tela_modo", 1);

        AplicarResolucao(resolucaoIndex, modoTelaIndex);
    }

    public void DefinirVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f);
        PlayerPrefs.SetFloat("volume", volume);
        PlayerPrefs.Save();
    }

    public void DefinirResolucao(int index)
    {
        int modo = modoTelaDropdown.value;
        AplicarResolucao(index, modo);
        PlayerPrefs.SetInt("resolucao_index", index);
        PlayerPrefs.Save();
    }

    public void DefinirModoTela(int modo)
    {
        int index = resolucaoDropdown.value;
        AplicarResolucao(index, modo);
        PlayerPrefs.SetInt("tela_modo", modo);
        PlayerPrefs.Save();
    }

    private void AplicarResolucao(int index, int modoTela)
    {
        if (index < 0 || index >= resolucoesFiltradas.Count) return;

        Resolution res = resolucoesFiltradas[index];

        FullScreenMode modo = FullScreenMode.Windowed;
        switch (modoTela)
        {
            case 0: modo = FullScreenMode.Windowed; break;
            case 1: modo = FullScreenMode.FullScreenWindow; break;
            case 2: modo = FullScreenMode.ExclusiveFullScreen; break;
        }

        // Usa o RefreshRate já presente na resolução, sem construir novo
        Screen.SetResolution(res.width, res.height, modo, res.refreshRateRatio);
    }
}
