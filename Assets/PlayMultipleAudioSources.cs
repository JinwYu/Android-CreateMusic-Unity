using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class PlayMultipleAudioSources : MonoBehaviour
{
    [SerializeField] // To make it show up in the inspector.
    private RecordedLoops recordedLoops;
    [SerializeField]
    private PlayOrStopSprite playOrStopSprite;
    [SerializeField]
    private PresetLoops presetLoops;

    private double nextEventTime;
    private int flip = 0;
    private AudioSource[] audioSources = new AudioSource[ApplicationProperties.NUM_POSSIBLE_RECORDINGS + ApplicationProperties.NUM_PRESET_LOOPS];

    private float[][] recordings;    

    public AudioMixer audioMixer; // Assign in the inspector.
    AudioMixerGroup[] audioMixerGroups;

    private bool playLoop = false;
    int indexOfLoopToPlay;
    private bool changedVolume = false;

    private void AssignMethodToRunDuringAnEvent()
    {
        ApplicationProperties.changeEvent += MethodToRun; // Subscribing to the event by adding the method to the "publisher".
    }

    void MethodToRun(State state)
    {
        //Debug.Log("New state in PlayMultipleAudioSources = " + state);

        if(state == State.Recording)
        {
            // If recording, lower the volume of the audio sources that are playing.
            ChangeVolumeForAudioSources(0.6f);
        }
        else
        {   
            // Else turn up the volume again if the app is in any other state.
            ChangeVolumeForAudioSources(1.0f);
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

        // Get all channels that are children to "loop1", which is the parent.
        audioMixerGroups = new AudioMixerGroup[ApplicationProperties.NUM_POSSIBLE_RECORDINGS + ApplicationProperties.NUM_PRESET_LOOPS];
        audioMixerGroups = audioMixer.FindMatchingGroups("loop1");

        // Play pre-recorded FL studio loops. Assign loops by dragging the sounds from assets in Unity inspector.  
        audioSources[0].clip = presetLoops.originalPresetLoops[0];
        audioSources[1].clip = presetLoops.originalPresetLoops[1];

        // Start playing all preset loops silently.
        audioSources[0].loop = true;
        audioSources[0].Play();
        audioSources[0].volume = 0.0f;
        audioSources[1].loop = true;
        audioSources[1].Play();
        audioSources[1].volume = 0.0f;
    }

    // Plays or stops a loop. Called when a button is clicked. 
    // Assigned in the inspector.
    public void PlayLoop(int index)
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

    private void AssignRecordingToAudioSource(int index)
    {
        int numSamples = (int)(recordedLoops.sampleRate * recordedLoops.secondsDurationRecording);

        int temp = indexOfLoopToPlay - ApplicationProperties.NUM_PRESET_LOOPS;
        Debug.Log("Index of recording to play = " + temp);
        float[] recordingToPlay = recordedLoops.recordings[indexOfLoopToPlay - ApplicationProperties.NUM_PRESET_LOOPS]; // Subtraction because "recordings" in RecordedLoops doesn't have the preset loops.
        int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording;

        audioSources[index].clip = AudioClip.Create("recorded samples", recordingToPlay.Length, (int)recordedLoops.numChannels, recordedLoops.sampleRate, false);
        audioSources[index].clip.SetData(recordingToPlay, 0);
        audioSources[index].loop = true;
    }

    private void ChangeVolumeForAudioSources(float volume)
    {
        changedVolume = true;

        // Check if any of the audio sources are playing.
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i].isPlaying && audioSources[i].volume > 0.1f) // Check with "0.1f" is there to not affect the preset loops volume if they're not audible.
            {
                audioSources[i].volume = volume;
            }
        }                                  
    }

    public void TogglePresetLoopVolume(int index)
    {
        if (audioSources[index].volume < 0.1f)
        {
            audioSources[index].volume = 1.0f;
            //Debug.Log("vol = 1.0f" + ", index = " + index);
        }
        else if(audioSources[index].volume > 0.9f)
        {
            //Debug.Log("vol = 0.0f" + ", index = " + index);
            audioSources[index].volume = 0.0f;
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
                        //Debug.Log("PLAY SCHEDULED LOOP, index = " + indexOfLoopToPlay);
                    }
                }
                else // Play immediately if no other source is playing.
                {
                    if (indexOfLoopToPlay == 0)
                    {
                        // Play both preset loops simultaneously.
                        audioSources[indexOfLoopToPlay].Play();
                        audioSources[indexOfLoopToPlay + 1].Play();
                        audioSources[indexOfLoopToPlay + 1].volume = 0.0f;
                    }
                    else if (indexOfLoopToPlay == 1)
                    {
                        audioSources[indexOfLoopToPlay].Play();
                        audioSources[indexOfLoopToPlay - 1].Play();
                        audioSources[indexOfLoopToPlay - 1].volume = 0.0f;
                    }
                    else
                    {
                        // Else play immediately.
                        audioSources[indexOfLoopToPlay].Play();
                    }
                }
            }

            nextEventTime += 60.0F / (float)ApplicationProperties.BPM * ApplicationProperties.NUM_BEATS_PER_LOOP / 16;// / 4; // Dela 16 för att starta tidigare än en hel bar efter vid playtryckning.
        }
    }
}
