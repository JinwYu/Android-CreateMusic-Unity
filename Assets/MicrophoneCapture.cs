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
        //Debug.Log("secondsDurationRecording = " + recordedLoops.secondsDurationRecording);

        // Check if there is at least one microphone connected.  
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("Microphone is not connected!"); // Throw a warning message at the console if there isn't.  
        }
        else // At least one microphone is present.  
        {
            micConnected = true;

            maxFreq = 48000;
            recordedLoops.sampleRate = maxFreq;
            
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

            Debug.Log("Start to record.");

            // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource.  
            audioSource.clip = Microphone.Start(null, false, lengthOfRecording + 1, maxFreq);

            // This loop will run for 0.5s (48000/2 samples). Needed to give a mobile phone time to load in the microphone.
            while (!(Microphone.GetPosition(null) > 48000 / 2)) { }

            numRecordButtonClicked++; // Keep track of the number of recordings.

            Invoke("SaveRecording", lengthOfRecording); // When the time of the recording has elapsed, save.
            Debug.Log("Recorded waiting to save.");
        }
        else // No microphone connected.
        {
            // Display a red error message.
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Microphone not connected!");
            Debug.Log("No microphone connected!");
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
        Debug.Log("indexOfRecording = " + numRecordButtonClicked);
        recordedLoops.numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels; // In samples/indices.
        recordedLoops.numChannels = audioSource.clip.channels;
        //Debug.Log("In save recording, num samples in rec = " + recordedLoops.numSamplesInRecording);

        int sizeOfRecording = (int) recordedLoops.numSamplesInRecording; // Keep the same length of every recording.
        //Debug.Log("In save recording, sizeOfRecording = " + sizeOfRecording);
        float[] fullRecording = new float[sizeOfRecording];
        audioSource.clip.GetData(fullRecording, 0); // Get the data of the recording from the buffer.
        float[] tempSamples = new float[sizeOfRecording-48000]; // Remove 1s (48000 samples) from the recording, since the mic recorded 1s longer to give the mobile time to load the microphone.
        System.Array.Copy(fullRecording, 48000/2, tempSamples, 0, tempSamples.Length); // Extract the recording starting at 0.5s and getting rid of the last 0.5s.

        // Räkna rms här?
        float sum = 0;
        for (int i = 0; i < tempSamples.Length; i++)
            sum += tempSamples[i] * tempSamples[i]; // sum squared samples

        float rmsValue = Mathf.Sqrt(sum / tempSamples.Length); // rms = square root of average

        float silentThreshold = 0.01f;
        if (rmsValue > silentThreshold)
        {
            recordedLoops.silentRecording = false;

            // Apply high and low pass filter.
            tempSamples = recordedLoops.ApplyHighPassFilter(tempSamples);
            tempSamples = recordedLoops.ApplyLowPassFilter(tempSamples);

            // Quantize the recording.
            tempSamples = recordedLoops.QuantizeRecording(tempSamples);
            Debug.Log("In MicrophoneCapture, Quantization done");

            if (!recordedLoops.silentRecording)
            {
                tempSamples = recordedLoops.Normalize(tempSamples);
                recordedLoops.silentRecording = false;
                Debug.Log("Normalizing recording because it wasn't silent.");
            }                
        }
        else
        {
            recordedLoops.silentRecording = true;

            Debug.Log("SILENT RECORDING IN MICROPHONECAPTURE SO NOT NORMALIZING!");
            float[] emptyResetLoop = new float[(int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels];
            //Debug.Log("empty loop length = " + emptyResetLoop.Length);
            for (int i = 0; i < emptyResetLoop.Length; i++)
                emptyResetLoop[i] = 0.0f;

            tempSamples = emptyResetLoop;

            // Gör om hela inspelningsgrejen
            // Gå tillbaka till en blå knapp för att starta spela in
                // kalla på showRecordButton i buttonmanager

            // ta numRecordButtonClicked-- för att gå tillbaka till rätt index
            // 

            // Flytta upp koden om att lägga i recordedLoops hit och i if ovan.

        }

        // Sätta silentRecording = true;
        // Därmed skippa lowpass, filter och quantization

        // Fattar dock inte varför den har kvar den gamla inspelningen?

        //audioSource.loop = true; // DEBUG
        //audioSource.Play(); // DEBUG

        // Remove sounds below the threshold.
        //for (int idx = 0; idx < sizeOfRecording; idx++)
        //    if (System.Math.Abs(tempSamples[idx]) < thresholdMicInput)
        //        tempSamples[idx] = 0.0f;





        //// Normalize the recording if not silent.
        //if (!recordedLoops.silentRecording)
        //{
        //    tempSamples = recordedLoops.Normalize(tempSamples);
        //    //recordedLoops.silentRecording = false;
        //    Debug.Log("Normalizing recording because it wasn't silent.");
        //}
        //else
        //{
        //    Debug.Log("SILENT RECORDING IN MICROPHONECAPTURE SO NOT NORMALIZING!");
        //    float[] emptyResetLoop = new float[(int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels];
        //    //Debug.Log("empty loop length = " + emptyResetLoop.Length);
        //    for (int i = 0; i < emptyResetLoop.Length; i++)
        //        emptyResetLoop[i] = 0.0f;

        //    tempSamples = emptyResetLoop;
        //}

        // Måste på något sett vara att scriptable object recorded loops 
        // sparar inspelningar sedan tidigare i recordings arrayen.
        // För jag tror mikrofonen inte spelar in något och getData ger typ tomt.

        // Save the recording.
        recordedLoops.SetRecording(indexOfRecording, tempSamples);
        Debug.Log("Saved recording in MicrophoneCapture script.");

        // DEBUG: jämföra inspelningen med quantized loopen
        //audioSource.loop = true;
        //audioSource.Play(); // Playback the recorded audio.
        //Debug.Log("Saved recording and Playing recording once.");

        // Saving recording complete, so show the play button image on the button.
        // TODO: kommer senare inte göras här utan kommer vara när ljuden processats.
        currentRecButtonSprite.SetToPlaySprite();
        currentRecButtonSprite.UpdateRecordingStatus(false);
    }

}

// TODO: Klarar just nu bara fyra st inspelningar sen blir det error

// TODO: För varje recording ska en ny knapp skapas där man ska kunna spela den senaste inspelningen
