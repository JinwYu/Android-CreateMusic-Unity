using UnityEngine;

/// <summary>
/// Stores every recorded loop and some information about the loops.
/// </summary>

// Allows to create the object in the project view.
namespace Musikverkstaden
{
    [CreateAssetMenu]
    public class RecordedLoops : ScriptableObject
    {

        [SerializeField]
        private Filters filters;
        [SerializeField]
        private Quantization quantization;

        public System.Collections.Generic.List<float[]> recordings;
        public int sampleRate = 48000;
        public float msDurationRecording;
        public float secondsDurationRecording;
        public float numSamplesInRecording;
        public float numChannels;
        public bool silentRecording;
        public int numSavedRecordings;

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
            for (int i = 0; i < recording.Length; i++)
            {
                float currentValue = System.Math.Abs(recording[i]);
                if (currentValue > maxValue)
                    maxValue = currentValue;
            }

            // Normalize the sound.
            for (int i = 0; i < recording.Length; i++)
                recording[i] = recording[i] / maxValue;

            return recording;
        }
    }
}
