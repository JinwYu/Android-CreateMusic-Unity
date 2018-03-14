using UnityEngine;
using UnityEngine.UI;

// Class to split the audio into smaller parts by finding the transients.
[CreateAssetMenu]
public class Quantization : ScriptableObject
{
    [SerializeField]
    private RecordedLoops recordedLoops;

    private AudioSource audioSource;
    private float[] recording;

    private int numSoundsAllowedInLoop = 10;
    float[][] allTrimmedSounds;
    private int numSavedTrimmedSounds;
    int rangeToInvestigateInSamples;
    private float beforeSoundThreshold = 0.1f; // 0.1f // Ideala värdet för kickdrum loopen är iaf 0.00001f.
    private float afterSoundThreshold = 0.00006f; // 0.00006f;
    private float rmsValue;

    private int numSnapLimits; // 16 är default, men kanske borde ha 8? 4 fungerar inte för den kapar det sista ljudet.
    int[] originalStartIndices;
    float[] newQuantizedLoop;
    private int numSnappedSounds;
    int[] snapLimits8Beats;
    int[] snapLimits16Beats;
    int snapintervaltest;
    private bool alreadySnappedToFirstLimit;

    // Indices of the audio segment to extract from the recording.
    int startIndex;
    int endIndex;

    private int minLengthInSamples = 4000; //9000; //3000; // The allowed minimum length of a trimmed segment in the number of samples/indices.
    private int lengthOfFade = 2400;//1500; // In samples.



    //void Start()
    //{

    //    // DEBUG FÖR ATT HA ETT KLIPP MAN VILL UNDERSÖKA, TA BORT NÄR SKA IMPLEMENTERAS I APPEN

    //    audioSource = GetComponent<AudioSource>();

    //    rangeToInvestigateInSamples = 44100 * 1 * audioSource.clip.channels / 1000; // Ges i samples. tid = 1ms.

    //    int numAudioChannels = (audioSource.clip.channels == 2) ? 2 : 1; // Check if it's stereo (two channels), because it affects the time calculation.

    //    // Fetch the sound clip which has been assigned in the inspector.
    //    int numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels; // In samples/indices.
    //    recording = new float[numSamplesInRecording];
    //    audioSource.clip.GetData(recording, 0);

    //    // FUNKAR INTE MED SCRIPTABLE OBJECT
    //    // DEBUG TO PLAY THE SOUND

    //    recording = Quantize(recording);

    //    audioSource.clip = AudioClip.Create("Quantized sound", audioSource.clip.samples, audioSource.clip.channels, 44100, false);
    //    audioSource.clip.SetData(recording, 0);
    //    audioSource.loop = true;
    //    audioSource.Play();
    //    Debug.Log("PLAYING QUANTIZED LOOP!");

    //    //    //Debug.Log("Kommer du hit så har koden inte fastnat iaf.");
    //}


