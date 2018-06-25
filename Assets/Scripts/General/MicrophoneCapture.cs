using System.Collections;
using UnityEngine;

/// <summary>
/// Records from the microphone, applies processing on the recording
/// and saves the recording in the scriptable object "RecordedLoops".
/// </summary>

namespace Musikverkstaden
{
    [RequireComponent(typeof(AudioSource))]
    public class MicrophoneCapture : MonoBehaviour
    {
        // A boolean that flags whether there's a connected microphone.
        private bool micConnected = false;

        //The maximum and minimum available recording frequencies 
        private int minFreq, maxFreq;

        private AudioSource audioSource;
        private int numRecordButtonClicked = 0;

        // Pixel offset used to place new buttons below existing ones.
        private const int pixelsButtonOffset = 55;

        float durationOfLoop;
        public AudioClip beepSound;

        // 48000 = one beat.
        public const int LENGTH_OF_DELAY_IN_SAMPLES = 48000 / 2;

        // Set as "true" to test the UI. It will add a fictional empty recording.
        private bool debugging = false;

        // To make it show up in the inspector.
        [SerializeField]
        private RecordedLoops recordedLoops;
        [SerializeField]
        private CurrentRecButtonSprite currentRecButtonSprite;

        // Assign the method to run during an event.
        private void AssignMethodToRunDuringAnEvent()
        {
            // Subscribing to the event by adding the method to the "publisher".
            ApplicationProperties.changeEvent += MethodToRun;
        }

        // The method which will run when an event is triggered.
        void MethodToRun(State state)
        {
            Debug.Log("New state in MicCapture = " + state);

            if (state == State.SilentRecording && !debugging)
            {
                // Decrease number of recordings.
                numRecordButtonClicked--;
            }
        }

        void Start()
        {
            AssignMethodToRunDuringAnEvent();

            // Init the scriptable object "RecordedLoops".
            recordedLoops.recordings = new System.Collections.Generic.List<float[]>();
            recordedLoops.numSavedRecordings = 0;

            // Calculate how many milliseconds in one beat.
            int msInAMinute = 60000;
            float msInOneBeat = msInAMinute / ApplicationProperties.BPM;

            // Calculate how many samples/indices corresponds to one second.
            recordedLoops.msDurationRecording = msInOneBeat * ApplicationProperties.NUM_BEATS_PER_LOOP;
            recordedLoops.secondsDurationRecording = recordedLoops.msDurationRecording / 1000;
            //Debug.Log("recordedLoops.secondsDurationRecording = " + recordedLoops.secondsDurationRecording);

            // Check if there is at least one microphone connected.  
            if (Microphone.devices.Length <= 0)
            {
                // Throw a warning message at the console if there isn't.  
                Debug.Log("Microphone is not connected!");
            }
            else // At least one microphone is present.  
            {
                micConnected = true;
                maxFreq = 48000;
                recordedLoops.sampleRate = maxFreq;
                audioSource = this.GetComponent<AudioSource>();
            }
        }

        // Starts the recording.
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

                // One more second, because Unity can not record time described by decimals.
                int lengthToRecord = lengthOfRecording + 1;

                //Debug.Log("Start to record.");

                // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource.  
                audioSource.clip = Microphone.Start(null, false, lengthToRecord, maxFreq);

                // Only record when the microphone is ready. Prevents the thump sound on mobile.
                while (!(Microphone.GetPosition(null) > 0)) { }

                // Keep track of the number of recordings.
                numRecordButtonClicked++;

                // When the time of the recording has elapsed, save.
                Invoke("SaveRecording", lengthOfRecording);

                //Debug.Log("Recorded waiting to save.");
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

            // Update state.
            ApplicationProperties.State = State.ProcessingAudio;

            // In samples/indices.
            recordedLoops.numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels;
            recordedLoops.numChannels = audioSource.clip.channels;

            // Keep the same length of every recording.
            int sizeOfRecording = (int)recordedLoops.numSamplesInRecording;
            float[] fullRecording = new float[sizeOfRecording];

            // Get the data of the recording from the buffer. 
            audioSource.clip.GetData(fullRecording, 0);

