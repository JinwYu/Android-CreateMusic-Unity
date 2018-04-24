using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class PlayMultipleAudioSources : MonoBehaviour
{
    public float bpm = 120.0F;
    public int numBeatsPerSegment;
    public AudioClip[] clips = new AudioClip[2]; // TODO: Ändra denna för implementation av flera pre-recorded clips.
    private double nextEventTime;
    private int flip = 0;
    private AudioSource[] audioSources = new AudioSource[RecordedLoops.NUM_POSSIBLE_RECORDINGS + RecordedLoops.NUM_PRESET_LOOPS];
    private bool running = false;

    private float[][] recordings;
    private int numRecordings;

    [SerializeField] // To make it show up in the inspector.
    private RecordedLoops recordedLoops;
    [SerializeField]
    private PlayOrStopSprite playOrStopSprite;

    public AudioMixer audioMixer; // Assign in the inspector.
    AudioMixerGroup[] audioMixerGroups;

    private bool playLoop = false;
    int indexOfLoopToPlay;

    void Start()
    {
        numBeatsPerSegment = recordedLoops.numBeatsPerSegment;

        int idx = 0;
        // Add multiple AudioSource components to allow simultaneous playback later.
        while (idx < RecordedLoops.NUM_POSSIBLE_RECORDINGS + RecordedLoops.NUM_PRESET_LOOPS)
        {
            GameObject child = new GameObject("Loop" + (idx+1));
            child.transform.parent = gameObject.transform;
            audioSources[idx] = child.AddComponent<AudioSource>();

            // Debug, lägger bara till en audio filter på den första audiosource, sen ta data från den är planen.
            // Skräpkod för att lägga unitys egna filter på samma sätt som lägger till audiosources ovan.
            //if (idx == 0)
            //{
            //    child.AddComponent<AudioHighPassFilter>();
            //    child.GetComponent<AudioHighPassFilter>().cutoffFrequency = 2000;
            //}

            idx++;
        }
        nextEventTime = AudioSettings.dspTime + 2.0F;

        // Get all channels that are children to "loop1", which is the parent.
        audioMixerGroups = new AudioMixerGroup[RecordedLoops.NUM_POSSIBLE_RECORDINGS + RecordedLoops.NUM_PRESET_LOOPS];
        audioMixerGroups = audioMixer.FindMatchingGroups("loop1");

        //running = true;

        // Play pre-recorded FL studio loops. Assign loops by dragging the sounds from assets in Unity inspector.
        audioSources[0].clip = clips[0];
        audioSources[1].clip = clips[1];    
    }

    // Garbage quick code, REFACTOR
    //void OnGUI()
    //{
        //// Play all recorded loops at the same start and simultaneously.
        //if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 55 * 4, 200, 50), "Play recorded loops"))
        //{
            //int numSamples = (int) (recordedLoops.sampleRate * recordedLoops.secondsDurationRecording);

            //// Play all of the "AudioSources".
            //for(int idx = 0; idx < RecordedLoops.NUM_POSSIBLE_RECORDINGS; idx++)
            //{
            //    float[] recordingToPlay = recordedLoops.recordings[idx];
            //    //int lengthOfRecordingToPlay = (int) recordedLoops.msDurationRecording; // Now all of the loops will have the same length.
            //    int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording;

            //    audioSources[idx].clip = AudioClip.Create("recorded samples", numSamplesInRecording, 1, recordedLoops.sampleRate, false);
            //    audioSources[idx].clip.SetData(recordingToPlay, 0);
            //    audioSources[idx].loop = true;


            //    // TODO: Ta bort ifsatsen, det är en kontroll nu för vi har bara 2 kanaler i mixern.
            //    if(idx <= 1)
            //    {
            //        audioSources[idx].outputAudioMixerGroup = audioMixerGroups[idx];
            //    }                

            //    audioSources[idx].Play();
            //}

            // spara förhoppningsvis det filtrerade ljudet
            //int sizeOfRecording = audioSources[0].clip.samples; // Keep the same length of every recording.

            //float[] tempSamples = new float[sizeOfRecording];
            //audioSources[0].clip.GetData(tempSamples, 0); // Get the data of the recording from the buffer.

            
            //audioSources[1].clip = AudioClip.Create("recorded samples", sizeOfRecording, 2, 44100, false);
            //audioSources[1].clip.SetData(tempSamples, 0);
            //audioSources[1].loop = true;
            //audioSources[1].Play();
        //}
    //}

    public void PlayLoop(int index)
    {
        if (audioSources[index].isPlaying)
        {
            playLoop = false;
            audioSources[index].Stop();

            // Show the play button.
            playOrStopSprite.SetIfButtonShouldShowPlaySprite(index, true);            
        }
        else
        {
            audioSources[index].loop = true;
            playLoop = true;
            indexOfLoopToPlay = index;

            // If one of the recorded loops should be played.
            if (indexOfLoopToPlay > RecordedLoops.NUM_PRESET_LOOPS - 1)
                AssignRecordingToAudioSource(index);

            // Show the stop button.
            playOrStopSprite.SetIfButtonShouldShowPlaySprite(index, false);
        }
    }

    private void AssignRecordingToAudioSource(int index)
    {
        int numSamples = (int)(recordedLoops.sampleRate * recordedLoops.secondsDurationRecording);

        int temp = indexOfLoopToPlay - RecordedLoops.NUM_PRESET_LOOPS;
        Debug.Log("Index of recording to play = " + temp);
        float[] recordingToPlay = recordedLoops.recordings[indexOfLoopToPlay - RecordedLoops.NUM_PRESET_LOOPS]; // Subtraction because "recordings" in RecordedLoops doesn't have the preset loops.
        int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording;

        //Debug.Log("samplerate in assignrecordingtoaudiosource = " + recordedLoops.sampleRate);
        audioSources[index].clip = AudioClip.Create("recorded samples", recordingToPlay.Length, (int)recordedLoops.numChannels, recordedLoops.sampleRate, false);
        audioSources[index].clip.SetData(recordingToPlay, 0);
        audioSources[index].loop = true;


        // TODO: Ta bort ifsatsen, det är en kontroll nu för vi har bara 2 kanaler i Audio mixern i Unity Editor.
        if (indexOfLoopToPlay <= 3) //1
        {
            // Är index 0 likamed "loop1" channeln?
            audioSources[index].outputAudioMixerGroup = audioMixerGroups[0]; // audioMixerGroups[index];
        } 
    }

    void Update()
    {
        //if (!running)
        //    return;

        double time = AudioSettings.dspTime;

        if (time + 1.0F > nextEventTime)
        {
            if (playLoop && !audioSources[indexOfLoopToPlay].isPlaying)
            {
                audioSources[indexOfLoopToPlay].PlayScheduled(nextEventTime);
                //audioSources[indexOfLoopToPlay].Play();
                Debug.Log("PLAY SCHEDULED LOOP, index = " + indexOfLoopToPlay);
            }

            nextEventTime += 60.0F / bpm * numBeatsPerSegment / 4; // Dela 16 för att starta tidigare än en hel bar efter vid playtryckning.

            //audioSources[0].PlayScheduled(nextEventTime);
            //audioSources[1].PlayScheduled(nextEventTime);
            //nextEventTime += 60.0F / bpm * numBeatsPerSegment;

            //audioSources[flip].clip = clips[flip];
            //audioSources[flip].PlayScheduled(nextEventTime);
            //Debug.Log("Scheduled source " + flip + " to start at time " + nextEventTime);
            //nextEventTime += 60.0F / bpm * numBeatsPerSegment;
            //flip = 1 - flip;
        }

        // Ta presetlooparna, kolla när de spelas
        if (audioSources[0].isPlaying)
        {
            //Debug.Log("audioSources[0].time = " + audioSources[0].time);
            //Debug.Log("audioSources[0].timeSamples = " + audioSources[0].timeSamples);
        }

        // När recordknappen trycks på, ska den kalla på en funktion som assignats i inspectorn
        // Denna funktion ska ta .timeSamples, då knappen trycktes, kanske ta hänsyn till att delay i MicCapture-scriptet
        // Sen använda detta värde som index i PhaseInversion-scriptet.        
    }

    public int GetCurrentTimeInSamplesForPresetLoops(int presetLoopIndex)
    {
        return audioSources[presetLoopIndex].timeSamples + (48000/2); // 48000/2 is the delay the mic has when recording.
    }
}
