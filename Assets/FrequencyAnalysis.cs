using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrequencyAnalysis : MonoBehaviour
{
    private AudioSource audioSource;
    public float frequency = 0.0f;
    private bool aRecordingExists = false;

    [SerializeField] // To make it show up in the inspector.
    private RecordedLoops recordedLoops;

    // Bara analysera en recording i taget!

    // Use this for initialization
    void Start ()
    {
        audioSource = this.GetComponent<AudioSource>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (aRecordingExists && audioSource.isPlaying)
        {
            frequency = GetFundamentalFrequency();
            Debug.Log("freq = " + frequency);
        }
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width / 2 - 100 - 200, Screen.height / 2 + 55 * 4, 200, 50), "Analyse freq"))
        {
            aRecordingExists = true;

            // Sätta audio source clip till datan från recordedLoops
            int numSamples = (int)(recordedLoops.sampleRate * recordedLoops.secondsDurationRecording);
            int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording;

            audioSource.clip = AudioClip.Create("Recording to analyse", numSamplesInRecording, 1, recordedLoops.sampleRate, false);
            audioSource.clip.SetData(recordedLoops.recordings[0], 0); // Ändra så blir en funktion som kan ta in en recording.

            //audioSource.loop = true;     
            //audioSource.mute = true; // Den ska vara tyst för spelaren medan vi analyserar loopen.
            audioSource.Play();

            // Verkar behöva routa ljudet från audiosource till en channel i audioMixern och dra ner
            // volymen på den, så att den spelas där istället när den analseras av det här scriptet.
        }
    }

    float GetFundamentalFrequency()
    {
        float fundamentalFrequency = 0.0f;
        float[] dataToAnalyse = new float[8192];

        // Analyse the audio stream coming through the audio source component using FFT.
        audioSource.GetSpectrumData(dataToAnalyse, 0, FFTWindow.BlackmanHarris);

        float loudestFreq = 0.0f;
        int indexOfHighestFreq = 0;

        // Keep the strength of the strongest signal and 
        // keep the index of the bin where that signal was found.
        for (int indexIterator = 1; indexIterator < 8192; indexIterator++)
        {
            if (loudestFreq < dataToAnalyse[indexIterator])
            {
                loudestFreq = dataToAnalyse[indexIterator];
                indexOfHighestFreq = indexIterator;
            }
        }
        fundamentalFrequency = indexOfHighestFreq * recordedLoops.sampleRate / 8192;

        return fundamentalFrequency;
    }



}
