using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OpcoesMenu : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Dropdown resolucaoDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider volumeSlider;

    private Resolution[] resolucoes;

    private void Start()
    {
        // Configura resoluções
        resolucoes = Screen.resolutions;
        resolucaoDropdown.ClearOptions();

        int resolucaoAtualIndex = 0;
        var options = new System.Collections.Generic.List<string>();

        for (int i = 0; i < resolucoes.Length; i++)
        {
            string opcao = resolucoes[i].width + " x " + resolucoes[i].height;
            options.Add(opcao);

            if (resolucoes[i].width == Screen.currentResolution.width &&
                resolucoes[i].height == Screen.currentResolution.height)
            {
                resolucaoAtualIndex = i;
            }
        }

        resolucaoDropdown.AddOptions(options);

        // Carregar preferências
        resolucaoDropdown.value = PlayerPrefs.GetInt("resolution", resolucaoAtualIndex);
        resolucaoDropdown.RefreshShownValue();

        fullscreenToggle.isOn = PlayerPrefs.GetInt("fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        float defaultVolume = 0.5f; // valor inicial
        float volume = PlayerPrefs.GetFloat("volume", defaultVolume);
        volumeSlider.value = volume;
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);

        // Aplicar configs salvas
        DefinirResolucao(resolucaoDropdown.value);
        DefinirTelaCheia(fullscreenToggle.isOn);
    }

    public void DefinirVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("volume", volume);
    }

    public void DefinirTelaCheia(bool telaCheia)
    {
        Screen.fullScreen = telaCheia;
        PlayerPrefs.SetInt("fullscreen", telaCheia ? 1 : 0);
    }

    public void DefinirResolucao(int resolucaoIndex)
    {
        Resolution resolucao = resolucoes[resolucaoIndex];
        Screen.SetResolution(resolucao.width, resolucao.height, Screen.fullScreen);
        PlayerPrefs.SetInt("resolution", resolucaoIndex);
    }
}
