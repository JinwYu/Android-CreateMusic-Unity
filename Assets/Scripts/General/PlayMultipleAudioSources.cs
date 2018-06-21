using UnityEngine;

/// <summary>
/// Handles the audio playback for the application.
/// Each loop is assigned to its own "AudioSource". 
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayMultipleAudioSources : MonoBehaviour
{
    [SerializeField]
    private RecordedLoops recordedLoops;
    [SerializeField]
    private PlayOrStopSprite playOrStopSprite;
    [SerializeField]
    private PresetLoops presetLoops;

    // Variables for calculating the correct timing to play a loop.
    private double nextEventTime;
    //private int flip = 0;

    // All of the AudioSources for the every loop.
    private AudioSource[] audioSources = new AudioSource[ApplicationProperties.NUM_POSSIBLE_RECORDINGS + ApplicationProperties.NUM_PRESET_LOOPS];

    // General variables for this class.
    private bool playLoop = false;
    int indexOfLoopToPlay;

    private void AssignMethodToRunDuringAnEvent()
    {
        // Subscribing to the event by adding the method to the "publisher".
        ApplicationProperties.changeEvent += MethodToRun; 
    }

    void MethodToRun(State state)
    {
        //Debug.Log("New state in PlayMultipleAudioSources = " + state);

        if(state == State.Recording)
        {
            // If recording, lower the volume of the audio sources that are playing.
            ChangeVolumeForAudioSources(ApplicationProperties.VOLUME_DURING_RECORDING_LEVEL);
        }
        else
        {   
            // Else turn up the volume again if the app is in any other state.
            ChangeVolumeForAudioSources(ApplicationProperties.DEFAULT_VOLUME_LEVEL);
        }
    }

    void Start()
    {
        AssignMethodToRunDuringAnEvent();

        int idx = 0;
        // Add multiple AudioSource components to allow simultaneous playback later.
        while (idx < ApplicationProperties.NUM_POSSIBLE_RECORDINGS + ApplicationProperties.NUM_PRESET_LOOPS)
        {
            GameObject child = new GameObject("Loop" + (idx+1));
            child.transform.parent = gameObject.transform;
            audioSources[idx] = child.AddComponent<AudioSource>();

            idx++;
        }
        nextEventTime = AudioSettings.dspTime + 2.0F;

        // Play pre-recorded FL studio loops. Assign loops by dragging the sounds from assets in Unity inspector.  
        for (int i = 0; i < ApplicationProperties.NUM_PRESET_LOOPS; i++)
            audioSources[i].clip = presetLoops.originalPresetLoops[i];

        // Lower the audio volume level for all recordings.
        for (int i = ApplicationProperties.NUM_PRESET_LOOPS; i < audioSources.Length; i++)
            audioSources[i].volume = ApplicationProperties.DEFAULT_VOLUME_LEVEL;

        // Start playing all preset loops silently.
        for(int i = 0; i < ApplicationProperties.NUM_PRESET_LOOPS; i++)
        {
            audioSources[i].loop = true;
            audioSources[i].Play();
            audioSources[i].volume = 0.0f;
        }
    }

    // Plays or stops a loop. Called when a button is clicked. 
    // Assigned in the inspector.
    public void PlayLoop(int index)
    {
        // Only run the code if it is not in the edit mode state.
        if (!(ApplicationProperties.State == State.EditMode))
        {
            if (audioSources[index].isPlaying)
            {
                // Show the play button.
                playOrStopSprite.SetIfButtonShouldShowPlaySprite(index, true);
                //Debug.Log("play green sprite true");

                playLoop = false;
                audioSources[index].Stop();
            }
            else
            {
                audioSources[index].loop = true;
                playLoop = true;
                indexOfLoopToPlay = index;

                // Show the stop button.
                playOrStopSprite.SetIfButtonShouldShowPlaySprite(index, false);
                //Debug.Log("Show stope sprite red");

                // If one of the recorded loops should be played.
                if (indexOfLoopToPlay > ApplicationProperties.NUM_PRESET_LOOPS - 1)
                    AssignRecordingToAudioSource(index);
            }
        }            
    }

    // Assigns a recording to an AudioSource.
    private void AssignRecordingToAudioSource(int index)
    {
        int temp = indexOfLoopToPlay - ApplicationProperties.NUM_PRESET_LOOPS;
        Debug.Log("Index of recording to play = " + temp);

        // Subtraction because "recordings" in RecordedLoops doesn't have the preset loops.
        float[] recordingToPlay = recordedLoops.recordings[indexOfLoopToPlay - ApplicationProperties.NUM_PRESET_LOOPS];

        audioSources[index].clip = AudioClip.Create("recorded samples", recordingToPlay.Length, (int)recordedLoops.numChannels, recordedLoops.sampleRate, false);
        audioSources[index].clip.SetData(recordingToPlay, 0);
        audioSources[index].loop = true;
    }

    private void ChangeVolumeForAudioSources(float volume)
    {
        // Check if any of the audio sources are playing.
        for (int i = 0; i < audioSources.Length; i++)
        {
            // Check with "0.1f" is there to not affect the preset loops volume if they're not audible.
            if (audioSources[i].isPlaying && audioSources[i].volume > 0.1f) 
            {
                audioSources[i].volume = volume;
            }
        }                                  
    }

    // Changes the volume of the preset loops. Called when a recording is in progress.
    public void TogglePresetLoopVolume(int index)
    {
        if (audioSources[index].volume < 0.1f)
        {
            audioSources[index].volume = ApplicationProperties.DEFAULT_VOLUME_LEVEL;
            //Debug.Log("vol = 1.0f" + ", index = " + index);
        }
        else if(audioSources[index].volume > (ApplicationProperties.DEFAULT_VOLUME_LEVEL - 0.2f))
        {
            //Debug.Log("vol = 0.0f" + ", index = " + index);
            audioSources[index].volume = 0.0f;
        }
    }

    // Stop the playback for all the audio sources that are playing.
    public void StopAllPlayback()
    {
        if(ApplicationProperties.State == State.EditMode)
        {
            for (int i = ApplicationProperties.NUM_PRESET_LOOPS; i < audioSources.Length; i++)
            {
                Debug.Log("Stopping audiosource with index = " + i);
                audioSources[i].Stop();
                indexOfLoopToPlay = 0;
            }
        }
    }

    void Update()
    {
        // Handle playback.
        double time = AudioSettings.dspTime;
        if (time + 1.0F > nextEventTime)
        {
            if (playLoop && !audioSources[indexOfLoopToPlay].isPlaying)
            {
                // Check if any of the other audio sources are playing.
                bool otherSourceIsPlaying = false;
                for(int i = 0; i < audioSources.Length; i++)
                {
                    if (audioSources[i].isPlaying)
                    {
                        otherSourceIsPlaying = true;
                        break;
                    }                        
                }

                if (otherSourceIsPlaying)
                {
                    // Handle special case, if it's the drums, play almost immediately.
                    if(indexOfLoopToPlay == 0)
                    {
                        // Play both preset loops simultaneously.
                        audioSources[indexOfLoopToPlay].PlayScheduled(nextEventTime / 64);
                        audioSources[indexOfLoopToPlay + 1].PlayScheduled(nextEventTime / 64);
                        audioSources[indexOfLoopToPlay + 1].volume = 0.0f;
                    }
                    else if(indexOfLoopToPlay == 1) // The guitar preset loop.
                    {
                        // Play both preset loops simultaneously.
                        audioSources[indexOfLoopToPlay].PlayScheduled(nextEventTime / 64);
                        audioSources[indexOfLoopToPlay - 1].PlayScheduled(nextEventTime / 64);
                        audioSources[indexOfLoopToPlay - 1].volume = 0.0f;
                    }
                    else
                    {
                        // Play the loop as usual with an event time.
                        audioSources[indexOfLoopToPlay].PlayScheduled(nextEventTime);
                        Debug.Log("PLAY SCHEDULED LOOP, index = " + indexOfLoopToPlay);
                    }
                }
                else // Play immediately if no other source is playing.
                {
                    // First preset loop.
                    if (indexOfLoopToPlay == 0)
                    {
                        // Play both preset loops simultaneously.
                        audioSources[indexOfLoopToPlay].Play();
                        audioSources[indexOfLoopToPlay + 1].Play();
                        audioSources[indexOfLoopToPlay + 1].volume = 0.0f;
                    }
                    else if (indexOfLoopToPlay == 1)
                    {
                        // Second preset loop.
                        audioSources[indexOfLoopToPlay].Play();
                        audioSources[indexOfLoopToPlay - 1].Play();
                        audioSources[indexOfLoopToPlay - 1].volume = 0.0f;
                    }
                    else
                    {
                        // Else play immediately.
                        audioSources[indexOfLoopToPlay].Play();
                        Debug.Log("Playing " + indexOfLoopToPlay + " immediately.");
                    }
                }
            }

            // Calculating the next interval to start a loop with the correct timing.
            // Divided by 16 to make the interval smaller. Can only divide by even numbers.
            nextEventTime += 60.0F / (float)ApplicationProperties.BPM * ApplicationProperties.NUM_BEATS_PER_LOOP / 16;
        }
    }
}
