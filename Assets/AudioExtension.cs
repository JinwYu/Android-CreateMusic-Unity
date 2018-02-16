using UnityEngine;
using UnityEngine.Audio;

public class AudioExtension : MonoBehaviour{

    public AudioMixer audioMixer;

    public AudioMixerGroup[] AllMixerGroups
    {
        get
        {
            return audioMixer.FindMatchingGroups(string.Empty);

        ;}
    }
}