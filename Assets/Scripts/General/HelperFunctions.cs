using UnityEngine;

/// <summary>
/// Class with helper functions: get RMS value, normalising and apply filters.
/// </summary>
public static class HelperFunctions {

    // Get the RMS value of a recording.
    public static float GetRMS(float[] temp)
    {
        float sum = 0;

        // Sum squared samples.
        for (int i = 0; i < temp.Length; i++)
            sum += temp[i] * temp[i]; 

        // Rms = square root of average.
        float rmsValue = Mathf.Sqrt(sum / temp.Length); 

        return rmsValue;
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

    // Apply highpass filter.
    public static float[] ApplyHighPassFilter(float[] recording)
    {
        Filters filters = new Filters();
        return filters.ApplyHighPassFilter(recording);
    }

    // Apply lowpassfilter.
    public static float[] ApplyLowPassFilter(float[] recording)
    {
        Filters filters = new Filters();
        return filters.ApplyLowPassFilter(recording);
    }
}
