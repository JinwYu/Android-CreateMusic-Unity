using B83.MathHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PitchDetector : MonoBehaviour {

    int QSamples = 65536; //131072;
    float[] tmp;
    Complex[] spec2;
    Complex[] spec3;
    

    AudioSource audioSource;

    private const float Threshold = 0.02f;
    public float PitchValue;
    private float _fSample;


    void Start () {
        tmp = new float[QSamples];
        spec2 = new Complex[QSamples];
        spec3 = new Complex[QSamples];

        _fSample = AudioSettings.outputSampleRate;
        //Debug.Log("_fsample = " + _fSample);

        audioSource = GetComponent<AudioSource>();

        float[] tempSamples = new float[audioSource.clip.samples];
        audioSource.clip.GetData(tempSamples, 0);

        //Debug.Log("tempSamples = " + tempSamples.Length);
        
        // copy the output data into the complex array
        for (int i = 0; i < QSamples; i++) //tempSamples.Length
        {
            spec2[i] = new Complex(tempSamples[i], 0);
            //Debug.Log("copying to complex");
        }
        // calculate the FFT
        FFT.CalculateFFT(spec2, false);
        //for (int i = 0; i < spec2.Length / 2; i++) // plot only the first half
        //{
        //    // multiply the magnitude of each value by 2
        //    Debug.DrawLine(new Vector3(i, 4), new Vector3(i, 4 + (float)spec2[i].magnitude * 2), Color.white);
        //}

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
                Debug.Log("inside, i = " + i);
            }

            
        }
        
        Debug.Log("max index = " + maxN);
        float freqN = maxN; // pass the index to a float variable
        if (maxN > 0 && maxN < QSamples - 1)
        { // interpolate index using neighbours
            var dL = tempSamples2[maxN - 1] / tempSamples2[maxN];
            var dR = tempSamples2[maxN + 1] / tempSamples2[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }
        //PitchValue = freqN * (44100 / 2.0f) / QSamples; // convert index to frequency
        PitchValue = freqN * 44100 / QSamples;
        Debug.Log("pitch = " + PitchValue);

    }
}
