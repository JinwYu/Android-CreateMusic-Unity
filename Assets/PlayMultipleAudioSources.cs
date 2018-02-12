using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class PlayMultipleAudioSources : MonoBehaviour
{
    public float bpm = 140.0F;
    public int numBeatsPerSegment = 16;
    public AudioClip[] clips = new AudioClip[2];
    private double nextEventTime;
    private int flip = 0;
    private AudioSource[] audioSources = new AudioSource[2];
    private bool running = false;

    private float[][] recordings;
    private int numRecordings;

    void Start()
    {
        int i = 0;
        while (i < 2)
        {
            GameObject child = new GameObject("Player");
            child.transform.parent = gameObject.transform;
            audioSources[i] = child.AddComponent<AudioSource>();
            i++;
        }
        nextEventTime = AudioSettings.dspTime + 2.0F;
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
        if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 55 * 4, 200, 50), "Play recorded loops"))
        {
            GameObject recordMicrophone = GameObject.Find("Record Microphone");
            MicrophoneCapture microphoneCapture = recordMicrophone.GetComponent<MicrophoneCapture>();
            recordings = microphoneCapture.recordings;
            numRecordings = 4; //microphoneCapture.NUM_POSSIBLE_RECORDINGS;

            int length = recordings[0].Length;
            audioSources[0].clip = AudioClip.Create("recorded samples", length, 1, 44100, false);
            audioSources[0].clip.SetData(recordings[0], 0);
            audioSources[0].loop = true;
            audioSources[0].Play();

            // Borde typ matcha length på båda för att samma längd på loopen iaf.

            int length1 = recordings[1].Length;
            audioSources[1].clip = AudioClip.Create("recorded samples", length1, 1, 44100, false);
            audioSources[1].clip.SetData(recordings[1], 0);
            audioSources[1].loop = true;
            audioSources[1].Play();

            //Debug.Log("should play recorded sounds now");
        }
    }





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

