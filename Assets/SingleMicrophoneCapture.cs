using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]

public class SingleMicrophoneCapture : MonoBehaviour
{
    //A boolean that flags whether there's a connected microphone  
    private bool micConnected = false;

    //The maximum and minimum available recording frequencies  
    private int minFreq;
    private int maxFreq;

    private AudioSource goAudioSource;

    private const int NR_POSSIBLE_RECORDINGS = 4;
    float[][] recordings; // Contains all of the recorded clips.
    private int nrRecordButtonClicked = -1; // Starts on -1 to get the right index.

    
    void Start()
    {
        //Check if there is at least one microphone connected  
        if (Microphone.devices.Length <= 0)
        {
            //Throw a warning message at the console if there isn't  
            Debug.LogWarning("Microphone not connected!");
        }
        else //At least one microphone is present  
        {
            //Set 'micConnected' to true  
            micConnected = true;

            //Get the default microphone recording capabilities  
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);

            //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
            if (minFreq == 0 && maxFreq == 0)
            {
                //...meaning 44100 Hz can be used as the recording sampling rate  
                maxFreq = 44100;
            }

            //Get the attached AudioSource component  
            goAudioSource = this.GetComponent<AudioSource>();
        }

        recordings = new float[NR_POSSIBLE_RECORDINGS][];
    }

    void OnGUI()
    {
        //If there is a microphone  
        if (micConnected)
        {
            //If the audio from any microphone isn't being captured  
            if (!Microphone.IsRecording(null))
            {
                //Case the 'Record' button gets pressed  
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Record"))
                {
                    //Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource  
                    goAudioSource.clip = Microphone.Start(null, true, 20, maxFreq);

                    nrRecordButtonClicked++;
                }

                // PLACEHOLDER: 55 typ mellan varje steg på height. Gör om till Switch kanske? Skriv en function för att returnera en rect eller nåt så inte argumenten är nested så här
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 80, 200, 50), "Play recording 1"))
                {
                    // Play recording 1.
                    if(nrRecordButtonClicked >= 0)
                    {
                        //goAudioSource.Stop();
                        int length = recordings[0].Length;
                        goAudioSource.clip = AudioClip.Create("recorded samples", length, 1, 44100, false);
                        goAudioSource.clip.SetData(recordings[0], 0);
                        goAudioSource.loop = true;
                        goAudioSource.Play();
                    }
                }
                if(GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 135, 200, 50), "Play recording 2"))
                {
                    if (nrRecordButtonClicked >= 1)
                    {
                        //goAudioSource.Stop();
                        int length = recordings[1].Length;
                        goAudioSource.clip = AudioClip.Create("recorded samples", length, 1, 44100, false);
                        goAudioSource.clip.SetData(recordings[1], 0);
                        goAudioSource.loop = true;
                        goAudioSource.Play();
                    }
                }
                //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 185, 200, 50), "Play recording 3"))
                //{

                //}
                //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 240, 200, 50), "Play recording 4"))
                //{

                //}
            }
            else //Recording is in progress  
            {
                //Case the 'Stop and Play' button gets pressed  
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Stop and Play!"))
                {
                    Microphone.End(null); //Stop the audio recording  
                    // Init the jagged array "recordings".
                    //for (int idx = 0; idx < NR_POSSIBLE_RECORDINGS; idx++)
                    //{
                    //    recordings[idx] = new float[goAudioSource.clip.samples * goAudioSource.clip.channels];
                    //}

                    recordings[nrRecordButtonClicked] = new float[goAudioSource.clip.samples * goAudioSource.clip.channels];

                    float[] tempSamples = new float[goAudioSource.clip.samples * goAudioSource.clip.channels];
                    goAudioSource.clip.GetData(tempSamples, 0); // Get the data from the buffer.

                    recordings[nrRecordButtonClicked] = tempSamples; // Save the recording.


                    // TODO: Klarar just nu bara fyra st inspelningar sen blir det error

                    // TODO: För varje recording ska en ny knapp skapas där man ska kunna spela den senaste inspelningen

                    // Bara för debuggning
                    goAudioSource.Play(); //Playback the recorded audio  

                }

                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 25, 200, 50), "Recording in progress...");
            }
        }
        else // No microphone  
        {
            //Print a red "Microphone not connected!" message at the center of the screen  
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Microphone not connected!");
        }

    }
}


/* MISC
 *                     // ctrl + e + c, +u
                    //for (int i = 0; i < 20; i++)
                    //{
                    //    debug.log("samples" + "[" + i + "] = " + samples[i]);
                    //}
 * 
 * 
 * 
 */
