using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Filters : ScriptableObject {

    [Range(5f, 1500)]
    int highPassCutoffFrequency = 8000; // 75

    [Range(100f, 5000)]
    int lowPassCutoffFrequency = 200; // 10000, allt under

    [Range(0.1f, 1.41421f)]
    float resonance = 0.2f;

    float a1, a2, a3, b1, b2;
    float in_1 = 0f, in_2 = 0f, out_1 = 0f, out_2 = 0f;

    private void SetUpCoefficients(int cutoffFrequency)
    {
        float c = Mathf.Tan(Mathf.PI * cutoffFrequency / AudioSettings.outputSampleRate);
        a1 = 1.0f / (1.0f + resonance * c + c * c);
        a2 = -2f * a1;
        a3 = a1;
        b1 = 2.0f * (c * c - 1.0f) * a1;
        b2 = (1.0f - resonance * c + c * c) * a1;
    }

    public float[] ApplyHighPassFilter(float[] recording)
    {
        SetUpCoefficients(highPassCutoffFrequency);

        for (int i = 0; i < recording.Length; i++)
        {
            float aux = recording[i];
            recording[i] = a1 * recording[i] + a2 * in_1 + a3 * in_2 - b1 * out_1 - b2 * out_2;
            in_2 = in_1;
            in_1 = aux;
            out_2 = out_1;
            out_1 = recording[i];
        }

        return recording;
    }

    public float[] ApplyLowPassFilter(float[] recording)
    {
        SetUpCoefficients(lowPassCutoffFrequency);

        for (int i = 0; i < recording.Length; i++)
        {
            float aux = recording[i];
            recording[i] = a1 * recording[i] + a2 * in_1 + a3 * in_2 - b1 * out_1 - b2 * out_2;
            in_2 = in_1;
            in_1 = aux;
            out_2 = out_1;
            out_1 = recording[i];
        }

        return recording;
    }
}