    public float[] Quantize(float[] loopToQuantize)
    {
        rmsValue = GetRMS(loopToQuantize); // Get the RMS of the recording.
        Debug.Log("rms = " + rmsValue);

        Init(loopToQuantize);
        //Debug.Log("After Init: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

        // Perform quantization if the recording was not silent, else send an empty recording back.
        float silentThreshold = 0.01f;
        if (rmsValue > silentThreshold)
        {
            DoSampleSplicing();
            Debug.Log("After Splicing: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

            if (numSavedTrimmedSounds > 0) // If there are any sounds to be quantized.
            {
                // Quantization.
                newQuantizedLoop = new float[recording.Length];
                snapLimits8Beats = new int[numSnapLimits]; // Saves the indices where a sound should snap to.
                int snapInterval = (int) recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels / numSnapLimits;
                snapintervaltest = snapInterval; // TODO: remove.

                snapLimits16Beats = new int[16];

                //Debug.Log("snapLimit[0] = " + snapLimits[0]);
                // Get the indices of the snap limits.
                for (int i = 1; i < numSnapLimits; i++)
                {
                    snapLimits8Beats[i] = snapLimits8Beats[i - 1] + snapInterval - 1;
                    //Debug.Log("snapLimit[" + i + "] = " + snapLimits[i]);
                }

                int snapInterval16 = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels / 16;
                for (int i = 1; i < 16; i++)
                {
                    snapLimits16Beats[i] = snapLimits16Beats[i - 1] + snapInterval16 - 1;
                    //Debug.Log("snapLimit[" + i + "] = " + snapLimits[i]);
                }


                if(numSavedTrimmedSounds < 7) // Kör 8 beats
                {
                    for (int soundIndex = 0; soundIndex < numSavedTrimmedSounds; soundIndex++) // Iterates through each sound.
                    {
                        for (int j = 1; j < snapLimits8Beats.Length; j++) // Iterates through the snap limits.
                        {
                            // Check if the start index of the sound is within two limits to the left and the right.
                            if (originalStartIndices[soundIndex] > snapLimits8Beats[j - 1] && originalStartIndices[soundIndex] < snapLimits8Beats[j])
                            {
                                // Compare the two distances to get the closest limit.
                                int leftDistanceToLimit = System.Math.Abs(snapLimits8Beats[j - 1] - originalStartIndices[soundIndex]);
                                int rightDistanceToLimit = System.Math.Abs(snapLimits8Beats[j] - originalStartIndices[soundIndex]);

                                /*
                                int soundLength = allTrimmedSounds[soundIndex].Length;

                                if(soundIndex == 0) // Om första ljudet i loopen,  No need to check previous sound's endIndex.
                                {
                                    // Snappa bara som inget har hänt på den första snap limit vilket är index = 0.
                                    SnapToLimit(j, false);
                                    Debug.Log("Snapped the first sound to the first limit.");
                                }
                                else // Inte det första ljudet, så måste kolla hur långt ljudet sträcker sig
                                {                                
                                    int endIndexForTheSound = originalStartIndices[soundIndex - 1] + soundLength;
                                    if (snapLimits8Beats[j] < endIndexForTheSound) // If sound is exceeding the limit to the right, go to the next snap limit. 
                                    {
                                        int halfSnapInterval = snapInterval / 2;
                                        if(snapLimits8Beats[j] + halfSnapInterval > endIndexForTheSound)
                                        {
                                            SnapToLimit(j, true);
                                            Debug.Log("SNAPPED TO HALF LIMIT, 16 BEATS.");
                                        }
                                        else
                                        {
                                            Debug.Log("Previous sound is still going on, can't snap here, so moving to the next snap limit");
                                            continue;
                                        }

                                    }
                                    else // Sound has ended before the next snap limit.
                                    {
                                        if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
                                        {
                                            SnapToLimit(j, false);
                                            Debug.Log("Snapped the sound:" + soundIndex + " to limitIndex = " + j);
                                        }
                                        else
                                        {
                                            soundIndex = numSavedTrimmedSounds; // Breaks the outer loop, ugly solution.
                                            Debug.Log("Breaking outer loop in the snap for-loops.");
                                            break; // Break, exceeds number of allowed sounds in the loop.
                                        }
                                    }



                                }*/

                                Debug.Log("SNAPPING 8 BEATS.");

                                if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
                                {
                                    if (leftDistanceToLimit < rightDistanceToLimit) // If closer to the snap limit to the left, else closer to the right.
                                        SnapToLimit(j - 1, false); // Snap to the left limit.
                                    else
                                        SnapToLimit(j, false); // Snap to the right limit.
                                }
                                else
                                {
                                    soundIndex = numSavedTrimmedSounds; // Breaks the outer loop, ugly solution.
                                    break; // Break, exceeds number of allowed sounds in the loop.
                                }
                            }
                        }
                    }
                }
                else // Kör 16 beats
                {
                    for (int soundIndex = 0; soundIndex < numSavedTrimmedSounds; soundIndex++) // Iterates through each sound.
                    {
                        for (int j = 1; j < snapLimits16Beats.Length; j++) // Iterates through the snap limits.
                        {
                            // Check if the start index of the sound is within two limits to the left and the right.
                            if (originalStartIndices[soundIndex] > snapLimits16Beats[j - 1] && originalStartIndices[soundIndex] < snapLimits16Beats[j])
                            {
                                Debug.Log("SNAPPING 16 BEATS.");

                                // Compare the two distances to get the closest limit.
                                int leftDistanceToLimit = System.Math.Abs(snapLimits16Beats[j - 1] - originalStartIndices[soundIndex]);
                                int rightDistanceToLimit = System.Math.Abs(snapLimits16Beats[j] - originalStartIndices[soundIndex]);

                                if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
                                {
                                    if (leftDistanceToLimit < rightDistanceToLimit) // If closer to the snap limit to the left, else closer to the right.
                                        SnapToLimit(j - 1, true); // Snap to the left limit.
                                    else
                                        SnapToLimit(j, true); // Snap to the right limit.
                                }
                                else
                                {
                                    soundIndex = numSavedTrimmedSounds; // Breaks the outer loop, ugly solution.
                                    break; // Break, exceeds number of allowed sounds in the loop.
                                }
                            }
                        }
                    }
                }

                

                recordedLoops.silentRecording = false;
            }
            else
            {
                // DEBUG: Byta sprite här till en template för att visa att det är en silent recording.
                // Display a red error message.

                newQuantizedLoop = new float[recording.Length]; // Return empty if no sounds were trimmed.
                for (int i = 0; i < newQuantizedLoop.Length; i++)
                    newQuantizedLoop[i] = 0.0f;

                recordedLoops.silentRecording = true;

                Debug.Log("Returning empty recording.");
            }

        }
        else
        {
            // DEBUG: Byta sprite här till en template för att visa att det är en silent recording.
            // Display a red error message.

            // Return an empty recording if the recording was silent.
            newQuantizedLoop = new float[recording.Length];
            for(int i = 0; i < newQuantizedLoop.Length; i++)
                newQuantizedLoop[i] = 0.0f;

            recordedLoops.silentRecording = true;

            Debug.Log("Silent recording, returning an empty recording.");
        }

        Debug.Log("After Quantization: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

        return newQuantizedLoop;
    }


    private void Init(float[] loopToQuantize)
    {
        // KOD FÖR IMPLEMENTATION AV QUANTIZATION SOM SCRIPTABLE OBJECT, ÄNDRA CLIP CHANNELS TILL NÅT MED ATT LÄGGA NUM CHANNELS I RECORDED LOOPS
        rangeToInvestigateInSamples = recordedLoops.sampleRate * 2000 * (int)recordedLoops.numChannels / 1000; // Ges i samples. tid = 1ms.
        int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels; // In samples/indices.
        //rangeToInvestigateInSamples = 44100 * 1 * (int)audioSource.clip.channels / 1000; // Ges i samples. tid = 1ms.
        //int numSamplesInRecording = (int)audioSource.clip.samples * (int)audioSource.clip.channels;
        recording = new float[numSamplesInRecording];
        //Debug.Log("numSamples = " + numSamplesInRecording);
        //Debug.Log("loopToQuantize.Length = " + loopToQuantize.Length);

        //// TODO: Temporary code to force the size of the recording to 4 seconds if 8 beats
        //int seconds = 4;
        //int numSamplesWhen8beats = seconds * recordedLoops.sampleRate;
        //recording = new float[numSamplesWhen8beats];

        recording = loopToQuantize;
        numSnapLimits = 8;
        //numSnapLimits = 16;
        alreadySnappedToFirstLimit = false;

        // Regulate the threshold with the RMS value of the signal.
        beforeSoundThreshold = rmsValue / 10; // för start sound verkar /10 vara bra
        afterSoundThreshold = rmsValue / 100000; // för snare sound verkar /10 000 vara ett bra endvärde. 100 000
        //Debug.Log("RMS = " + rmsValue);
        //Debug.Log("BeforeSoundThresh = " + beforeSoundThreshold);
        //Debug.Log("AfterSoundThresh = " + afterSoundThreshold);

        startIndex = 0;
        endIndex = 0;

        numSavedTrimmedSounds = 0;
        numSnappedSounds = 0;

        allTrimmedSounds = new float[numSoundsAllowedInLoop][];
        originalStartIndices = new int[numSoundsAllowedInLoop];

        Debug.Log("INIT DONE");
    }

    private void DoSampleSplicing()
    {
        // The loop below detects relevant sounds in the recording, extracts them and saves them in smaller segments.
        // The loop iterates through the recording and checks if there is audio within a chosen range
        // to see if the values goes from i.e. zero to a value above a threshold. If that occurs, then a sound is beginning.
        int idx = 0;
        while (idx < recording.Length)
        {
            bool isQuietBeforeTransient = IsItQuietBeforeTheTransient(idx);
            bool soundExistsAfterTransient = DoesSoundExistsAfterTransient(idx);

            // If a transient is found. (Means that a sound begins since the value goes from zero to something).
            if (idx >= endIndex && isQuietBeforeTransient && soundExistsAfterTransient)
            {
                startIndex = GetStartIndex(idx); // Find the index of where within the range the sound begins.
                int tempEndIndex = GetEndIndex(startIndex); // Detect the duration of the sound starting from where the transient is.

                int durationOfSegmentInSamples = tempEndIndex - startIndex;

                // Save the trimmed audio segment if it exceeds the allowed minimum length.
                if ((durationOfSegmentInSamples > (minLengthInSamples * recordedLoops.numChannels)) && (numSavedTrimmedSounds < numSoundsAllowedInLoop))
                {
                    //Debug.Log("durationOfSegmentInSamples = " + durationOfSegmentInSamples);
                    //Debug.Log("minlength = " + minLengthInSamples);
                    //Debug.Log("numchannels * minlength= " + recordedLoops.numChannels * minLengthInSamples);

                    endIndex = tempEndIndex;
                    idx = endIndex; // The while-loop will keep looping from the index of the end of the  most recently detected sound.
                    idx++;

                    //Debug.Log("numSavedTrimmed = " + numSavedTrimmedSounds);
                    //Debug.Log("originalStartIndices.Length = " + originalStartIndices.Length);
                    originalStartIndices[numSavedTrimmedSounds] = startIndex; // Save start indices for each saved segment for later use during quantization.

                    SaveTrimmedAudioSegment(startIndex, endIndex);

                    continue;
                }
                else // The audio segment is too short.
                {
                    //idx++;
                    idx = tempEndIndex;
                    idx++;
                    continue;
                }
            }
            idx++;
        }

        //Debug.Log("SAMPLESPLICING DONE");
    }

    private bool IsItQuietBeforeTheTransient(int idx)
    {
        return (System.Math.Abs(recording[idx]) < beforeSoundThreshold) ? true : false;
    }

    private bool DoesSoundExistsAfterTransient(int idx)
    {
        bool soundExistsAfterTransient;

        // Investigate if a sound appears in the recording by looking at the current index and an index further ahead.
        if (idx < recording.Length - rangeToInvestigateInSamples)
        {
            soundExistsAfterTransient = (System.Math.Abs(recording[idx + rangeToInvestigateInSamples]) > afterSoundThreshold) ? true : false;
        }
        else
        {
            // Regulating the index that looks ahead of the current index to keep it from exceeding the range of the array.
            int indexAhead = idx + rangeToInvestigateInSamples;
            int howMuchOverTheLength = indexAhead - recording.Length;
            indexAhead = recording.Length - rangeToInvestigateInSamples + howMuchOverTheLength;

            soundExistsAfterTransient = (System.Math.Abs(recording[indexAhead]) > afterSoundThreshold) ? true : false;
        }

        return soundExistsAfterTransient;
    }

    private int GetStartIndex(int idx)
    {
        int startIndex;
        int j = 0;

        // Goes through the range and detects when sound begins.
        while ((idx + j) < recording.Length && System.Math.Abs(recording[idx + j]) < beforeSoundThreshold)
        {
            j++;
        }
        startIndex = idx + j;

        return startIndex;
    }

    private int GetTransientIndex(int startIndex)
    {
        // One beat is 9600 samples.
        // Search through the first beat to find the index with the max value within it.
        float maxValue = 0;
        int transientIndex = 0;
        for (int k = 0; k < 4800 && (startIndex + k) < recording.Length; k++) // Kanske borde testa 9600 / 2 = 4800 eller 2400.
        {
            if (recording[startIndex + k] > maxValue)
            {
                maxValue = recording[startIndex + k];                
                transientIndex = startIndex + k; // Save the index with the max value.
            }
        }
        return transientIndex;
    }

    private int GetEndIndex(int startIndex)
    {
        int idx = startIndex + 1; // The index to start from in the recording.
        bool stillSoundAhead = true;

        // The while-loop will continue until the sound ends. It checks if there is a sound value at the current index and
        // at an index further ahead. This is to minimize the risk of dividing a single sound into even smaller segments.
        while ((idx < recording.Length && stillSoundAhead))
        {
            int howMuchAheadToLook = 2000; // In samples. 1600
            int indexAhead = idx + howMuchAheadToLook;
            if (indexAhead > recording.Length)
            {
                int howMuchOverTheLength = indexAhead - recording.Length;
                indexAhead = recording.Length - howMuchAheadToLook + howMuchOverTheLength;

                stillSoundAhead = (System.Math.Abs(recording[indexAhead]) > afterSoundThreshold) ? true : false;
            }
            else
            {
                stillSoundAhead = (System.Math.Abs(recording[idx + howMuchAheadToLook - 1]) > afterSoundThreshold) ? true : false;
            }
            idx++;
        }
        return idx;
    }

    private void SaveTrimmedAudioSegment(int startIndex, int endIndex)
    {
        //Debug.Log("INSIDE SAVETREIMMEDAUDIOSEGMENT");

        if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
        {
            allTrimmedSounds[numSavedTrimmedSounds] = GetSegmentFromRecording(startIndex, endIndex);
            numSavedTrimmedSounds++;
            Debug.Log("Inside saveTrimmedSOunds, numSavedTrimmedSounds = " + numSavedTrimmedSounds);
            //Debug.Log("startIndex = " + startIndex);
            //Debug.Log("endIndex = " + endIndex);
            Debug.Log("Sound segment saved.");

            // Playing sound. // DEBUG, REMOVE LATER
            if (numSavedTrimmedSounds == 4)
            {
                //int clipPlaying = 1;
                //audioSource.clip = AudioClip.Create("Trimmed sound", allTrimmedSounds[clipPlaying].Length, audioSource.clip.channels, 44100, false);
                //audioSource.clip.SetData(allTrimmedSounds[clipPlaying], 0);
                //audioSource.loop = true;
                //audioSource.Play();
                //Debug.Log("¨PLAYING CLIP: " + clipPlaying);
            }
        }
        else
        {
            Debug.Log("MAX NR SAVED SOUNDS!");
        }
    }

    private float[] GetSegmentFromRecording(int startIndex, int endIndex)
    {
        int lengthInSamples = endIndex - startIndex;
        float[] trimmedIndividualSound = new float[lengthInSamples];

        // Copy the sound over from the recording.
        for (int idx = 0; idx < lengthInSamples; idx++)
            trimmedIndividualSound[idx] = recording[startIndex + idx];

        // Fade the start and ending of the segment.      
        return FadeRecording(trimmedIndividualSound); ;
    }

    private float[] FadeRecording(float[] segment)
    {
        //int lengthOfFade = (int)segment.Length / 5;

        Debug.Log("segment length = " + segment.Length);

        //if (segment.Length < lengthOfFade)
        //{
        //    return new float[1];
        //}

        // Kan error bero på det? Nånting att algoritmen för endindex fortsätter? trots att längden har gått?

        // TODO: hitta max inom den första beaten, och välja det indexet som ska snappas, men
        // så klart behålla det som är framför som skjuts fram, kommer behöva akta om snappas
        // första snap limit för man kan inte gå innan index noll ju
        /*
        if(segment.Length > 19200) // Larger than one two bars.
        {
            Debug.Log("Starting to GATE.");
            float[] gateTheFourthBeat = new float[segment.Length];

            // samples = time * fs; ---> 38400 samples / 4 = 

            // 9600 / 4 = 2400, 9600 / 2 = 4800

            // Generate an array for "gating" the loop.
            // Makes the fourth beat silent.
            int b = 0;
            for(int i  = 0; i < segment.Length; i++)
            {
                // Måste fadea in och ut efter varje gate 

                if(b < 38400)
                {
                    gateTheFourthBeat[i] = 1.0f;
                }
                else if( b >= 38400 && b < 48000) // For i > 1500 to 2000.
                {
                    gateTheFourthBeat[i] = 0.0f;
                }
                else // b > 2000
                {
                    b = 0;
                    continue;
                }             

                b++;
            }
            Debug.Log("Generated the gate-array");

            // Gate the segment.
            for(int j = 0; j < segment.Length; j++)
            {
                segment[j] = segment[j] * gateTheFourthBeat[j];
            }

            Debug.Log("GATING DONE");
        }
        */

        float[] fadeIn = new float[lengthOfFade];
        float[] fadeOut = new float[lengthOfFade];

        int x = lengthOfFade;
        for (int i = 0; i < lengthOfFade; i++)
        {
            fadeIn[i] = (float)i / lengthOfFade;
            fadeOut[i] = (float)x / lengthOfFade;

            x--;
        }

        // Fade in.
        for (int i = 0; i < lengthOfFade; i++)
            segment[i] = segment[i] * fadeIn[i];

        //// Fade out.
        int indexToStartFadingOut = segment.Length - lengthOfFade;
        int k = 0;
        for (int i = indexToStartFadingOut; i < segment.Length; i++)
        {
            segment[i] = segment[i] * fadeOut[k];
            //Debug.Log("fadeOut[" + k + "] = " + fadeOut[k]);
            k++;
        }

        return segment;
    }

    private void SnapToLimit(int limitIndex, bool snapTo16Beats)
    {
        if (numSnappedSounds < numSoundsAllowedInLoop && numSnappedSounds <= numSavedTrimmedSounds)
        {
            int lengthOfSound = allTrimmedSounds[numSnappedSounds].Length;

            //Debug.Log("newQuantizedLoop Length = " + newQuantizedLoop.Length);

            // Verkar vara fel att den vill snappa två gånger på den första snaplimit?
            if (limitIndex == 0)
            {
                alreadySnappedToFirstLimit = true;
                Debug.Log("SNAPPAR TILL FÖRSTA SNAPLIMIT");

                // Det blir nog pops och clicks när den snappar ett ljud till samma snap limit
                // algoritmen skriver liksom över det ljud som snappades först på samma limit
                // dessa ljud kan ha olika längder och så hör man början av det korta ljudet
                // sedan började det andra ljudet jätte abrupt för två ljud är mixade ihop.
                // HUR LÖSER MAN DETTA DÅ?
                // jo, kanske genom att räkna hur många ljud som snappats till ett index
                // Jämföra längd mellan dessa två ljud, och behålla det längsta?
                // Snappa det längsta ljudet till limiten
                // Men detta blir ju sjukt ooptimerat eftersom man typ måste radera det ljud man
                // redan kopierat? sämst ju

                // Ovan kan lösas genom att spara alla ljud i hela inspelningen
                // Välja de 8 med störst längd och lagra deras startIndex/transientIndex 

                // Alternativ är annars att göra:
                // Om redan en snap på en limit, sätt på 16 bitar? Kanske låter skit dock

                // Eller: Eller spara alla klipp i hela inspelningen, men spara bara 
                // de 8 största, sen snappa de inte på en plats de redan varit på

                // Eller: Bestämma en fixed längd på ett ljud? Så ljud får bara vara en viss längd? 
                // Kommer det bli för många hack? Men kanske är värt för att det ska låta rent

                // Spara indexet på snappade ljudet innan
                // Om indexet är samma som prevSnappedSoundIndex
                // Fade:a ljudet som tidigare snappats och nollställ allt efter med en loop som går så långt som
                // allTrimmedSounds[numSnappedSound - 1].Length och sätt likamed noll.
                // Sätt nuvarande ljud till en 16:del limit på den 16:del som kommer närmast så långt jag fade:a på
                // ljudet innan.

                // HELST AV ALLT VILL JAG UNDVIKA ATT SNAPPAR TILL SAMMA LIMIT OCH DÄRMED
                // SKRIVER ÖVER LJUD HELT I ONÖDAN SÅ LJUDEN MED OLIKA LÄNGD JU MIXAS IHOP
                // OCH SKAPAR POPS & CLICKS FÖR ÖVERGÅNGEN FRÅN DET KORTA LJUDET TILL DET
                // LÅNGA LJUDET ÄR ABRUPT OCH ICKE FADE:AD. SKAPAS DÅ KONSTIGA LJUD SOM
                // HACKAR OCH FOLK FATTAR INTE VAD SOM HÄNT MED LOOPEN

                // Snappa till första 16:del limit efter det första ljudet har slutat
                // Generera 16:dels limits
                // int endIndexOfPrevSnappedSound = snapLimit[limitIndex] + allTrimmedSounds[numSnappedSounds - 1].Length;
                // Jämföra detta endIndex med 16:dels limits
                // och Snappa till närmaste index snap limit

                // Kolla längden av ljudet som ska snappas med allTrimmedSounds[numSnappedSounds - 1].Length
                // Redan innan skickar snapindex till denna funktion (SnapToLimit)
                // Kolla så snapLimits[i] > 

            }

            int startIndexOfSoundToBeSnapped = originalStartIndices[numSnappedSounds];
            int transientIndex = GetTransientIndex(startIndexOfSoundToBeSnapped); // Get transient Index.
            //int numIndicesFromStartIndex = transientIndex - startIndexOfSoundToBeSnapped;
            int numIndicesFromStartIndex = 0;

            if (!snapTo16Beats) // Snap to 8 beats.
            {
                // Snap the sound to the limit.
                // for (int i = snapLimits[limitIndex]; i < lengthOfSound + snapLimits[limitIndex] && i < newQuantizedLoop.Length; i++)
                int k = 0;
                for (int i = snapLimits8Beats[limitIndex]; k < lengthOfSound && i < newQuantizedLoop.Length; i++)
                {
                    // In "allTrimmedSounds": 
                    // 1st argument = Index for which of the individual sounds to snap, is constant here.
                    // 2nd argument = Iterates through the sound segment.
                    if (!(limitIndex == 0)) // Check if it's not the first sound to be snapped to avoid out of index errors.
                    {
                        // Just nu kan den gå in här flera gånger ju, för om ljud nr 2, ska snappas till den första limiten
                        // vid index noll, så är ju numSnappedSounds = 2, och då går den in här och försöker korrigera
                        // index med transientIndexet.

                        // Place the sound at a the given snap limit.
                        // "i - numIndicesFromStartIndex" makes the sound's transient start at the snap limit.
                        newQuantizedLoop[i - numIndicesFromStartIndex] = allTrimmedSounds[numSnappedSounds][k];
                    }
                    else
                    {
                        // HUR GÅR DET INDEX OUT OF RANGE???
                        newQuantizedLoop[i] = allTrimmedSounds[numSnappedSounds][k];
                    }

                    k++;
                }
                numSnappedSounds++;
                Debug.Log("numSnappedSounds = " + numSnappedSounds);
            }
            else // Snap to 16 beats
            {   
                // Snap the sound to the limit.
                // for (int i = snapLimits[limitIndex]; i < lengthOfSound + snapLimits[limitIndex] && i < newQuantizedLoop.Length; i++)
                int k = 0;
                for (int i = snapLimits16Beats[limitIndex]; k < lengthOfSound && i < newQuantizedLoop.Length; i++)
                {
                    // In "allTrimmedSounds": 
                    // 1st argument = Index for which of the individual sounds to snap, is constant here.
                    // 2nd argument = Iterates through the sound segment.
                    if (!(limitIndex == 0)) // Check if it's not the first sound to be snapped to avoid out of index errors.
                    {
                        // Just nu kan den gå in här flera gånger ju, för om ljud nr 2, ska snappas till den första limiten
                        // vid index noll, så är ju numSnappedSounds = 2, och då går den in här och försöker korrigera
                        // index med transientIndexet.

                        // Place the sound at a the given snap limit.
                        // "i - numIndicesFromStartIndex" makes the sound's transient start at the snap limit.
                        newQuantizedLoop[i] = allTrimmedSounds[numSnappedSounds][k];
                    }
                    else
                    {
                        // HUR GÅR DET INDEX OUT OF RANGE???
                        newQuantizedLoop[i] = allTrimmedSounds[numSnappedSounds][k];
                    }

                    k++;
                }
                numSnappedSounds++;
                Debug.Log("numSnappedSounds = " + numSnappedSounds);
            }

            
        }
        else
        {
            Debug.Log("Number allowed sounds has been exceeded!");
        }
    }

    public float GetRMS(float[] recording)
    {
        //float DbValue;
        //float RefValue = 0.1f;
        float sum = 0;

        for (int i = 0; i < recording.Length; i++)
            sum += recording[i] * recording[i]; // sum squared samples

        rmsValue = Mathf.Sqrt(sum / recording.Length); // rms = square root of average
        //DbValue = 20 * Mathf.Log10(RmsValue / RefValue); // calculate dB

        return rmsValue;
    }
}
