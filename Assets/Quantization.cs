using UnityEngine;

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
    float[] newQuantizedLoop;
    private int numSnappedSounds;
    int[] snapLimits;
    int snapintervaltest;

    // Indices of the audio segment to extract from the recording.
    int startIndex;
    int endIndex;

    private int minLengthInSamples = 4000; //9000; //3000; // The allowed minimum length of a trimmed segment in the number of samples/indices.
    private int lengthOfFade = 1000;//1500; // In samples.

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
                snapLimits = new int[numSnapLimits]; // Saves the indices where a sound should snap to.
                int snapInterval = (int) recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels / numSnapLimits;
                snapintervaltest = snapInterval; // TODO: remove.
                // Get the indices of the snap limits.
                for (int i = 1; i < numSnapLimits; i++)
                    snapLimits[i] = snapLimits[i - 1] + snapInterval;

                for (int i = 0; i < numSavedTrimmedSounds; i++) // Iterates through each sound.
                {
                    for (int j = 1; j < snapLimits.Length; j++) // Iterates through the snap limits.
                    {
                        // Check if the start index of the sound is within two limits to the left and the right.
                        if (originalStartIndices[i] > snapLimits[j - 1] && originalStartIndices[i] < snapLimits[j])
                        {
                            // Compare the two distances to get the closest limit.
                            int leftDistanceToLimit = System.Math.Abs(snapLimits[j - 1] - originalStartIndices[i]);
                            int rightDistanceToLimit = System.Math.Abs(snapLimits[j] - originalStartIndices[i]);

                            if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
                            {
                                if (leftDistanceToLimit < rightDistanceToLimit) // If closer to the snap limit to the left, else closer to the right.
                                    SnapToLimit(j - 1);
                                else
                                    SnapToLimit(j);
                            }
                            else
                            {
                                i = numSavedTrimmedSounds; // Breaks the first loop, ugly solution.
                                break; // Break, exceeds number of allowed sounds in the loop.
                            }
                        }
                    }
                }
            }
            else
            {
                newQuantizedLoop = new float[recording.Length]; // Return empty if no sounds were trimmed.
                Debug.Log("Returning empty recording.");
            }

        }
        else
        {
            // Return an empty recording if the recording was silent.
            newQuantizedLoop = new float[recording.Length];
            Debug.Log("Silent recording, returning an empty recording.");
        }

        Debug.Log("After Quantization: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

        return newQuantizedLoop;
    }

    private void Init(float[] loopToQuantize)
    {
        // KOD FÖR IMPLEMENTATION AV QUANTIZATION SOM SCRIPTABLE OBJECT, ÄNDRA CLIP CHANNELS TILL NÅT MED ATT LÄGGA NUM CHANNELS I RECORDED LOOPS
        rangeToInvestigateInSamples = recordedLoops.sampleRate * 1 * (int)recordedLoops.numChannels / 1000; // Ges i samples. tid = 1ms.
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
        {
            trimmedIndividualSound[idx] = recording[startIndex + idx];
        }

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

        float[] fadeIn = new float[lengthOfFade];
        float[] fadeOut = new float[lengthOfFade];

        int x = lengthOfFade;
        for (int i = 0; i < lengthOfFade; i++)
        {
            fadeIn[i] = (float)i / lengthOfFade;
            fadeOut[i] = (float)x / lengthOfFade;

            x--;
        }

        //// Fade in.
        for (int i = 0; i < lengthOfFade; i++)
        {
            //if (i < 500)
            //{
            //    float b = segment[i] * fadeIn[i];
            //    Debug.Log("segment[" + i + "] = " + segment[i]);
            //    Debug.Log("segment[" + i + "] * fadeIn[i] = " + b);
            //}
            segment[i] = segment[i] * fadeIn[i];
            //Debug.Log("segment[" + i + "]´= " + segment[i]);
        }

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

    private void SnapToLimit(int limitIndex)
    {
        if (numSnappedSounds < numSoundsAllowedInLoop && numSnappedSounds <= numSavedTrimmedSounds)
        {
            int lengthOfSound = allTrimmedSounds[numSnappedSounds].Length;

            Debug.Log("newQuantizedLoop Length = " + newQuantizedLoop.Length);

            if (limitIndex == 0)
            {
                Debug.Log("SNAPPAR TILL FÖRSTA SNAPLIMIT");
            }

            // Snap the sound to the limit.
            for (int i = snapLimits[limitIndex]; i < lengthOfSound + snapLimits[limitIndex]; i++)
            {
                //Debug.Log("i = " + i);
                //int temp = i - snapLimits[limitIndex];
                //Debug.Log("i - snapLimits[limitIndex] = " + temp);
                // Copy the sound over to the quantized position.

                

                newQuantizedLoop[i] = allTrimmedSounds[numSnappedSounds][i - snapLimits[limitIndex]];
            }
            numSnappedSounds++;
            Debug.Log("numSnappedSounds = " + numSnappedSounds);
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
