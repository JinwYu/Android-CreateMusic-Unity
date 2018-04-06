using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using B83.MathHelpers;
using Pitch;

public class ImplementBernessScript : MonoBehaviour {

    AudioSource audioSource;
    static PitchTracker pitchTracker;
    float[] semitoneIntervals;

    // Use this for initialization
    void Start () {

        // Get debug audio from the AudioSource.
        audioSource = GetComponent<AudioSource>();
        float[] tempSamples = new float[audioSource.clip.samples];
        audioSource.clip.GetData(tempSamples, 0);
        //audioSource.Play();

        // Hitta frekvensen på det inkommande ljudet (just nu bara debug data från audiosource).
        float freqAutocorrelation = FindFreqWithAutocorrelation(tempSamples);
        float freqHighestFreqWins = FindFreqWithHighestFreqWins(tempSamples);
        Debug.Log("Highest freq wins = " + freqHighestFreqWins);

        // Hitta vilken not som frekvensen ligger närmast.
        GenerateMusicalNoteFreqs();

        float soundFreq = freqHighestFreqWins;
        int indexOfSemitoneToShiftTo = 0;
        float scaleFactor = 0.0f;
        // Loopa igenom hela semitoneIntervals
        for (int i = 1; i < semitoneIntervals.Length; i++)
        {
            float prevNoteFreq = semitoneIntervals[i - 1];
            float nextNoteFreq = semitoneIntervals[i];

            // If within two musical note frequencies.
            if(prevNoteFreq < soundFreq && soundFreq < nextNoteFreq)
            {
                float prevDiff = soundFreq - prevNoteFreq;
                float nextDiff = nextNoteFreq - soundFreq;

                //indexOfSemitoneToShiftTo = (prevDiff > nextDiff) ? i : i - 1;
                if(prevDiff > nextDiff) // Closer to the right.
                {
                    indexOfSemitoneToShiftTo = i;

                    // Calc. scaling factor.
                    // om vi är under desired frekvens, måste skalan vara över 1
                    scaleFactor = semitoneIntervals[indexOfSemitoneToShiftTo] / soundFreq;


                }
                else // Closer to the left.
                {
                    indexOfSemitoneToShiftTo = i - 1;

                    scaleFactor = soundFreq / semitoneIntervals[indexOfSemitoneToShiftTo];
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
        if (indexOfSemitoneToShiftTo >= 11)
        {
            semitoneNumID = (indexOfSemitoneToShiftTo % 11); // För man får resten och den resten är vilket num ID.
        }
        else
        {
            semitoneNumID = indexOfSemitoneToShiftTo + 1; // För man får resten och den resten är vilket num ID.
        }
        
        Debug.Log("scalefactor = " + scaleFactor);
        Debug.Log("indexOfSemitoneToShiftTo = " + indexOfSemitoneToShiftTo);
        Debug.Log("semitoneNumID = " + semitoneNumID);

        // Pitchshifta.
        double value = 2.0;
        //double power = 5.0 / 12.0;
        double power = semitoneNumID / 12.0;
        double pitchFactor = System.Math.Pow(value, power);

        Debug.Log("Before scale: pitchFactor = " + pitchFactor);
        pitchFactor = pitchFactor * scaleFactor;
        Debug.Log("After scale: pitchFactor = " + pitchFactor);

        // Fixa ostämda klipp som jag kan testa med. 
        // Kolla sen om pitchen är rätt med ett kall till methode FindpitchHighestFreqWins
        // och be till gud att det funkar.


        //(float pitchShift, long numSampsToProcess, long fftFrameSize, long osamp, float sampleRate, float[] indata)
        //PitchShifter.PitchShift((float)pitchFactor, tempSamples.LongLength, 2048, 4, 44100, tempSamples);
        //Debug.Log("Pitch shifted the clip.");

        // Spela upp det pitchshiftade klippet.
        //audioSource.clip = AudioClip.Create("pitch shifted clip", tempSamples.Length, audioSource.clip.channels, 44100, false);
        //audioSource.clip.SetData(tempSamples, 0);
        //audioSource.loop = true;
        //audioSource.Play();     
    }

    private float FindFreqWithAutocorrelation(float[] tempSamples)
    {
        pitchTracker = new PitchTracker
        {
            SampleRate = 44100.0,
            RecordPitchRecords = true,
            PitchRecordHistorySize = 10
        };
        pitchTracker.ProcessBuffer(tempSamples, tempSamples.Length); // Data to process.
        //Debug.Log(pitchTracker.CurrentPitchRecord.Pitch);

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

    private void GenerateMusicalNoteFreqs()
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

    private float GetPowerOfTwo(double semitoneID)
    {
        double value = 2.0;
        double power = semitoneID / 12.0;
        double pitchFactor = System.Math.Pow(value, power);

        return (float)pitchFactor;
    }


    // Get fundamental frequency.
    public float FindFreqWithHighestFreqWins(float[] tempSamples)
    {
        int QSamples = 65536; //131072;
        float[] tmp;
        Complex[] spec2;
        Complex[] spec3;

        const float Threshold = 0.02f;
        float PitchValue;
        float _fSample;


        tmp = new float[QSamples];
        spec2 = new Complex[QSamples];
        spec3 = new Complex[QSamples];

        _fSample = AudioSettings.outputSampleRate;
        //Debug.Log("_fsample = " + _fSample);

        audioSource = GetComponent<AudioSource>();

        float[] tempSamples1 = new float[audioSource.clip.samples];
        audioSource.clip.GetData(tempSamples1, 0);

        // copy the output data into the complex array
        for (int i = 0; i < QSamples; i++) //tempSamples.Length
        {
            spec2[i] = new Complex(tempSamples[i], 0);
            //Debug.Log("copying to complex");
        }
        // calculate the FFT
        FFT.CalculateFFT(spec2, false);

        float[] tempSamples2 = FFT.Complex2Float(spec2, false);

        // Nu har vi en FFT:ad array, vi måste hitta frekvensen nu bara
        float maxV = 0;
        var maxN = 0;
        for (int i = 0; i < QSamples; i++)
        { // find max 
            //if (!(tempSamples2[i] > maxV) || !(tempSamples2[i] > Threshold))
            //    continue;
            //Debug.Log("i = " + i);
            if ((tempSamples2[i] > maxV) && (tempSamples2[i] > Threshold))
            {
                maxV = tempSamples2[i];
                maxN = i; // maxN is the index of max
                //Debug.Log("inside, i = " + i);
            }
        }

        //Debug.Log("max index = " + maxN);
        float freqN = maxN; // pass the index to a float variable
        if (maxN > 0 && maxN < QSamples - 1)
        { // interpolate index using neighbours
            var dL = tempSamples2[maxN - 1] / tempSamples2[maxN];
            var dR = tempSamples2[maxN + 1] / tempSamples2[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }
        //PitchValue = freqN * (44100 / 2.0f) / QSamples; // convert index to frequency
        PitchValue = freqN * 44100 / QSamples; // TODO: ändra till 48000
        //Debug.Log("pitch = " + PitchValue);

        return PitchValue;
    }


}


// MISC/TANKAR

// Det måste ju vara rätt pitch detection inne i pitchshifter scriptet
        // eftersom den pitchar till rätt frekvens/ton. 
        // Kan jag identifiera den koden som fixar det?
        // För jag behöver veta fundamental frekvensen för att kunna bestämma vilken not jag ska
        // pitcha till
        // SVAR: Nej kan inte, pitch shifter vet inget om vilken frekvens den har, eller jo i varje bin, men orkar inte extract det här.

        //---------------------------------------------------------
        // Gör en array med alla fundamentalfrekvenser 
        // då kan jag få reda på generellt om ljudets frekvens är nära tex A eller B
        // Räkna ut skillnaden i antal frekvenser som ljudet är fel i frekvens till den ton den är närmast tex A
        // Konvertera denna differens till pitchfactor
        // Pitcha enligt det

        // eftersom algoritmen jag har någorlunda rätt kan identifiera vilken frekvens
        // som ett ljud har, och eftersom det är multiplar till varje not, så räcker
        // det bara att veta vilken not man vill pitchshifta till. Vilken oktav behövs därför inte
        //-------------------------------------------------

        // Bra svar här: https://stackoverflow.com/questions/1847633/net-library-to-identify-pitches
        // MusiGenesis


        // Måste fixa hur 1 till 0.5 ger en oktav neråt och 1 till 2 ger en oktav uppåt
        // Måste reglera hur pitch factor ska reglera

        // Om length i antal sampel för ett klipp är lite eller om klippet är highpitched
        // så ska jag inte pitchshifta

        // Om flera segment sitter ihop så kanske jag ska skicka alla de som ett stort klipp att pitchas
        // för annars kommer varje individuellt väldigt kort segment att pitchshiftas och
        // det kommer uppstå konstiga skillnader i ton mellan klippen och förmodligen låta hemskt

        // Lära mig all teknik/matematik bakom den här skiten

        // NEDAN ÄR BARA KOD FÖR ATT HITTA FREKVENSEN

