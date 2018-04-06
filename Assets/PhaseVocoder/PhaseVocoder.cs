using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseVocoder : MonoBehaviour {

    int framesize = 2048;
    int fftsize = 2048;
    int analysis_hopsize = 256; // TODO: välj parametrar efter tabellen i rapporten
    int resynthesis_hopsize;
    int number_of_frame;
    float[,] magnitude;
    float[,] phase;
    float[] buffer;
    string window_type = "Hamming";
    float[] windowbuffer;

    AudioSource audioSource;

    void Start () {

        audioSource = GetComponent<AudioSource>();
        float[] tempSamples = new float[audioSource.clip.samples];
        audioSource.clip.GetData(tempSamples, 0);
        int input_sound_size = tempSamples.Length;

        number_of_frame = input_sound_size / analysis_hopsize;
        magnitude = new float[number_of_frame, fftsize];
        phase = new float[number_of_frame, fftsize];
        buffer = new float[fftsize];
        windowbuffer = new float[framesize];
    }

    /*
        Input: input_sound_info, fftsize, window_type,
        framesize, analysis_hopsize, number_of_frame
        Output: magnitude, phase
    */
    /*
        The analysis algorithm in the phase vocoder is follwing steps;
        1. Take frame(N) size samples from input sound. Store them into a frame buffer.
        2. Choose a proper window and window the buffer
        3. Zero-pad the buffer
        4. FFT-shift the zero-padded buffer.
        5. Decompose the buffer by Fourier transform
        6. Calculate and store magnitude and phase (output from analysis that is used in resynthesis).
        7. Go back to step 1 and start taking frame size sample located at hop size distant from
        the first sample of the previous loop.
    */

    private void Analysis(int input_sound_info, int fftsize, string window_type,
    int framesize, int analysis_hopsize, int number_of_frame)
    {
        int input_index = 0;
        float[] fft_complex_number = new float[fftsize];//has both real and imaginary parts

        //windowbuffer = window(windowtype, framesize);
        // TODO: fixa hur skapar ett window av hamming

        for (int i = 0; i < number_of_frame; i++)
        {
            for (int j = 0; j < framesize; j++)
            {
                //buffer[j] = input_sound[input_index + j] * windowbuffer[j];
            }
            //fftshift(buffer);
            //fft(buffer, fft_complex_number);
            //get_magnitude(magnitude[i][], fft_complex_number);
            //get_phase(phase[i][], fft_complex_number);
            //input_index += analysis_hop;
        }

    }

}
