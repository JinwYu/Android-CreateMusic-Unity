using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImplementBernessScript : MonoBehaviour {

    AudioSource audioSource;

	// Use this for initialization
	void Start () {

        audioSource = GetComponent<AudioSource>();

        float[] tempSamples = new float[audioSource.clip.samples];
        audioSource.clip.GetData(tempSamples, 0);

        PitchShifter.PitchShift(2.0f, tempSamples.LongLength, 44100, tempSamples);
        Debug.Log("Pitch shifted the clip.");

        audioSource.clip = AudioClip.Create("pitch shifted clip", tempSamples.Length, audioSource.clip.channels, 44100, false);
        audioSource.clip.SetData(tempSamples, 0);
        audioSource.loop = true;

        audioSource.Play();
        Debug.Log("Playing pitchshifted clip.");

        // Måste fixa hur 1 till 0.5 ger en oktav neråt och 1 till 2 ger en oktav uppåt
        // Måste reglera hur pitch factor ska reglera

        // Om length i antal sampel för ett klipp är lite eller om klippet är highpitched
        // så ska jag inte pitchshifta

        // Om flera segment sitter ihop så kanske jag ska skicka alla de som ett stort klipp att pitchas
        // för annars kommer varje individuellt väldigt kort segment att pitchshiftas och
        // det kommer uppstå konstiga skillnader i ton mellan klippen och förmodligen låta hemskt

        // Lära mig all teknik/matematik bakom den här skiten
    }
}
