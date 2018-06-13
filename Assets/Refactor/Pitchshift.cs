using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Den ska bara ta in godtycklig segment och returnera en pitchshifted segment
public class Pitchshift
{
    //private static float[] segment;
    private static Pitch.PitchTracker pitchTracker;
    private static float[] semitoneIntervals;

    public float[] PitchshiftSegment(float[] segment)
    {
        // Hitta frekvensen på det inkommande ljudet (just nu bara debug data från audiosource).
        //float freqAutocorrelation = FindFreqWithAutocorrelation(segment);
        float freqHighestFreqWins = FindFreqWithHighestFreqWins(segment);
        //Debug.Log("Highest freq wins i början av funktionen = " + freqHighestFreqWins);

        // Hitta vilken not som frekvensen ligger närmast.
        GenerateMusicalNoteFreqs();

        float soundFreq = freqHighestFreqWins;
        int indexOfSemitoneToShiftTo = 0;

        // Find which semitone the frequency is.
        for (int i = 1; i < semitoneIntervals.Length; i++)
        {
            float prevNoteFreq = semitoneIntervals[i - 1];
            float currentNoteFreq = semitoneIntervals[i];

            // If within two musical note frequencies.
            if (prevNoteFreq < soundFreq && soundFreq < currentNoteFreq)
            {
                float prevDiff = System.Math.Abs(soundFreq - prevNoteFreq);
                float currentDiff = System.Math.Abs(currentNoteFreq - soundFreq);
                //Debug.Log("prevDiff = " + prevDiff + "   and nextDiff = " + currentDiff);

                if (prevDiff > currentDiff) // Closer to the the current musical note.
                {
                    //Debug.Log("CLOSER RIGHT (current i)");
                    indexOfSemitoneToShiftTo = i;
                    break;
                }
                else // Closer to the left.
                {
                    //Debug.Log("CLOSER LEFT (i-1)");
                    indexOfSemitoneToShiftTo = i - 1;
                    break;
                }
            }
        }
        //Debug.Log("freq[i] = " + semitoneIntervals[indexOfSemitoneToShiftTo]);

        // Justera pitchshift enligt vilken bokstavsnot den ska pitchas till genom att räkna ut nåt med differens 
        // i frekvenser som skiljer från falsk frek till riktigt not frek.

        // vi har ljudfrek, desired frek, och vi vill ha nåt, 
        // Vi har riktiga pitchfaktorn, men pitchshift antar att vi redan har en låt i rätt stämd frekvens.
        // Hitta pitchfaktorn för att tune:a till rätt frekvens

        // Vi vill till den frekvensen

        // Om det är ett B vi vill skala till ,  2^1/12 är c och b är 2^11/12, dessa behövs ändras för att kompensera
        // för de frekvenser som vi är efter på
        // Vi vill nå 2^11/12, hitta en skala
        // minst / störst = skala
        // skala * 2^11/12
        // om vi är under desired frekvens, måste skalan vara över 1


        // Ta reda på vilken bokstavsnot som det ska skalas till.
        // Vi har indexOfSemi...
        double semitoneNumID;
        if (indexOfSemitoneToShiftTo + 1 >= 12)
        {
            if ((indexOfSemitoneToShiftTo + 1) % 12 == 0) // om det är B, specialfall
                semitoneNumID = 12; // För man får resten och den resten är vilket num ID.
            else
                semitoneNumID = indexOfSemitoneToShiftTo % 12;

            //semitoneNumID = System.Math.Round((double)indexOfSemitoneToShiftTo / 12);
            //Debug.Log("Inne i if >= 12");
        }
        else
        {
            semitoneNumID = indexOfSemitoneToShiftTo + 1; // För man får resten och den resten är vilket num ID.
            //Debug.Log("Inne i else, semitone plus 1");
        }

        //Debug.Log("scalefactor = " + scaleFactor);
        //Debug.Log("indexOfSemitoneToShiftTo = " + indexOfSemitoneToShiftTo);
        Debug.Log("semitoneNumID = " + semitoneNumID);


        // Vi har tex 1/12 = 0.12, och vi vill kanske ha 1.2/12 = 0.14
        // För att få det borde jag göra något med att 
        // ta den frekvens vi har från början, och jämföra med närmaste
        // tons frekvens. Hitta hur mycket som måste skalas för att nå dit.
        // 2^1/12 * FreqInitial =  FreqFinal
        // 2^1/12 = freqFinal / freqInitial

        float initialFreq = freqHighestFreqWins;//FindFreqWithHighestFreqWins(segment);
        float desiredFinalFreq = semitoneIntervals[indexOfSemitoneToShiftTo];

        float newScaleFactor = 1.0f;
        if (initialFreq != 0)
        {
            newScaleFactor = desiredFinalFreq / initialFreq;
        }
        Debug.Log("initialFreq = " + initialFreq + ", desiredFinalFreq = " + desiredFinalFreq + ", newScaleFactor = " + newScaleFactor);

        // Kolla sen om pitchen är rätt med ett kall till metoden FindpitchHighestFreqWins
        float freqBeforePitchshifting = initialFreq; //FindFreqWithHighestFreqWins(segment);
        Debug.Log("freq BEFORE pitchshifting = " + freqBeforePitchshifting + ", segment.Length = " + segment.Length);

        // Check if it is worth pitchshifting if the difference in frequency between the initial and the desired frequency are minimal.
        float difference = System.Math.Abs(initialFreq - desiredFinalFreq);
        bool worthPitchshifting = (difference > 0.0f);

        // Pitchshifta klippet.
        if (newScaleFactor != 1.0f && worthPitchshifting)
        {
            PitchShifter.PitchShift((float)newScaleFactor, segment.LongLength, 512, 4, 48000, segment); // 44100 och 2048, 1024

            // Hitta frekvensen efter pitchshifting och jämför med fundamentalfrekvenser
            float freqAfterPitchshifting = FindFreqWithHighestFreqWins(segment);
            Debug.Log("freq AFTER pitchshifting = " + freqAfterPitchshifting);
            Debug.Log("Segment has been pitchshifted!");
        }
        else
        {
            Debug.Log("No pitchshifting took place. Difference between frequencies is = " + difference);
        }

        return segment;
    }


