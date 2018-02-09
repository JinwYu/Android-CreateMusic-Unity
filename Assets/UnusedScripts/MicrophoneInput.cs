using UnityEngine;
using System.Collections;
using System; // Behövdes för att kunna använda array.

[RequireComponent(typeof(AudioSource))]
public class MicrophoneInput : MonoBehaviour
{
    public float sensitivity = 100;
    public float loudness = 0;

    AudioSource audioSource;

    float[] samples;

    //public float[] recording;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.clip = Microphone.Start(null, true, 5, 44100); // Spelar in i tio sekunder.

        //GetComponent<AudioSource>().loop = true; // Set the AudioClip to loop
        //GetComponent<AudioSource>().mute = true; // Mute the sound, we don't want the player to hear it
        while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started

        /*        
        int size = GetComponent<AudioSource>().clip.samples;
        Debug.LogError("size = " + size); // = 441 000

        recording = new float[size];
        GetComponent<AudioSource>().clip.GetData(recording, 0);
        */


       
        samples = new float[audioSource.clip.samples * audioSource.clip.channels];

        audioSource.clip.GetData(samples, 0); // Hämtar samples från buffern?
        /*
        int i = 0;
        while (i < samples.Length)
        {
            samples[i] = samples[i] * 0.5F;
            ++i;
        }
        */

        //Microphone.End(null);


        //audioSource.clip.SetData(samples, 0);   // Sätter AudioSource med "samples"??

        

        InvokeRepeating("playRecordedSound", 5.0f, 5.0f);
        // Starting in 2 seconds.
        // a projectile will be launched every 7 seconds

        // Metronom
        //InvokeRepeating("playMetronomeSound", 0.0f, 1.0f);


        // TODO: 
        /*
         * En metronom
         * 
         * Trigga inspelning med en knapp
         * 
         * Spara i minnet eller hårddisk? Vilken är snabbast? för kommer behövas i realtid senare.
         * 
         * Ha flera inspelningar
         * 
         */



        //Debug.LogError(recording[40]);
    }

    void Update()
    {
        //loudness = GetAveragedVolume() * sensitivity;

        // Var 10e sekund spela upp "samples"

        

    }


    void playRecordedSound()
    {
        //Microphone.End(null);
        audioSource.clip.SetData(samples, 0);
        audioSource.Play();
    }

    void playMetronomeSound()
    {
        //audioSource.Play();
    }

    float GetAveragedVolume()
    {
        float[] data = new float[256];
        float a = 0;
        GetComponent<AudioSource>().GetOutputData(data, 0);
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / 256;
    }
}


/*
 * 
 * public void StopRecord() { int lastTime = Microphone.GetPosition(null); if (lastTime == 0) return;

     Debuger.Log("lastTime =" + lastTime);
     Microphone.End(null);
     float[] samples = new float[AudioSource.clip.samples]; //
     AudioSource.clip.GetData(samples, 0);
     float[] ClipSamples = new float[lastTime];
     Array.Copy(samples, ClipSamples, ClipSamples.Length - 1);
     AudioSource.clip = AudioClip.Create("playRecordClip", ClipSamples.Length, 1, 44100, false, false);
     AudioSource.clip.SetData(ClipSamples, 0);
    }
 * 
 * 
 * 
 * 
 * 
 * 
 */
