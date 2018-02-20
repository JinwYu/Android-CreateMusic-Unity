using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class to split the audio into smaller parts by finding the transients.

public class Quantization : MonoBehaviour {

    private AudioSource audioSource;
    private float[] recording;

	// Use this for initialization
	void Start () {

        // ladda in trumloopen som en array
        audioSource = GetComponent<AudioSource>();

        // Check if it's stereo (two channels), because it affects the time calculation.
        if(audioSource.clip.channels == 2)
        {

        }
        else // Mono.
        {
            
        }

        int numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels; // In samples/indices.
        recording = new float[numSamplesInRecording];
        audioSource.clip.GetData(recording, 0);
        //Debug.Log("length of the loop is = " + loop.Length);

        //GetComponent<AudioSource>().loop = true; // Set the AudioClip to loop
        //GetComponent<AudioSource>().Play();

        // Ta reda på hur många index en sekund motsvarar
        float msDurationRecording = ((recording.Length * 1000) * audioSource.clip.channels) / 44100; // Nämnaren är samplingfreq och 1000 är för att göra till ms.
                                                                                                     // TODO: ta från RecordedLoops istället sen, tempkod för att testa algoritmen just nu.

        // Bestäm hur lång en paus ska vara i  ms, enl. undersökning i adobe audition verkar
        // 120ms rimligt för denna trumloop.

        // Hitta transienten.

        //float threshold = 0.000001f;
        //int numSamplesForEmptyThreshold = 44100 * 30 / 1000;
        //Debug.Log("antal sampel för tysthet = " + numSamplesForEmptyThreshold);
        //int counter = 0;
        //int startIndex = 0;
        //int idx = 0;
        //bool trimmedClipReadyToSave = false;
        //int endIndex = 0;
        //bool clipHasBeenSaved = false;

        // Om tyst i 60ms och en transient dyker upp, antalSampel = 44100 * 60/1000

        //for (int idx = 0; idx < recording.Length; idx++)
        //while (idx < recording.Length)
        //{
        //    float absValOfSingleSample = System.Math.Abs(recording[idx]);

        //    Debug.Log("absVal = " + absValOfSingleSample);
        //    // Om det är låg volym
        //    if (absValOfSingleSample < threshold)
        //    {
        //        Debug.Log("1. Ett sampel är mindre än threshold.");
        //        // Räkna hur många index som är tyst
        //        counter++;

        //        // Spara klippet
        //        if (trimmedClipReadyToSave)
        //        {
        //            Debug.Log("Dags att spara klippet till en array.");
        //            trimmedClipReadyToSave = false;
        //            clipHasBeenSaved = true;
        //        }
        //    }

        //    // Hittat en transient.
        //    // När varit tyst i tex 30ms och sedan går över till högre värde
        //    // Borde förbättra så att algoritmen  räknar att det gradvis går uppåt i intensitet
        //    if (counter > numSamplesForEmptyThreshold && absValOfSingleSample > threshold)
        //    {
        //        Debug.Log("2. En transient har hittats.");
        //        int index = 0;

        //        // Iterera index till nästa transient hittats?
        //        while (System.Math.Abs(recording[idx + index]) > threshold)
        //        {
        //            index++;
        //            Debug.Log("´3. Räknar antal index som ett ljud pågår över threshold. index = " + index);
        //        }
        //        startIndex = idx;
        //        endIndex = idx + index;
        //        counter = 0;
        //        trimmedClipReadyToSave = true;
        //    }


        //    if (clipHasBeenSaved)
        //    {
        //        idx = endIndex;
        //        clipHasBeenSaved = false;
        //    }
        //    idx++;
        //}



        //for (int idx = 0; idx < idx + 3 && idx < recording.Length; idx++) // ändra till 3 till intervallet den ska söka igenom
        //{
        //    if(recording[idx] < threshold)
        //    {
        //        itIsQuiet = true;
        //        break;
        //        // Problem att det kan ju vara tyst länge
        //    }
        //}

        //int startIndex = 0;
        //int endIndex;
        //int counter = 0;
        //float threshold = 0.1f;
        //bool firstRangeIsQuiet = false;
        //bool secondRangeHasSound = false;
        //int range = 44100 * 100 * audioSource.clip.channels / 1000;
        //float[] trimmedIndividualSound;
        //bool firstTimeSaving = true;
        ////Debug.Log("range = " + range);
        //bool isQuiet = true;


        //// Går genom hela listan
        ////for (int idx = 0; idx < 70000; idx++)
        //int idx = 0;
        //int keepGoingFromThisIndex = 0;

        //while (idx < 1000)
        //{
        //    //int firstEndIndex = idx + range;

        //    // Kolla om det sker en förändring mellan tystnad och ljud.
        //    if (System.Math.Abs(recording[idx]) < threshold)
        //    {
        //        isQuiet = true;
        //        Debug.Log("QUIET");
        //    }
        //    else
        //    {
        //        isQuiet = false;
        //        Debug.Log("SOUND");
        //    }

        //    if (!isQuiet)
        //    {
        //        int counterr = 0;
        //        int udx = idx;

        //        Debug.Log("--------------------- Found a transient -----------------------------");
        //        Debug.Log("Startar counter för att beräkna längden.");
        //        while (System.Math.Abs(recording[udx]) > threshold)
        //        {
        //            counterr++; // = längden i index
        //            Debug.Log("counter = " + counterr);
        //            udx++;
        //        }

        //        keepGoingFromThisIndex = udx + counterr;

        //        if (firstTimeSaving)
        //        {
        //            // Spara klippet
        //            trimmedIndividualSound = new float[counterr];

        //            // Kopiera över ljudet
        //            for (int odx = 0; odx < counterr; odx++)
        //            {
        //                trimmedIndividualSound[odx] = recording[udx + odx];
        //            }

        //            //if (counter != 0)
        //            //{
        //                Debug.Log("Clip saved, playing.");
        //                audioSource.clip = AudioClip.Create("recorded samples", counterr, 2, 44100, false);
        //                audioSource.clip.SetData(trimmedIndividualSound, 0);
        //                audioSource.loop = true;
        //                audioSource.Play();
        //                firstTimeSaving = false;
        //                break;
        //            //}
        //        }                
        //    }

        //    idx++;
        //    // Fixa så idx fortsätter från counterr
        //    //idx = idx + keepGoingFromThisIndex + 1;
        //    Debug.Log("End of the loop index = " + idx);
        //}




        // Ta start index och längden i counter och spara detta


        // Spara ljudet.

        // Spara start och end

        // Räkna hur länge ljud spelas
        /*
        while (idx < 1000)
        {
            int firstEndIndex = idx + range;

            //Gå igenom den första range: en och om det inte är tyst så break:ar denna nedan loop.
            for (int udx = idx; udx < firstEndIndex; udx++)
            {
                if (System.Math.Abs(recording[udx]) < threshold)
                {
                    firstRangeIsQuiet = true;
                    Debug.Log("1a loopen, det är tyst här.");
                    Debug.Log("udx (1a loop iterator) = " + udx + " och firstEndIndex = " + firstEndIndex);
                }
                else
                {
                    firstRangeIsQuiet = false;
                    Debug.Log("Break in first loop. Ljud här.");
                    break;
                }
            }

            int secondEndIndex = firstEndIndex + range / 3;

            // Behöver inte köras om firstRangeIsQuiet är false ju
            // Kör bara om det är tyst från första range-loopen.
            if (firstRangeIsQuiet)
            {
                Debug.Log("Kollar andra range:n");

                for (int ydx = firstEndIndex + 1; ydx < secondEndIndex; ydx++)
                {
                    if (System.Math.Abs(recording[ydx]) > threshold)
                    {
                        secondRangeHasSound = true;
                        Debug.Log("2a loopen, det är ljud här.");
                    }
                    else
                    {
                        secondRangeHasSound = false;
                        Debug.Log("Break in second range loop. Tyst här.");
                        break; // No need to continue this loop if theres no sound after the quiet part above.
                    }
                }
            }

            if (firstRangeIsQuiet && secondRangeHasSound && firstTimeSaving)
            {
                Debug.Log("Found a transient");

                // Räkna hur länge ljudet pågår
                counter = 0;
                int ndx = firstEndIndex + 1;

                while (System.Math.Abs(recording[ndx]) > threshold)
                {
                    counter++;
                    ndx++; // Iterate.
                    Debug.Log("counter inside = " + counter);
                }

                // Spara klippet
                trimmedIndividualSound = new float[counter];

                // Kopiera över ljudet
                for (int odx = 0; odx < counter; odx++)
                {
                    trimmedIndividualSound[odx] = recording[ndx + odx];
                }

                if (counter != 0)
                {
                    Debug.Log("Clip saved, playing.");
                    audioSource.clip = AudioClip.Create("recorded samples", counter, 2, 44100, false);
                    audioSource.clip.SetData(trimmedIndividualSound, 0);
                    audioSource.loop = true;
                    audioSource.Play();
                    firstTimeSaving = false;
                    break;
                }
            }

            idx = firstEndIndex + 1;
            Debug.Log("idx at the end of the loop = " + idx);
        }
        */


        // ---------------------------- KL. 15.00 ----------------------------

        float threshold = 0.00001f; // Ideala värdet för kickdrum loopen är iaf 0.00001f.
        int j = 0;
        //int rangeToInvestigate = 8000;
        int rangeToInvestigate = 44100 * 200 * audioSource.clip.channels / 1000; // Ges i samples. tid = 200ms.
        float[] trimmedIndividualSound;
        bool firstTimeSaving = true;
        

        for (int idx = 0; idx < recording.Length - rangeToInvestigate; idx++)
        {
            bool isQuietBeforeTransient = (System.Math.Abs(recording[idx]) < threshold) ? true : false;
            bool soundAfterTransient = (System.Math.Abs(recording[idx + rangeToInvestigate]) > threshold) ? true : false;

            if(isQuietBeforeTransient && soundAfterTransient && firstTimeSaving)
            {
                int startIndex = 0;
               
                // Hitta skiftningen mellan noll volym till ljud och hitta start index.
                for(int jdx = 0; jdx < rangeToInvestigate; jdx++)
                {
                    if(System.Math.Abs(recording[idx+jdx]) > threshold)
                    {
                        startIndex = idx + jdx; // Save the start index.
                        break;
                    }
                }

                // Hitta end index.
                int udx = startIndex;
                int counter = 0;
                while(System.Math.Abs(recording[udx+1]) > threshold)
                {
                    counter++;
                    Debug.Log("counter = " + counter);
                    udx++;
                }

                // Spara skiten.
                // Spara klippet
                trimmedIndividualSound = new float[counter];

                // Kopiera över ljudet
                for (int odx = 0; odx < counter; odx++)
                {
                    trimmedIndividualSound[odx] = recording[startIndex + odx];
                }

                if (counter != 0)
                {
                    Debug.Log("Clip saved, playing.");
                    audioSource.clip = AudioClip.Create("recorded samples", counter, 2, 44100, false);
                    audioSource.clip.SetData(trimmedIndividualSound, 0);
                    audioSource.loop = true;
                    audioSource.Play();
                    firstTimeSaving = false;
                    //break;
                }
            }
        }




        Debug.Log("Kommer du hit så har koden inte fastnat iaf.");

    }
	
	// Update is called once per frame
	void Update () {
		
	}




}
