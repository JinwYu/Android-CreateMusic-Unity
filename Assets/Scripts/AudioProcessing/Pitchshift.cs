using UnityEngine;

/// <summary>
/// This class takes an audio segment as input, finds which western musical note (semitone) the segment's
/// frequency is closest to and then it pitchshifts the segment to that note's frequency using 
/// a phase vocoder.
/// </summary>
public class Pitchshift
{
    private float[] semitoneIntervals;

    // Pitchshifts an audio segment to the frequency of the closest musical note.
    public float[] PitchshiftSegment(float[] segment)
    {
        // Find the frequency of the segment.
        float freqHighestFreqWins = FindFreqWithHighestFreqWins(segment);

        // Generate musical notes to compare the segment's frequency with.
        GenerateMusicalNoteFreqs();

        float soundFreq = freqHighestFreqWins;
        int indexOfSemitoneToShiftTo = 0;

        // Find the index for the semitone that the segment's frequency is closest to.
        for (int i = 1; i < semitoneIntervals.Length; i++)
        {
            float prevNoteFreq = semitoneIntervals[i - 1];
            float currentNoteFreq = semitoneIntervals[i];

            // If within two musical note frequencies.
            if (prevNoteFreq < soundFreq && soundFreq < currentNoteFreq)
            {
                float prevDiff = System.Math.Abs(soundFreq - prevNoteFreq);
                float currentDiff = System.Math.Abs(currentNoteFreq - soundFreq);

                // Closer to the the current, else closer to the left.
                if (prevDiff > currentDiff) 
                {
                    indexOfSemitoneToShiftTo = i;
                    break;
                }
                else 
                {
                    indexOfSemitoneToShiftTo = i - 1;
                    break;
                }
            }
        }

        // DEBUG: To control that the correct semitone has been chosen.
        /*
        // Determine which type of note the closest frequency has.
        // If the index is over 12, then it is not in the first octave.
        double semitoneNumID;
        if (indexOfSemitoneToShiftTo + 1 >= 12)
        {
            // Modulus with "12" because an octave has 12 semitones.
            // The first if statement handles the specials case when it is a "B" note.
            // The "remainder" from the modulus is the musical note.
            if ((indexOfSemitoneToShiftTo + 1) % 12 == 0) 
                semitoneNumID = 12;
            else
                semitoneNumID = indexOfSemitoneToShiftTo % 12;
        }
        else
        {
            // If it is in the first octave.
            semitoneNumID = indexOfSemitoneToShiftTo + 1;
        }

        Debug.Log("indexOfSemitoneToShiftTo = " + indexOfSemitoneToShiftTo);
        Debug.Log("semitoneNumID = " + semitoneNumID);
        */

        // Calculate the scale factor to scale from the initial frequency to the desired frequency.
        // Here is an example:
        // 2^1/12 * FreqInitial = FreqFinal
        // 2^1/12 = FreqFinal / FreqInitial
        float initialFreq = freqHighestFreqWins;
        float desiredFinalFreq = semitoneIntervals[indexOfSemitoneToShiftTo];
        float newScaleFactor = 1.0f;
        // Should not compare a float with zero, but in this case it is okay
        // because "initialFreq" is assigned with "freqHighestFreqWins" which
        // got its value from the function "FindFreqWithHighestFreqWins" which
        // will return zero if something went wrong.
        if (initialFreq != 0)
        {
            newScaleFactor = desiredFinalFreq / initialFreq;
        }

        // DEBUG: To display information about the frequencies etc.
        /*
        Debug.Log("initialFreq = " + initialFreq + ", desiredFinalFreq = " + desiredFinalFreq + ", newScaleFactor = " + newScaleFactor);
        float freqBeforePitchshifting = initialFreq; //FindFreqWithHighestFreqWins(segment);
        Debug.Log("freq BEFORE pitchshifting = " + freqBeforePitchshifting + ", segment.Length = " + segment.Length);
        */

        // Check if it is worth pitchshifting if the difference in frequency between the initial and the desired frequency are minimal.
        float difference = System.Math.Abs(initialFreq - desiredFinalFreq);
        bool worthPitchshifting = (difference > 2.0f); // In Hz.

        // Pitchshift the segment.
        if (newScaleFactor != 1.0f && worthPitchshifting)
        {
            PitchShifter.PitchShift((float)newScaleFactor, segment.LongLength, 512, 4, 48000, segment);

            // DEBUG: Compare the pitchshifted segment's frequency to the initial frequency.
            /*
            float freqAfterPitchshifting = FindFreqWithHighestFreqWins(segment);
            Debug.Log("freq AFTER pitchshifting = " + freqAfterPitchshifting);
            Debug.Log("Segment has been pitchshifted!");
            */    
        }
        else
        {
            Debug.Log("No pitchshifting took place. Difference between frequencies is = " + difference);
        }

        return segment;
    }

