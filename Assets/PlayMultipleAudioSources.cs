using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class PlayMultipleAudioSources : MonoBehaviour
{
    public float bpm = 140.0F;
    public int numBeatsPerSegment = 16;
    public AudioClip[] clips = new AudioClip[2]; // TODO: Ändra denna för implementation av flera pre-recorded clips.
    private double nextEventTime;
    private int flip = 0;
    private AudioSource[] audioSources = new AudioSource[RecordedLoops.NUM_POSSIBLE_RECORDINGS];
    private bool running = false;

    private float[][] recordings;
    private int numRecordings;

    [SerializeField] // To make it show up in the inspector.
    private RecordedLoops recordedLoops;

    public AudioMixer audioMixer; // Assign in the inspector.
    AudioMixerGroup[] audioMixerGroups;  

    void Start()
    {
        int idx = 0;

        // Add multiple AudioSource components to allow simultaneous playback later.
        while (idx < RecordedLoops.NUM_POSSIBLE_RECORDINGS)
        {
            GameObject child = new GameObject("Loop" + (idx+1));
            child.transform.parent = gameObject.transform;
            audioSources[idx] = child.AddComponent<AudioSource>();

            idx++;
        }
        nextEventTime = AudioSettings.dspTime + 2.0F;

        // Get all channels that are children to "loop1", which is the parent.
        audioMixerGroups = new AudioMixerGroup[RecordedLoops.NUM_POSSIBLE_RECORDINGS];
        audioMixerGroups = audioMixer.FindMatchingGroups("loop1"); 

        //running = true;

        // Play pre-recorded FL studio loops. Assign loops by dragging the sounds from assets in Unity inspector.
        //audioSources[0].clip = clips[0];
        //audioSources[0].loop = true;
        //audioSources[0].Play();

        //audioSources[1].clip = clips[1];
        //audioSources[1].loop = true;
        //audioSources[1].Play();     
    }

    // Garbage quick code, REFACTOR
    void OnGUI()
    {
        // Play all recorded loops at the same start and simultaneously.
        if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 55 * 4, 200, 50), "Play recorded loops"))
        {
            int numSamples = (int) (recordedLoops.sampleRate * recordedLoops.secondsDurationRecording);

            // Play all of the "AudioSources".
            for(int idx = 0; idx < RecordedLoops.NUM_POSSIBLE_RECORDINGS; idx++)
            {
                float[] recordingToPlay = recordedLoops.recordings[idx];
                //int lengthOfRecordingToPlay = (int) recordedLoops.msDurationRecording; // Now all of the loops will have the same length.
                int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording;

                audioSources[idx].clip = AudioClip.Create("recorded samples", numSamplesInRecording, 1, recordedLoops.sampleRate, false);
                audioSources[idx].clip.SetData(recordingToPlay, 0);
                audioSources[idx].loop = true;

                
                // TODO: Ta bort ifsatsen, det är en kontroll nu för vi har bara 2 kanaler i mixern.
                if(idx <= 1)
                {
                    audioSources[idx].outputAudioMixerGroup = audioMixerGroups[idx];
                }                

                audioSources[idx].Play();
            }
        }
    }






    // BRA KOD FÖR NÄR MAN SPELAT IN ETT KLIPP SOM PROCESSERATS OCH SOM SPELAS UPP VID RÄTT START
    //void Update()
    //{
    //    if (!running)
    //        return;

    //    double time = AudioSettings.dspTime;

    //    if (time + 1.0F > nextEventTime)
    //    {
    //        audioSources[0].PlayScheduled(nextEventTime);
    //        audioSources[1].PlayScheduled(nextEventTime);
    //        nextEventTime += 60.0F / bpm * numBeatsPerSegment;

    //        //audioSources[flip].clip = clips[flip];
    //        //audioSources[flip].PlayScheduled(nextEventTime);
    //        //Debug.Log("Scheduled source " + flip + " to start at time " + nextEventTime);
    //        //nextEventTime += 60.0F / bpm * numBeatsPerSegment;
    //        //flip = 1 - flip;
    //    }
    //}
}


// MISC
/*
 * GameObject.Find("ThePlayer").GetComponent<PlayerScript>().Health -= 10.0f;;
 * 
 * //Kortare kod för access är GameObject.Find("ThePlayer").GetComponent<PlayerScript>().Health -= 10.0f;;
            //GameObject recordMicrophone = GameObject.Find("Record Microphone");
            //MicrophoneCapture microphoneCapture = recordMicrophone.GetComponent<MicrophoneCapture>();
            //recordings = microphoneCapture.recordings;
            //numRecordings = 4; //microphoneCapture.NUM_POSSIBLE_RECORDINGS;
 * 
 */
