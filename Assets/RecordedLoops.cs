using UnityEngine;

[CreateAssetMenu] // Allows to create the object in the project view.
public class RecordedLoops : ScriptableObject {

    [SerializeField]
    private Filters filters;
    [SerializeField]
    private Quantization quantization;

    public float[][] recordings;
    public const int NUM_POSSIBLE_RECORDINGS = 6;
    public const int NUM_PRESET_LOOPS = 2;
    public int bpm = 120; // Beats per minute (tempo).
    public int sampleRate = 44100;
    public int numBeatsPerSegment = 8;
    public float msDurationRecording;
    public float secondsDurationRecording;
    public float numSamplesInRecording;
    public float numChannels;
    

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

    public float[] ApplyHighPassFilter(float[] recording)
    {
        return filters.ApplyHighPassFilter(recording);
    }

    public float[] ApplyLowPassFilter(float[] recording)
    {
        return filters.ApplyLowPassFilter(recording);
    }

    public float[] QuantizeRecording(float[] recording)
    {
        return quantization.Quantize(recording);
    }

    public float[] Normalize(float[] recording)
    {
        // Find the max value.
        float maxValue = 0;
        for(int i = 0; i < recording.Length; i++)
        {
            float currentValue = System.Math.Abs(recording[i]);
            if (currentValue > maxValue)
                maxValue = currentValue;
        }
        
        // Normalize the sound.
        for(int i = 0; i < recording.Length; i++)
            recording[i] = recording[i] / maxValue;   

        return recording;
    }


}
