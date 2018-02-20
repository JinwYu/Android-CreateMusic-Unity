using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu] // Allows to create the object in the project view.
public class RecordedLoops : ScriptableObject {

	public float[][] recordings;
    public int testing;
    public const int NUM_POSSIBLE_RECORDINGS = 4;
    public int bpm = 120; // Beats per minute (tempo).
    public int sampleRate = 44100;
    public int numBeatsPerSegment = 4;
    public float msDurationRecording;
    public float secondsDurationRecording;
    public float numSamplesInRecording;


    public void SetRecording(int index, float[] recordingToSet)
    {
        recordings[index] = recordingToSet;
    }

    // Kan vara onödig för man kan nå genom att skriva recordedLoops.recordings.
    // Får se om jag ska sätta recordings som private och enbart ha getters och setters.
    public float[] GetIndividualRecording(int index)
    {
        return recordings[index];
    }

    public float[][] GetAllRecordings()
    {
        return recordings;
    }


}