    // Generate the frequencies for the musical notes from C2 to B7.
    private void GenerateMusicalNoteFreqs()
    {
        // 12 semitones * 6 octaves, from C2 to B7.
        semitoneIntervals = new float[12 * 6]; 

        // The fundamental frequencies of each semitone in the first octave.
        semitoneIntervals[0] = 65.41f;
        semitoneIntervals[1] = 69.30f;
        semitoneIntervals[2] = 73.42f;
        semitoneIntervals[3] = 77.78f;
        semitoneIntervals[4] = 82.41f;
        semitoneIntervals[5] = 87.31f;
        semitoneIntervals[6] = 92.50f;
        semitoneIntervals[7] = 98.00f;
        semitoneIntervals[8] = 103.83f;
        semitoneIntervals[9] = 110.00f;
        semitoneIntervals[10] = 116.54f;
        semitoneIntervals[11] = 123.47f;

        // Generate the rest of the semitones starting from the second octave (index = 12).
        // This is possible because the rest of the semitones are multiples of the fundamental
        // frequency for each musical note.
        int index = 12;
        int semitoneNumID = 1;
        int counter = 0;
        while (index < semitoneIntervals.Length)
        {
            // Reset if exceeds 12 (which is an octave)
            if (semitoneNumID >= 12)
            {
                semitoneNumID = 1;
                counter++;
            }

            int prevSemitoneIndex = (index > 11) ? index - 12 : index;

            // Calculate the multiple and assign it.
            semitoneIntervals[index] = semitoneIntervals[prevSemitoneIndex] * 2;

            index++;
            semitoneNumID++;
        }
    }

    // Returns the power of two.
    private float GetPowerOfTwo(double semitoneID)
    {
        double value = 2.0;
        double power = semitoneID / 12.0;
        double pitchFactor = System.Math.Pow(value, power);

        return (float)pitchFactor;
    }

    // Get fundamental frequency of an audio segment.
    // FFT the segment, find the highest frequency and return it.
    public float FindFreqWithHighestFreqWins(float[] segment)
    {
        int QSamples = 4096;
        int[] powerOfTwoIntervals = new int[16];
        double exp = 2;
        for (int k = 1; k < powerOfTwoIntervals.Length; k++)
        {
            powerOfTwoIntervals[k - 1] = (int)System.Math.Pow(2, exp + k - 1);
            powerOfTwoIntervals[k] = (int)System.Math.Pow(2, exp + k);

            if (powerOfTwoIntervals[k - 1] < segment.Length && segment.Length < powerOfTwoIntervals[k])
            {
                QSamples = powerOfTwoIntervals[k - 1];
                break;
            }

            k++;
        }

        B83.MathHelpers.Complex[] spec2;

        float PitchValue = 1;
        float _fSample;

        spec2 = new B83.MathHelpers.Complex[QSamples];

        _fSample = AudioSettings.outputSampleRate;

        // Copy the output data into the complex array.
        for (int i = 0; i < QSamples; i++)
        {
            spec2[i] = new B83.MathHelpers.Complex(segment[i], 0);
        }

        // Calculate the FFT.
        B83.MathHelpers.FFT.CalculateFFT(spec2, false);

        float[] tempSamples2 = B83.MathHelpers.FFT.Complex2Float(spec2, false);

        // We now have the array that FFT has been applied to, time to find the frequency.
        float maxV = 0;
        var maxN = 0;
        for (int i = 0; i < QSamples; i++)
        { 
            // Find max.
            if ((tempSamples2[i] > maxV))
            {
                maxV = tempSamples2[i];
                // "maxN" is the index of max.
                maxN = i; 
            }
        }

        // Pass the index to a float variable.
        float freqN = maxN; 
        if (maxN > 0 && maxN < QSamples - 1)
        { 
            // Interpolate index using neighbours.
            var dL = tempSamples2[maxN - 1] / tempSamples2[maxN];
            var dR = tempSamples2[maxN + 1] / tempSamples2[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }
        // Convert index to frequency. "48000" is the sampling rate.
        PitchValue = freqN * 48000 / QSamples; 

        return PitchValue;
    }
}
