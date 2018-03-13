using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class has methods that apply a high- or low pass filter.
// Code borrowed from "Sound Filters and Effects" by 3y3net, version 1.6, from Unity's assets store. 

[CreateAssetMenu]
public class Filters : ScriptableObject {

    [Range(5f, 1500)]
    int highPassCutoffFrequency = 60; // 75, Frequencies over the threshold will be audible.

    [Range(100f, 17000)] //[Range(100f, 5000)] 
    int lowPassCutoffFrequency = 15000; // 10000, Frequencies under the threshold will be audible

    [Range(0.1f, 1.41421f)]
    float resonance = 0.2f;

    float a1, a2, a3, b1, b2;
    float in_1 = 0f, in_2 = 0f, out_1 = 0f, out_2 = 0f;

    private void SetUpCoefficients(int cutoffFrequency, bool isItHighPassFilter)
    {
        if (isItHighPassFilter)
        {
            float c = Mathf.Tan(Mathf.PI * cutoffFrequency / AudioSettings.outputSampleRate);
            a1 = 1.0f / (1.0f + resonance * c + c * c);
            a2 = -2f * a1;
            a3 = a1;
            b1 = 2.0f * (c * c - 1.0f) * a1;
            b2 = (1.0f - resonance * c + c * c) * a1;
        }
        else
        {
            float c = 1.0f / Mathf.Tan(Mathf.PI * cutoffFrequency / AudioSettings.outputSampleRate);
            a1 = 1.0f / (1.0f + resonance * c + c * c);
            a2 = 2f * a1;
            a3 = a1;
            b1 = 2.0f * (1.0f - c * c) * a1;
            b2 = (1.0f - resonance * c + c * c) * a1;
        }
        
    }

    public float[] ApplyHighPassFilter(float[] recording)
    {
        SetUpCoefficients(highPassCutoffFrequency, true);

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
        SetUpCoefficients(lowPassCutoffFrequency, false);

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
