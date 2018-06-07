﻿using System.Collections;
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
    public const int LENGTH_OF_DELAY_IN_SAMPLES = 48000 / 2; // 48000 = one beat.

    [SerializeField] // To make it show up in the inspector.
    private RecordedLoops recordedLoops;
    [SerializeField]
    private CurrentRecButtonSprite currentRecButtonSprite;

    // Assign the method to run during an event.
    private void AssignMethodToRunDuringAnEvent()
    {
        ApplicationProperties.changeEvent += MethodToRun; // Subscribing to the event by adding the method to the "publisher".
    }
    
    // The method which will run when an event is triggered.
    void MethodToRun(State state)
    {
        Debug.Log("New state in MicCapture = " + state);  // This will trigger anytime you call MoreApples() on ClassA

        if (state == State.SilentRecording)
        {
            // Decrease number of recordings.
            numRecordButtonClicked--;

            Debug.Log("NUM REC BUT CLICKED = " + numRecordButtonClicked);
        }
    }

    void Start()
    {
        AssignMethodToRunDuringAnEvent();

        recordedLoops.recordings = new float[ApplicationProperties.NUM_POSSIBLE_RECORDINGS][];

        // Calculate how many milliseconds in one beat.
        int msInAMinute = 60000;
        float msInOneBeat = msInAMinute / ApplicationProperties.BPM;

        // Calculate how many samples/indices corresponds to one second.
        recordedLoops.msDurationRecording = msInOneBeat * ApplicationProperties.NUM_BEATS_PER_LOOP;
        recordedLoops.secondsDurationRecording = recordedLoops.msDurationRecording / 1000;
        Debug.Log("recordedLoops.secondsDurationRecording = " + recordedLoops.secondsDurationRecording);

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
            // Set state.
            ApplicationProperties.State = State.Recording;

            // Play a beep sound to give feedback that a recording has started.
            GetComponent<AudioSource>().PlayOneShot(beepSound); 

            // Set the button image to show that a recording is in progress.
            currentRecButtonSprite.SetToRecInProgSprite1();
            currentRecButtonSprite.UpdateRecordingStatus(true);

            int lengthOfRecording = (int)recordedLoops.secondsDurationRecording;
            recordedLoops.secondsDurationRecording = lengthOfRecording;
            int lengthToRecord = lengthOfRecording + 1; // One more second, because Unity can not record time described by decimals.

            Debug.Log("Start to record.");

            // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource.  
            audioSource.clip = Microphone.Start(null, false, lengthToRecord, maxFreq);

            // This loop will run for 0.5s (48000/2 samples, 48000 = Samplerate). Needed to give a mobile phone time to load in the microphone.
            while (!(Microphone.GetPosition(null) > LENGTH_OF_DELAY_IN_SAMPLES)) { }
            // This code also adds an annoying delay to the circle bar when the record-button is pressed.
            // This needs to be solved somehow.
            // Tror detta stoppa applikationen helt tills mikrofonen är redo att spela in
            // Kanske kan lösa detta genom att spela in hela tiden i bakgrunden
            // Men enbart börja spara när player klickar på spela in knappen, dvs invoke kallas
            // när recordknappen tryckts på. Och sedan kapar man så mycket samples
            // efter som går utöver 4.136
            //while (Microphone.GetPosition(null) <= 0) ; Vet inte vad denna kod gör.

            numRecordButtonClicked++; // Keep track of the number of recordings.

            Invoke("SaveRecording", lengthOfRecording); // When the time of the recording has elapsed, save.

            Debug.Log("Recorded waiting to save.");
        }
        else // No microphone connected.
        {
            Debug.Log("No microphone connected!");
        }
    }

    // Saves the data recorded by the microphone. Called from the "StartRecording" function.
    void SaveRecording()
    {
        // Update state.
        ApplicationProperties.State = State.RecordingOver;

        currentRecButtonSprite.UpdateRecordingStatus(false);

        // Stop the audio recording if it hasn't already been stopped. 
        Microphone.End(null); 

        int indexOfRecording = numRecordButtonClicked - 1;
        Debug.Log("indexOfRecording = " + numRecordButtonClicked);
        recordedLoops.numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels; // In samples/indices.
        recordedLoops.numChannels = audioSource.clip.channels;

        int sizeOfRecording = (int) recordedLoops.numSamplesInRecording; // Keep the same length of every recording.
        float[] fullRecording = new float[sizeOfRecording];
        audioSource.clip.GetData(fullRecording, 0); // Get the data of the recording from the buffer.        

        // Exempel: Se nedan, om 1s = 48000 samples, så är 0.6s = 28800
        // 4.174 är 2 bars i 115bpm
        //  4 * 48000 + 0.174 * 48000 = 200352 samples totalt
        // 4 * 48000 =  192000 samples

        // Compensate for when a bpm when the total time in seconds is a decimal.
        // TODO: Ta bort hårdkodat, fixa så anpassar ifall presetlooparna byts ut. (Behövs inte just nu)
        int compensationFor116bpm = 6528 - 1; // 0.146 * SampleRate
        Debug.Log("compensationFor116bpm = " + compensationFor116bpm);

        // The index of where the part of the recording, that we want, starts.
        int startIndex = LENGTH_OF_DELAY_IN_SAMPLES - 1;

        // Remove 1s (48000 samples) from the recording, since the mic recorded 1s longer to give the mobile time to load the microphone.
        int amountToRemoveBecauseOfDelayLength = 2 * LENGTH_OF_DELAY_IN_SAMPLES - 1;
        int totalLengthToRetrieve = fullRecording.Length - amountToRemoveBecauseOfDelayLength + compensationFor116bpm;

        // Array to save the part of the recording to.
        float[] tempSamples = new float[totalLengthToRetrieve];
        Debug.Log("||||||||||||||||||||<>>>>>>>>>>>>>>>>>>>>>> BEFORE: tempSamples.Length = " + tempSamples.Length);
        Debug.Log("totalLengthToRetrieve = " + totalLengthToRetrieve);

        // Retrieve the part of the recording we want.
        System.Array.Copy(fullRecording, startIndex, tempSamples, 0, totalLengthToRetrieve); // Extract the recording starting at 0.5s and getting rid of the last 0.5s.
        //System.Array.Copy(fullRecording, 0, tempSamples, 0, tempSamples.Length);
        Debug.Log("||||||||||||||||||||<>>>>>>>>>>>>>>>>>>>>>> AFTER: tempSamples.Length = " + tempSamples.Length + ", should be = 198528");

        // Get RMS-value.
        float sum = 0;
        for (int i = 0; i < tempSamples.Length; i++)
            sum += tempSamples[i] * tempSamples[i]; // Sum squared samples.

        float rmsValue = Mathf.Sqrt(sum / tempSamples.Length); // Rms = square root of average.

        float silentThreshold = 0.01f;
        if (rmsValue > silentThreshold) // Don't send the recording for processing if it is too quiet.
        {
            // Update state.
            ApplicationProperties.State = State.ProcessingAudio;

            recordedLoops.silentRecording = false;

            // Apply high and low pass filter.
            tempSamples = HelperFunctions.ApplyHighPassFilter(tempSamples);
            tempSamples = HelperFunctions.ApplyLowPassFilter(tempSamples);

            // Quantize the recording.
            tempSamples = recordedLoops.QuantizeRecording(tempSamples);
            Debug.Log("In MicrophoneCapture, Quantization done");

            // Take care of case when no trimmed segments were saved in during the Quantization.
            if(ApplicationProperties.State == State.SilentInQuantization)
            {
                ApplicationProperties.State = State.SilentRecording;
            }
            else // Not silent, so normalize it.
            {
                tempSamples = HelperFunctions.Normalize(tempSamples);
                recordedLoops.silentRecording = false;
                Debug.Log("Normalizing recording because it wasn't silent.");

                // Update state.
                ApplicationProperties.State = State.FinishedProcessing;
            }

            //if (!recordedLoops.silentRecording)
            //{
            //    tempSamples = HelperFunctions.Normalize(tempSamples);
            //    recordedLoops.silentRecording = false;
            //    Debug.Log("Normalizing recording because it wasn't silent.");

            //    // Update state.
            //    ApplicationProperties.State = State.FinishedProcessing;
            //}            
        }
        else // It is a silent recording.
        {
            ApplicationProperties.State = State.SilentRecording;
        }

        // Save the recording.
        if (ApplicationProperties.State != State.SilentRecording)
        {
            recordedLoops.SetRecording(indexOfRecording, tempSamples);
            Debug.Log("Saved recording in MicrophoneCapture script.");

            ApplicationProperties.State = State.SavedRecording;
        }

        // Set to default state.
        if (ApplicationProperties.State == State.SavedRecording)
            ApplicationProperties.State = State.Default;
    }
}