            // Compensate for when a bpm when the total time in seconds is a decimal.
            // TODO: Fix hard coded code below in case the preset loops are switched. (Not needed now).
            int compensationFor116bpm = 6528 - 1; // 0.146 * SampleRate
                                                  //Debug.Log("compensationFor116bpm = " + compensationFor116bpm);

            // The index of where the part of the recording, that we want, starts.
            int startIndex = LENGTH_OF_DELAY_IN_SAMPLES - 1;

            // Remove 1s (48000 samples) from the recording, since the mic recorded 1s longer to give the mobile time to load the microphone.
            int amountToRemoveBecauseOfDelayLength = 2 * LENGTH_OF_DELAY_IN_SAMPLES - 1;
            int totalLengthToRetrieve = fullRecording.Length - amountToRemoveBecauseOfDelayLength + compensationFor116bpm;

            // Array to save the part of the recording to.
            float[] tempSamples = new float[totalLengthToRetrieve];

            // Retrieve the part of the recording we want.
            // Extract the recording starting at 0.5s and getting rid of the last 0.5s.
            System.Array.Copy(fullRecording, startIndex, tempSamples, 0, totalLengthToRetrieve);

            // Get RMS-value.
            // Sum squared samples.
            float sum = 0;
            for (int i = 0; i < tempSamples.Length; i++)
                sum += tempSamples[i] * tempSamples[i];

            // Rms = square root of average.
            float rmsValue = Mathf.Sqrt(sum / tempSamples.Length);

            float silentThreshold = 0.01f;

            // Don't send the recording for processing if it is too quiet.
            if (rmsValue > silentThreshold && !debugging)
            {
                // Begin our heavy work in a coroutine.
                // Coroutine needed to not freezy the main thread
                // which freezes the GUI when processing being performed.
                StartCoroutine(YieldingWork(tempSamples));
            }
            else // It is a silent recording.
            {
                if (!debugging)
                    ApplicationProperties.State = State.SilentRecording;
            }
        }

        // Replace "tempSamples" to add a debug recording.
        private float[] GenerateDebugRecording(float[] tempSamples)
        {
            debugging = true;

            float[] tempDebugData = new float[tempSamples.Length];

            for (int i = 0; i < tempSamples.Length; i++)
            {
                tempDebugData[i] = 0.5f;
            }

            return tempDebugData;
        }

        // The processing which is performed and called from the coroutine.
        IEnumerator YieldingWork(float[] tempSamples)
        {
            bool workDone = false;

            while (!workDone)
            {
                // Perform processing during 2.4 seconds.
                float delay = 2.4f;
                yield return new WaitForSeconds(delay);

                // Update state.
                ApplicationProperties.State = State.ProcessingAudio;

                recordedLoops.silentRecording = false;

                // Apply high and low pass filter.
                tempSamples = HelperFunctions.ApplyHighPassFilter(tempSamples);
                tempSamples = HelperFunctions.ApplyLowPassFilter(tempSamples);

                // Quantize the recording.
                tempSamples = recordedLoops.QuantizeRecording(tempSamples);
                //Debug.Log("In MicrophoneCapture, Quantization done");

                // Take care of case when no trimmed segments were saved in during the Quantization.
                if (ApplicationProperties.State == State.SilentInQuantization)
                {
                    ApplicationProperties.State = State.SilentRecording;

                    // Trigger so the dot animation is disabled.
                    ApplicationProperties.State = State.FinishedProcessing;
                }
                else // Not silent, so normalize it.
                {
                    tempSamples = HelperFunctions.Normalize(tempSamples);
                    recordedLoops.silentRecording = false;
                    //Debug.Log("Normalizing recording because it wasn't silent.");
                }

                // Save the recording.
                if (ApplicationProperties.State != State.SilentRecording || debugging)
                {
                    // Generates a debug recording if the debugging mode is enabled.
                    if (debugging)
                        tempSamples = GenerateDebugRecording(tempSamples);

                    recordedLoops.recordings.Add(tempSamples);
                    Debug.Log("Saved recording in MicrophoneCapture script.");

                    // Update state.
                    ApplicationProperties.State = State.FinishedProcessing;

                    ApplicationProperties.State = State.SavedRecording;
                }

                // Set to default state.
                if (ApplicationProperties.State == State.SavedRecording)
                    ApplicationProperties.State = State.Default;

                workDone = true; // Exit the loop.
            }
        }

    }
}