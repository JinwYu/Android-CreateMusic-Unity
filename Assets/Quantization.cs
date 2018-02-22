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

        // ----------------------------- 18-02-21 ----------------------------------------
        
        Debug.Log("REC. SIZE = " + recording.Length);
        float[][] allTrimmedSounds = new float[10][]; // TODO: Ändra antal möjliga ljud i en loop här.
        int numSavedTrimmedSounds = 0;
        int rangeToInvestigateInSamples = 44100 * 1 * audioSource.clip.channels / 1000; // Ges i samples. tid = 1ms.
        float BeforeSoundThreshold = 0.001f; // Ideala värdet för kickdrum loopen är iaf 0.00001f.
        float AfterSoundThreshold = 0.00006f;//0.00001f;
        int startIndex = 0;
        int endIndex = 0;
        int idx = 0;
        while (idx < recording.Length) // - rangeToInvestigateInSamples)
        {
            bool isQuietBeforeTransient = (System.Math.Abs(recording[idx]) < BeforeSoundThreshold) ? true : false;
            bool soundExistsAfterTransient;
            if (idx < recording.Length - rangeToInvestigateInSamples)
            {
                soundExistsAfterTransient = (System.Math.Abs(recording[idx + rangeToInvestigateInSamples]) > AfterSoundThreshold) ? true : false;
            }
            else // När den är över range:n.
            {
                // Reglera indexet, så den alltid håller sig inom Length men utanför recording.Length - rangeToInvestigateInSamples.
                int indexAhead = idx + rangeToInvestigateInSamples;
                int howMuchOverTheLength = indexAhead - recording.Length;
                indexAhead = recording.Length - rangeToInvestigateInSamples + howMuchOverTheLength;
                //Debug.Log("index Ahead 1 = " + indexAhead);
                soundExistsAfterTransient = (System.Math.Abs(recording[indexAhead]) > AfterSoundThreshold) ? true : false;
            }
            

            // Om hittar transient.
            if (idx >= endIndex && isQuietBeforeTransient && soundExistsAfterTransient)
            {
                // Hitta skiftningen mellan noll volym till ljud och hitta start index.
                int jdx = 0;
                while((idx + jdx) < recording.Length && System.Math.Abs(recording[idx + jdx]) < BeforeSoundThreshold)
                {
                    jdx++;
                }
                startIndex = idx + jdx;                

                // Hitta end index. När ljudet tystnar är endindex.
                int udx = startIndex + 1;
                int numSlotsCounter = 0;
                bool stillSoundAhead = true;
                // Nu fortsätter loopen ifall både nuvarande indexet är över threshold och ifall det fortfarande finns ljud över threshold
                // längre fram. Detta gjordes för att inte splitta dela upp ett enskilt ljud i flera mindre segment pga att tänk hur en
                // sinusvåg ibland korsar noll, i den punkten är ju värdet mindre än threshold och därför skulle loopen brytas utan
                // den kollen.
                while ((udx < recording.Length && System.Math.Abs(recording[udx]) > AfterSoundThreshold) || (udx < recording.Length && stillSoundAhead))
                {
                    // Ifall vi är i slutet av recording.
                    int howMuchAheadToLook = 2000; // In samples.
                    int indexAhead = udx + howMuchAheadToLook;
                    if(indexAhead > recording.Length)
                    {
                        int howMuchOverTheLength = indexAhead - recording.Length;
                        indexAhead = recording.Length - howMuchAheadToLook + howMuchOverTheLength;
                        //Debug.Log("index Ahead 2 = " + indexAhead);

                        // Kolla ifall det är ljud 300 ms framåt, isåfall fortsätter loopen, så den inte slutar prematurely och kapar ett ljud i flera bitar.
                        stillSoundAhead = (System.Math.Abs(recording[indexAhead]) > AfterSoundThreshold) ? true : false;
                    }
                    else
                    {
                        stillSoundAhead = (System.Math.Abs(recording[udx + howMuchAheadToLook]) > AfterSoundThreshold) ? true : false;
                    }
                    numSlotsCounter++;
                    udx++;

                    // Kanske ändra loopen så loopen fortsätter så länge en av de
                }

                // Minimum storlek dvs. hur kort ett ljud är i samples.
                int minLengthInSamples = 3000; // 3000 tar iaf med den sista jättekorta högsta oktav noten som är 500ms lång i STEREO.

                if (numSlotsCounter > minLengthInSamples * audioSource.clip.channels)
                {
                    endIndex = udx;
                    idx = endIndex;
                    idx++;

                    // Spara genom funktionskall som skicka indexen.
                    if(numSavedTrimmedSounds < 10)
                    {
                        allTrimmedSounds[numSavedTrimmedSounds] = GetSegmentFromRecording(startIndex, endIndex);
                        numSavedTrimmedSounds++;
                        Debug.Log("startIndex = " + startIndex);
                        Debug.Log("endIndex = " + endIndex);
                        Debug.Log("Sound segment saved.");

                        // Playing sound. // DEBUG, REMOVE LATER
                        if(numSavedTrimmedSounds == 4)
                        {
                            int clipPlaying = 3;
                            audioSource.clip = AudioClip.Create("Trimmed sound", allTrimmedSounds[clipPlaying].Length, audioSource.clip.channels, 44100, false);
                            audioSource.clip.SetData(allTrimmedSounds[clipPlaying], 0);
                            //audioSource.loop = true;
                            audioSource.Play();
                            Debug.Log("¨PLAYING CLIP: " + clipPlaying);
                        }
                    }
                    else
                    {
                        Debug.Log("MAX NR SAVED SOUNDS!");
                    }

                    continue;
                }
                else
                {
                    idx++;
                    continue; // Fortsätt iterera ett steg för klippet är för kort för att ens bry sig om.
                }               
            }
            idx++;
        }       

        Debug.Log("Kommer du hit så har koden inte fastnat iaf.");
    }

    private float[] GetSegmentFromRecording(int startIndex, int endIndex)
    {
        int lengthInSamples = endIndex - startIndex;
        float[] trimmedIndividualSound = new float[lengthInSamples];

        // Kopiera över ljudet
        for (int idx = 0; idx < lengthInSamples; idx++)
        {
            trimmedIndividualSound[idx] = recording[startIndex + idx];
        }

        return trimmedIndividualSound;
    }
    
	
	// Update is called once per frame
	void Update () {
		
	}




}
