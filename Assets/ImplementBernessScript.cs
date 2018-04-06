using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using B83.MathHelpers;
using Pitch;

public class ImplementBernessScript : MonoBehaviour {

    AudioSource audioSource;
    static PitchTracker pitchTracker;

    // Use this for initialization
    void Start () {

        



        audioSource = GetComponent<AudioSource>();

        float[] tempSamples = new float[audioSource.clip.samples];
        audioSource.clip.GetData(tempSamples, 0);
        //audioSource.Play();

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
        //    //Debug.Log("Pitch skdajakdj = " + pitchRecord.Pitch.ToString());
        //    // ... or whatever
        //}

        //for (int i = 0; i < pitchTracker.PitchRecords.Count; i++)
        //{
        //    Debug.Log("pitchjhadkjshakd = " );
        //}
        //Debug.Log(" AHAHAHAHA = " + test);


        // Get initial pitch value.
        DetectPitch(tempSamples);

        double value = 2.0;
        double power = 5.0 / 12.0;
        double pitchFactor = System.Math.Pow(value, power);
        //Debug.Log("factor = " + pitchFactor);
        // (float pitchShift, long numSampsToProcess, long fftFrameSize, long osamp, float sampleRate, float[] indata)
        //PitchShifter.PitchShift((float)pitchFactor, tempSamples.LongLength, 2048, 4, 44100, tempSamples);
        Debug.Log("Pitch shifted the clip.");

        // Varför är det så kort klipp? är det för klassen returnerar en mindre version.

        //audioSource.clip = AudioClip.Create("pitch shifted clip", tempSamples.Length, audioSource.clip.channels, 44100, false);
        //audioSource.clip.SetData(tempSamples, 0);
        //audioSource.loop = true;

        //audioSource.Play();
        Debug.Log("Playing pitchshifted clip.");

        // Get the pitch value of the pitch shifted sound.
        Debug.Log("Freq of pitch shifted sound: ");
        DetectPitch(tempSamples);


        // Det måste ju vara rätt pitch detection inne i pitchshifter scriptet
        // eftersom den pitchar till rätt frekvens/ton. 
        // Kan jag identifiera den koden som fixar det?
        // För jag behöver veta fundamental frekvensen för att kunna bestämma vilken not jag ska
        // pitcha till

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


    }



    // Get fundamental frequency.
    public void DetectPitch(float[] tempSamples)
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
        Debug.Log("pitch = " + PitchValue);
    }


}
