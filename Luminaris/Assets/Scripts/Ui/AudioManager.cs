using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configurações")]
    [SerializeField] private AudioMixerGroup masterGroup;

    [Header("Clips Registrados")]
    [SerializeField] private AudioClip andandoClip;
    [SerializeField] private AudioClip pulandoClip;
    [SerializeField] private AudioClip checkpointClip;
    [SerializeField] private AudioClip powerUpClip;
    [SerializeField] private AudioClip lavaClip;
    [SerializeField] private AudioClip morteClip;

    private readonly Dictionary<string, AudioClip> clips = new();
    private readonly List<AudioSource> loopSources = new();
    private AudioSource uiSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        if (masterGroup != null)
            uiSource.outputAudioMixerGroup = masterGroup;

        clips["Andando"] = andandoClip;
        clips["Pulando"] = pulandoClip;
        clips["Checkpoint"] = checkpointClip;
        clips["PowerUp"] = powerUpClip;
        clips["Lava"] = lavaClip;
        clips["Morrendo"] = morteClip;
    }

    // Sons de UI
    public void PlayUISound(AudioClip clip)
    {
        if (clip == null) return;
        uiSource.PlayOneShot(clip);
    }

    // Sons rápidos
    public void PlaySound(string key)
    {
        if (!clips.ContainsKey(key) || clips[key] == null) return;

        var source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = masterGroup;
        source.PlayOneShot(clips[key]);
        Destroy(source, clips[key].length);
    }

    // Sons em loop
    public AudioSource PlayLoop(string key, GameObject target)
    {
        if (!clips.ContainsKey(key) || clips[key] == null) return null;

        var source = target.AddComponent<AudioSource>();
        source.clip = clips[key];
        source.loop = true;
        source.outputAudioMixerGroup = masterGroup;
        source.Play();
        loopSources.Add(source);
        return source;
    }

    public void StopLoop(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        loopSources.Remove(source);
        Destroy(source);
    }

    public void PauseAllLoops()
    {
        foreach (var source in loopSources)
        {
            if (source != null && source.isPlaying)
                source.Pause();
        }
    }

    public void ResumeAllLoops()
    {
        foreach (var source in loopSources)
        {
            if (source != null && !source.isPlaying)
                source.UnPause();
        }
    }
}
