using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneCapture : MonoBehaviour
{
    public int lengthOfRecording = 20;

    //A boolean that flags whether there's a connected microphone  
    private bool micConnected = false;

    //The maximum and minimum available recording frequencies  
    private int minFreq;
    private int maxFreq;

    private AudioSource goAudioSource;

    public const int NUM_POSSIBLE_RECORDINGS = 4;
    public float[][] recordings; // Contains all of the recorded clips.
    private int numRecordButtonClicked = 0; // Starts on -1 to get the right index.

    private const int pixelsButtonOffset = 55; // Used place new buttons below existing ones.

    
    void Start()
    {
        //Check if there is at least one microphone connected  
        if (Microphone.devices.Length <= 0)
        {
            Debug.LogWarning("Microphone is not connected!"); // Throw a warning message at the console if there isn't.  
        }
        else //At least one microphone is present.  
        {
            micConnected = true;            
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq); // Get the default microphone recording capabilities.  

            //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
            if (minFreq == 0 && maxFreq == 0) { maxFreq = 44100; } //...meaning 44100 Hz can be used as the recording sampling rate  

            goAudioSource = this.GetComponent<AudioSource>(); 
        }
        recordings = new float[NUM_POSSIBLE_RECORDINGS][];
    }

    void OnGUI()
    {
        // "If" there is a microphone, "else" no microphone connected.
        if (micConnected)
        {
            // "If" the audio from any microphone isn't being captured, "else" recording is in progress.
            if (!Microphone.IsRecording(null))
            {
                //Case the 'Record' button gets pressed  
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 50), "Record"))
                {
                    // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource.  
                    goAudioSource.clip = Microphone.Start(null, true, lengthOfRecording, maxFreq);
                    numRecordButtonClicked++;
                }

                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset, 200, 50), "Play recording 1"))
                {
                    if(numRecordButtonClicked >= 1)
                        playRecordedSound(0); // Play recording 1.
                }
                if(GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset*2, 200, 50), "Play recording 2"))
                {
                    if (numRecordButtonClicked >= 2)
                        playRecordedSound(1);
                }
                //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset * 3, 200, 50), "Play recording 3"))
                //{
                //    if (nrRecordButtonClicked >= 3) playRecordedSound(2);
                //}
                //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset * 4, 200, 50), "Play recording 4"))
                //{
                //    if (nrRecordButtonClicked >= 4) playRecordedSound(3);
                //}
            }
            else //Recording is in progress  
            {
                //Case the 'Stop and Play' button gets pressed  
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Stop and Play!"))
                {
                    Microphone.End(null); //Stop the audio recording  

                    recordings[numRecordButtonClicked-1] = new float[goAudioSource.clip.samples * goAudioSource.clip.channels];

                    float[] tempSamples = new float[goAudioSource.clip.samples * goAudioSource.clip.channels];
                    goAudioSource.clip.GetData(tempSamples, 0); // Get the data from the buffer.

                    recordings[numRecordButtonClicked-1] = tempSamples; // Save the recording.

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

    void playRecordedSound(int index)
    {
        int length = recordings[index].Length;
        goAudioSource.clip = AudioClip.Create("recorded samples", length, 1, 44100, false);
        goAudioSource.clip.SetData(recordings[index], 0);
        //goAudioSource.loop = true;
        //goAudioSource.Play();
    }



}


/* MISC
 *                     // ctrl + e + c, +u
                    //for (int i = 0; i < 20; i++)
                    //{
                    //    debug.log("samples" + "[" + i + "] = " + samples[i]);
                    //}
 * 
 *                     // Init the jagged array "recordings".
                    //for (int idx = 0; idx < NR_POSSIBLE_RECORDINGS; idx++)
                    //{
                    //    recordings[idx] = new float[goAudioSource.clip.samples * goAudioSource.clip.channels];
                    //}
 * 
 * 
 * //goAudioSource.Stop();
 * 
 */


// TODO: Klarar just nu bara fyra st inspelningar sen blir det error

// TODO: För varje recording ska en ny knapp skapas där man ska kunna spela den senaste inspelningen
