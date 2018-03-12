using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneCapture : MonoBehaviour
{
    //private int lengthOfRecording = 3;     
    private bool micConnected = false; // A boolean that flags whether there's a connected microphone.
    private int minFreq, maxFreq; //The maximum and minimum available recording frequencies 
    private AudioSource audioSource;
    private int numRecordButtonClicked = 0;
    private const int pixelsButtonOffset = 55; // Used to place new buttons below existing ones.
    float durationOfLoop;
    float thresholdMicInput = 0.0015f;
    public AudioClip beepSound;

    [SerializeField] // To make it show up in the inspector.
    private RecordedLoops recordedLoops;
    [SerializeField]
    private CurrentRecButtonSprite currentRecButtonSprite;

    void Start()
    {
        recordedLoops.recordings = new float[RecordedLoops.NUM_POSSIBLE_RECORDINGS][];

        // Calculate how many milliseconds in one beat.
        int msInAMinute = 60000;
        float msInOneBeat = msInAMinute / recordedLoops.bpm;

        // Calculate how many samples/indices corresponds to one second.
        recordedLoops.msDurationRecording = msInOneBeat * recordedLoops.numBeatsPerSegment;
        recordedLoops.secondsDurationRecording = recordedLoops.msDurationRecording / 1000;

        // Check if there is at least one microphone connected.  
        if (Microphone.devices.Length <= 0)
        {
            Debug.LogWarning("Microphone is not connected!"); // Throw a warning message at the console if there isn't.  
        }
        else // At least one microphone is present.  
        {
            micConnected = true;            
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq); // Get the default microphone recording capabilities.  

            //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
            if (minFreq == 0 && maxFreq == 0)
            {
                //...meaning 44100 Hz can be used as the recording sampling rate.  
                maxFreq = 44100;
                recordedLoops.sampleRate = maxFreq;
            } 
            audioSource = this.GetComponent<AudioSource>(); 
        }
    }

   
    public void StartRecording()
    {
        // "If" there is a microphone, "else" no microphone connected.
        if (micConnected)
        {
            //GetComponent<AudioSource>().PlayOneShot(beepSound); // Play a beep sound to give feedback that a recording has started.

            // Set the button image to show that a recording is in progress.
            currentRecButtonSprite.SetToRecInProgSprite1();
            currentRecButtonSprite.UpdateRecordingStatus(true);

            int lengthOfRecording = (int)recordedLoops.secondsDurationRecording;
            recordedLoops.secondsDurationRecording = lengthOfRecording;

            // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource.  
            audioSource.clip = Microphone.Start(null, false, lengthOfRecording, maxFreq);

            numRecordButtonClicked++; // Keep track of the number of recordings.

            Invoke("SaveRecording", lengthOfRecording); // When the time of the recording has elapsed, save.
            Debug.Log("Recorded waiting to save.");
        }
        else // No microphone connected.
        {
            // Display a red error message.
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Microphone not connected!");
        }
    }

    //void OnGUI()
    //{
        //// "If" there is a microphone, "else" no microphone connected.
        //if (micConnected)
        //{
        //    // "If" the audio from any microphone isn't being captured, "else" recording is in progress.
        //    if (!Microphone.IsRecording(null))
        //    {
        //        //Case the 'Record' button gets pressed  
        //        if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 50), "Record"))
        //        {
        //            int lengthOfRecording = (int) recordedLoops.secondsDurationRecording;
        //            recordedLoops.secondsDurationRecording = lengthOfRecording;

        //            // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource.  
        //            audioSource.clip = Microphone.Start(null, false, lengthOfRecording, maxFreq);

        //            numRecordButtonClicked++; // Keep track of the number of recordings.

        //            Invoke("SaveRecording", lengthOfRecording); // When the time of the recording has elapsed, save.
        //        }

        //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset, 200, 50), "Play recording 1"))
        //{
        //    if (numRecordButtonClicked >= 1)
        //        PlayRecordedSound(0); // Play recording 1.
        //}
        //        if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset*2, 200, 50), "Play recording 2"))
        //        {
        //            if (numRecordButtonClicked >= 2)
        //                PlayRecordedSound(1);
        //        }
        //        //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset * 3, 200, 50), "Play recording 3"))
        //        //{
        //        //    if (nrRecordButtonClicked >= 3) playRecordedSound(2);
        //        //}
        //        //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + pixelsButtonOffset * 4, 200, 50), "Play recording 4"))
        //        //{
        //        //    if (nrRecordButtonClicked >= 4) playRecordedSound(3);
        //        //}
        //    }
        //    else // Recording is in progress.  
        //    {
        //        GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 25, 200, 50), "Recording in progress...");
        //    }
        //}
        //else // No microphone connected.
        //{
        //    // Display a red error message.
        //    GUI.contentColor = Color.red;
        //    GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Microphone not connected!");
        //}

    //}

    //void PlayRecordedSound(int index)
    //{
    //    float[] recordingToPlay = recordedLoops.GetIndividualRecording(index);
    //    //float[] recordingToPlay = recordedLoops.recordings[index];
    //    int sizeOfRecording = recordingToPlay.Length;
    //    int sampleRate = recordedLoops.sampleRate;
    //    audioSource.clip = AudioClip.Create("recorded samples", sizeOfRecording, 1, sampleRate, false);
    //    audioSource.clip.SetData(recordingToPlay, 0);
    //    //goAudioSource.loop = true;
    //    audioSource.Play();
    //}

    // Saves the data recorded by the microphone. Called from the "StartRecording" function.
    void SaveRecording()
    {
        currentRecButtonSprite.UpdateRecordingStatus(false);

        Microphone.End(null); // Stop the audio recording if it hasn't already been stopped. 

        int indexOfRecording = numRecordButtonClicked - 1;
        recordedLoops.numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels; // In samples/indices.
        recordedLoops.numChannels = audioSource.clip.channels;

        int sizeOfRecording = (int) recordedLoops.numSamplesInRecording; // Keep the same length of every recording.

        float[] tempSamples = new float[sizeOfRecording];
        audioSource.clip.GetData(tempSamples, 0); // Get the data of the recording from the buffer.

        // Remove sounds below the threshold.
        //for (int idx = 0; idx < sizeOfRecording; idx++)
        //    if (System.Math.Abs(tempSamples[idx]) < thresholdMicInput)
        //        tempSamples[idx] = 0.0f;

        // Apply high and low pass filter.
        tempSamples = recordedLoops.ApplyHighPassFilter(tempSamples);
        tempSamples = recordedLoops.ApplyLowPassFilter(tempSamples);

        // Quantize the recording.
        //tempSamples = recordedLoops.QuantizeRecording(tempSamples);
        //Debug.Log("Quantization done");

        // Normalize the recording.
        tempSamples = recordedLoops.Normalize(tempSamples);

        // Save the recording.
        recordedLoops.SetRecording(indexOfRecording, tempSamples);

        // Bara för debuggning
        //audioSource.loop = true;
        audioSource.Play(); // Playback the recorded audio.
        Debug.Log("Saved recording and Playing recording once.");

        // Saving recording complete, so show the play button image on the button.
        // TODO: kommer senare inte göras här utan kommer vara när ljuden processats.
        currentRecButtonSprite.SetToPlaySprite();
        currentRecButtonSprite.UpdateRecordingStatus(false);
    }

}

// TODO: Klarar just nu bara fyra st inspelningar sen blir det error

// TODO: För varje recording ska en ny knapp skapas där man ska kunna spela den senaste inspelningen
