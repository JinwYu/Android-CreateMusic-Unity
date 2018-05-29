using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperFunctions {

    // Get the RMS value of a recording.
    public static float GetRMS(float[] temp)
    {
        float sum = 0;

        for (int i = 0; i < temp.Length; i++)
            sum += temp[i] * temp[i]; // Sum squared samples.

        float rmsValue = Mathf.Sqrt(sum / temp.Length); // Rms = square root of average.

        return sum;
    }

    // Normalize an audio array.
    public static float[] Normalize(float[] temp)
    {
        // Find the max value.
        float maxValue = 0;
        for (int i = 0; i < temp.Length; i++)
        {
            float currentValue = System.Math.Abs(temp[i]);
            if (currentValue > maxValue)
                maxValue = currentValue;
        }

        // Normalize the sound.
        for (int i = 0; i < temp.Length; i++)
            temp[i] = temp[i] / maxValue;

        return temp;
    }

    public static float[] ApplyHighPassFilter(float[] recording)
    {
        Filters filters = new Filters();
        return filters.ApplyHighPassFilter(recording);
    }

    public static float[] ApplyLowPassFilter(float[] recording)
    {
        Filters filters = new Filters();
        return filters.ApplyLowPassFilter(recording);
    }

    // TODO: Den här koden fungerar inte, försök fixa så quantization kallas här.
    //public static float[] QuantizeRecording(float[] recording)
    //{
    //    Quantization quantization = new Quantization();
    //    return quantization.Quantize(recording);
    //}


}
