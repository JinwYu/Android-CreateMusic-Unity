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

    private int numSoundsAllowedInLoop = 20;
    float[][] allTrimmedSounds;
    private int numSavedTrimmedSounds;
    int rangeToInvestigateInSamples;
    private float beforeSoundThreshold = 0.1f; // 0.1f // Ideala värdet för kickdrum loopen är iaf 0.00001f.
    private float afterSoundThreshold = 0.00006f; // 0.00006f;
    private float rmsValue;

    private int numSnapLimits; // 16 är default, men kanske borde ha 8? 4 fungerar inte för den kapar det sista ljudet.
    int[] originalStartIndices;
    int[] originalEndIndices;
    float[] newQuantizedLoop;
    private int numSnappedSounds;
    int[] snapLimits8Beats;
    int[] snapLimits16Beats;
    private int numSamplesWithSound;
    int snapintervaltest;

    float[] fadeInSegment;
    float[] fadeOutSegment;

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
                snapLimits16Beats = new int[16];
                int snapInterval8 = (int) recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels / numSnapLimits;
                int snapInterval16 = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels / 16;
                snapintervaltest = snapInterval8; // TODO: remove.                

                // Generate the indices of the snap limits.
                snapLimits8Beats = GenerateSnapLimits(snapInterval8, snapLimits8Beats);
                snapLimits16Beats = GenerateSnapLimits(snapInterval16, snapLimits16Beats);

                bool snapTo8beats;
                int thresholdDistance = 12000 / 2; // 1 beat är 12000, vi brukar snappa till 8 beats för bra ljud, men 1/2 beat om vi ska snappa ord till 16:delar.
                int distance;                

                // Iterates through each sound.
                for (int soundIndex = 0; soundIndex < numSavedTrimmedSounds; soundIndex++) 
                {   
                    // Calculate the distance between two sounds next to each other, measured in samples.
                    if (soundIndex == 0)
                        distance = originalStartIndices[soundIndex];  // Distance from zero to the first sound.
                    else
                    {
                        int prevEndIndex = originalEndIndices[soundIndex - 1];
                        int currentStartIndex = originalStartIndices[soundIndex];
                        distance = System.Math.Abs(prevEndIndex - currentStartIndex); // Distance between two sounds next to eachother measured in indices/samples.
                    }

                    // Check if the sound should be snapped to 8 beats or 8 beats / 2.
                    if (distance > thresholdDistance)
                    {
                        snapTo8beats = true;
                        Debug.Log("snapTo8Beats = " + true);
                    }
                    else
                    {
                        snapTo8beats = false;
                        Debug.Log("snapTo8Beats = " + false);
                    }

                    // Snap to 8 beats or 16 beats.
                    if (snapTo8beats)
                        FindLimitToSnapThenSnap(soundIndex, snapLimits8Beats);
                    else
                        FindLimitToSnapThenSnap(soundIndex, snapLimits16Beats);             
                }
            }
        }
        else
        {
            // DEBUG: Byta sprite här till en template för att visa att det är en silent recording.
            // Display a red error message.

            recordedLoops.silentRecording = true;
            Debug.Log("Silent recording, returning an empty recording.");
        }

        // Calculate the percentage of sound in the quantized loop. Borde egentligen bara ljud som snappats.
        Debug.Log("NUMSAMPLESWITHSOUND = " + numSamplesWithSound/2);
        Debug.Log("NEW LOOP LENGTH = " + newQuantizedLoop.Length);
        double percentageSoundInLoop = (double)numSamplesWithSound / 2 / newQuantizedLoop.Length; // 2 är numChannels, vet inte varför jag måste ha det här, men numSamplesWithSound blir annars alltid 355200 typ
        Debug.Log("PERCENTAGE SOUND IN LOOP = " + percentageSoundInLoop);

        if (numSnappedSounds > 0)
            

        // Return an empty recording if the recording was silent.
        if (recordedLoops.silentRecording)
        {
            newQuantizedLoop = new float[recording.Length];
            for (int i = 0; i < newQuantizedLoop.Length; i++)
                newQuantizedLoop[i] = 0.0f;
        }
        else
            recordedLoops.silentRecording = false;

        double percentageThreshold = 0.8;
        if(percentageSoundInLoop > percentageThreshold)
        {
            Debug.Log("Exceeded percentage threshold, sending loop to be gated.");
            ApplyGatingToLoop();
        }

        Debug.Log("After Quantization: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

        return newQuantizedLoop;
    }

    private void Init(float[] loopToQuantize)
    {
        rangeToInvestigateInSamples = recordedLoops.sampleRate * 2000 * (int)recordedLoops.numChannels / 1000; // Ges i samples. tid = 1ms.
        int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels; // In samples/indices.

        recording = new float[numSamplesInRecording];
        recording = loopToQuantize;

        // Regulate the threshold with the RMS value of the signal.
        beforeSoundThreshold = rmsValue / 10; // för start sound verkar /10 vara bra
        afterSoundThreshold = rmsValue / 100000; // för snare sound verkar /10 000 vara ett bra endvärde. 100 000

        // Temporary indices for a spliced segment.
        startIndex = 0;
        endIndex = 0;

        numSnapLimits = 8;
        numSavedTrimmedSounds = 0;
        numSnappedSounds = 0;
        numSamplesWithSound = 0; // Used for calculating percentage of sound in the final loop.

        // Create the fade vectors.
        fadeInSegment = new float[lengthOfFade];
        fadeOutSegment = new float[lengthOfFade];
        int x = lengthOfFade;
        for (int i = 0; i < lengthOfFade; i++)
        {
            fadeInSegment[i] = (float)i / lengthOfFade;
            fadeOutSegment[i] = (float)x / lengthOfFade;
            x--;
        }

        allTrimmedSounds = new float[numSoundsAllowedInLoop][];
        originalStartIndices = new int[numSoundsAllowedInLoop];
        originalEndIndices = new int[numSoundsAllowedInLoop];

        Debug.Log("INIT DONE");
    }

    private void FindLimitToSnapThenSnap(int soundIndex, int[] snapLimits)
    {
        Debug.Log("Inside FINDLIMITTOSNAPTHENSNAP");
        // Iterates through the snap limits.
        for (int j = 1; j < snapLimits.Length; j++)
        {
            // Check if the start index of the sound is within two limits to the left and the right.
            if (originalStartIndices[soundIndex] > snapLimits[j - 1] && originalStartIndices[soundIndex] < snapLimits[j])
            {
                // Compare the two distances to get the closest limit.
                int leftDistanceToLimit = System.Math.Abs(snapLimits[j - 1] - originalStartIndices[soundIndex]);
                int rightDistanceToLimit = System.Math.Abs(snapLimits[j] - originalStartIndices[soundIndex]);

                if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
                {
                    Debug.Log("Inside FindlimitTOSnapThenSnap, numsavedtrimmedsounds < numsoundsallowedloop");
                    if (leftDistanceToLimit < rightDistanceToLimit) // If closer to the snap limit to the left, else closer to the right.
                        SnapToLimit(j - 1, snapLimits); // Snap to the left limit.
                    else
                        SnapToLimit(j, snapLimits); // Snap to the right limit.
                }
                else
                {
                    Debug.Log("Breaks FINDLIMITTOSNAPTHENSNAP LOOP");
                    // Måste ha nånting som ersätter och returnar ett tyst ljud
                    recordedLoops.silentRecording = false;

                    break; // Break, exceeds number of allowed sounds in the loop.
                }
                    
            }
        }
    }

    private int[] GenerateSnapLimits(int snapInterval, int[] snapLimits)
    {
        for (int i = 1; i < snapLimits.Length; i++)
            snapLimits[i] = snapLimits[i - 1] + snapInterval - 1;

        return snapLimits;
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
                    endIndex = tempEndIndex;
                    idx = endIndex; // The while-loop will keep looping from the index of the end of the  most recently detected sound.
                    idx++;

                    // Save start and end indices.
                    originalStartIndices[numSavedTrimmedSounds] = startIndex;
                    originalEndIndices[numSavedTrimmedSounds] = endIndex;

                    numSamplesWithSound += endIndex - startIndex;// Sum the number of samples that contains sound information.
                    Debug.Log("numSamplesWithSound =" + numSamplesWithSound );

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
        Debug.Log("segment length = " + segment.Length);
        //numSamplesWithSound += segment.Length;

        // Fade in.
        for (int i = 0; i < lengthOfFade; i++)
            segment[i] = segment[i] * fadeInSegment[i];

        // Fade out.
        int indexToStartFadingOut = segment.Length - lengthOfFade;
        int k = 0;
        for (int i = indexToStartFadingOut; i < segment.Length; i++)
        {
            segment[i] = segment[i] * fadeOutSegment[k];
            k++;
        }

        return segment;
    }

    private void ApplyGatingToLoop()
    {
        Debug.Log("Starting to GATE.");
        float[] beatGate = new float[newQuantizedLoop.Length];

        int gateInterval = 1*12000;
        int divideBy = 8;
        int fadeDistance = gateInterval / divideBy;
        float[] fadeIn = new float[fadeDistance];
        float[] fadeOut = new float[fadeDistance];

        // Create fade vectors.
        int x = fadeDistance;
        for (int i = 0; i < fadeDistance; i++)
        {
            fadeIn[i] = (float)i / fadeDistance;
            fadeOut[i] = (float)x / fadeDistance;
            x--;
        }
        
        // Create the gate array.
        int k = 0;
        for (int i = 0; i < newQuantizedLoop.Length; i++)
        {
            if (k < gateInterval) // Where there should be sound.
            {
                // Fade in the gate.
                if(k < gateInterval / divideBy)
                {
                    for (int b = 0; b < fadeIn.Length; b++)
                        beatGate[i] *= 1 * fadeIn[b];
                }
                else if(k > ((divideBy-1)*gateInterval/ divideBy)) // Fade out the gate.
                {
                    for (int b = 0; b < fadeOut.Length; b++)
                        beatGate[i] *= 1 * fadeOut[b];
                }
                else // In the middle of the fades.
                {
                    beatGate[i] = 1.0f;
                }                
            }
            else if (k >= gateInterval && k < (2 * gateInterval)) // Where it should be silent.
            {
                beatGate[i] = 0.0f;
            }                
            else // Start over for the next part of the loop to be gated.
            {
                k = 0;
                continue;
            }

            k++;
        }

        Debug.Log("Generated the gate-array");

        // Gate the quantized loop.
        for (int j = 0; j < newQuantizedLoop.Length; j++)
            newQuantizedLoop[j] = newQuantizedLoop[j] * beatGate[j];

        Debug.Log("GATING DONE");
    }

    // Snaps one sound at the time, and "numSnappedSounds" is the current index of the sound which will be snapped.
    private void SnapToLimit(int limitIndex, int[] snapLimits)
    {
        Debug.Log("Inside SNAPTOLIMIT");
        if (numSnappedSounds < numSoundsAllowedInLoop && numSnappedSounds <= numSavedTrimmedSounds)
        {
            Debug.Log("Inside IF");
            int lengthOfSound = allTrimmedSounds[numSnappedSounds].Length;

            // Verkar vara fel att den vill snappa två gånger på den första snaplimit?
            if (limitIndex == 0)
            {
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
                // Men detta skulle inte lösa problemet med att den väljer att ta bort korta ljud i ett tal helt i onödan

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
            // FINNS DET SÄTT ATT HITTA VILKA SORTS LJUD SOM SKULLE FUNGERA BRA MED ATT HITTA TRANSIENTEN?????
            // Antingen har varje sound transient, fast att man söker inom ett mycket kortare intervall än vad det är satt till nu
            int numIndicesFromStartIndex = 0;

            int k = 0;
            for (int i = snapLimits[limitIndex]; k < lengthOfSound && i < newQuantizedLoop.Length; i++)
            {
                // In "allTrimmedSounds": 
                // 1st argument = Index for which of the individual sounds to snap, is constant here.
                // 2nd argument = Iterates through the sound segment.
                if (!(limitIndex == 0)) // Check if it's not the first sound to be snapped to avoid out of index errors.
                {
                    // Place the sound at a the given snap limit.
                    // "i - numIndicesFromStartIndex" makes the sound's transient start at the snap limit.
                    newQuantizedLoop[i - numIndicesFromStartIndex] = allTrimmedSounds[numSnappedSounds][k];
                }
                else
                    newQuantizedLoop[i] = allTrimmedSounds[numSnappedSounds][k];

                k++;
            }

            numSamplesWithSound += k; // Used for calculating percentage.

            numSnappedSounds++;
            Debug.Log("numSnappedSounds = " + numSnappedSounds);
        }
        else
        {
            Debug.Log("Exceeds number of allowed sounds!");
        }
        Debug.Log("At the end of SNAPTOLIMIT, debug i else should've been shown");
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



// KOD FÖR SÄTT ATT SNAPPA SÅ ALDRIG HAMNAR PÅ SAMMA LIMIT
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