    private static float FindFreqWithAutocorrelation(float[] tempSamples)
    {
        //pitchTracker = new PitchTracker
        //{
        //    SampleRate = 44100.0,
        //    RecordPitchRecords = true,
        //    PitchRecordHistorySize = 10
        //};
        //pitchTracker.ProcessBuffer(tempSamples, tempSamples.Length); // Data to process.
        //                                                             //Debug.Log(pitchTracker.CurrentPitchRecord.Pitch);

        //var pitchRecords = pitchTracker.PitchRecords;

        //foreach (var pitchRecord in pitchRecords)
        //{
        //    Debug.Log("Pitch skdajakdj = " + pitchRecord.Pitch.ToString());
        //    // ... or whatever
        //}

        //for (int i = 0; i < pitchTracker.PitchRecords.Count; i++)
        //{
        //    Debug.Log("pitchjhadkjshakd = " );
        //}
        //Debug.Log(" AHAHAHAHA = " + test);

        float temp = 0.0f;
        return temp;
    }

    private static void GenerateMusicalNoteFreqs()
    {
        semitoneIntervals = new float[12 * 6]; // 12 semitones * 6 octaves, från C2 till B7.

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

        int index = 12;
        int semitoneNumID = 1;
        int counter = 0;
        while (index < semitoneIntervals.Length)
        {
            if (semitoneNumID >= 12)
            {
                semitoneNumID = 1;
                counter++;
            }

            int prevSemitoneIndex = (index > 11) ? index - 12 : index;
            semitoneIntervals[index] = semitoneIntervals[prevSemitoneIndex] * 2;
            //Debug.Log("semitoneIntervals[" + index + "] = " + semitoneIntervals[index]);

            index++;
            semitoneNumID++;
        }
    }

    private static float GetPowerOfTwo(double semitoneID)
    {
        double value = 2.0;
        double power = semitoneID / 12.0;
        double pitchFactor = System.Math.Pow(value, power);

        return (float)pitchFactor;
    }


    // Get fundamental frequency.
    public float FindFreqWithHighestFreqWins(float[] segment)
    {
        int QSamples = 4096;//2048; //131072;
        int[] powerOfTwoIntervals = new int[16];
        double exp = 2;
        for (int k = 1; k < powerOfTwoIntervals.Length; k++)
        {
            powerOfTwoIntervals[k - 1] = (int)System.Math.Pow(2, exp + k - 1);
            powerOfTwoIntervals[k] = (int)System.Math.Pow(2, exp + k);

            //Debug.Log("powerOfTwoIntervals[k-1] = " + powerOfTwoIntervals[k - 1]);
            //Debug.Log("powerOfTwoIntervals[k] = " + powerOfTwoIntervals[k]);
            if (powerOfTwoIntervals[k - 1] < segment.Length && segment.Length < powerOfTwoIntervals[k])
            {
                //Debug.Log("ASSIGNED QSAMPLES.");
                QSamples = powerOfTwoIntervals[k - 1];
                break;
            }

            k++;
        }

        // Debug.Log("QSamples = " + QSamples);


        float[] tmp;
        B83.MathHelpers.Complex[] spec2;
        B83.MathHelpers.Complex[] spec3;

        const float Threshold = 0.02f;
        float PitchValue = 1;
        float _fSample;


        tmp = new float[QSamples];
        spec2 = new B83.MathHelpers.Complex[QSamples];
        spec3 = new B83.MathHelpers.Complex[QSamples];

        _fSample = AudioSettings.outputSampleRate;
        //Debug.Log("_fsample = " + _fSample);

        // copy the output data into the complex array
        for (int i = 0; i < QSamples; i++) //tempSamples.Length
        {
            spec2[i] = new B83.MathHelpers.Complex(segment[i], 0);
            //Debug.Log("copying to complex");
        }
        // calculate the FFT
        B83.MathHelpers.FFT.CalculateFFT(spec2, false);

        float[] tempSamples2 = B83.MathHelpers.FFT.Complex2Float(spec2, false);

        // Nu har vi en FFT:ad array, vi måste hitta frekvensen nu bara
        float maxV = 0;
        var maxN = 0;
        for (int i = 0; i < QSamples; i++)
        { 
            // find max 
            if ((tempSamples2[i] > maxV)) // && (tempSamples2[i] > Threshold))
            {
                maxV = tempSamples2[i];
                maxN = i; // maxN is the index of max
            }
        }

        //Debug.Log("maxV = " + maxV);

        //Debug.Log("max index = " + maxN);
        float freqN = maxN; // pass the index to a float variable
        if (maxN > 0 && maxN < QSamples - 1)
        { // interpolate index using neighbours
            var dL = tempSamples2[maxN - 1] / tempSamples2[maxN];
            var dR = tempSamples2[maxN + 1] / tempSamples2[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }
        //PitchValue = freqN * (44100 / 2.0f) / QSamples; // convert index to frequency
        PitchValue = freqN * 48000 / QSamples; // TODO: ändra till 48000
                                                //Debug.Log("pitch = " + PitchValue);

        return PitchValue;
    }
}
