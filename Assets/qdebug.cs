﻿using UnityEngine;

// Class to split the audio into smaller parts by finding the transients.
public class qdebug : MonoBehaviour
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

    int numSnapLimits = 8; // 16 är default, men kanske borde ha 8? 4 fungerar inte för den kapar det sista ljudet.
    int[] originalStartIndices;
    float[] newQuantizedLoop;
    private int numSnappedSounds = 0;
    int[] snapLimits;

    // Indices of the audio segment to extract from the recording.
    int startIndex;
    int endIndex;

    private int minLengthInSamples = 4000; //9000; //3000; // The allowed minimum length of a trimmed segment in the number of samples/indices.
    private int lengthOfFade = 1000;//1500; // In samples.

    //NYTT
    float silenceThreshold;
    float NyafterSoundThreshold;

    void Start () {

        // DEBUG FÖR ATT HA ETT KLIPP MAN VILL UNDERSÖKA, TA BORT NÄR SKA IMPLEMENTERAS I APPEN
        
        audioSource = GetComponent<AudioSource>();  

        int numAudioChannels = (audioSource.clip.channels == 2) ? 2 : 1; // Check if it's stereo (two channels), because it affects the time calculation.

        // Fetch the sound clip which has been assigned in the inspector.
        int numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels; // In samples/indices.
        recording = new float[numSamplesInRecording];
        audioSource.clip.GetData(recording, 0);

        //Debug.Log("numSamples = " + numSamplesInRecording);
        //Debug.Log("recording.Length = " + recording.Length);

        // NYTT (finns redan i Init i riktiga scriptet)
        silenceThreshold = GetRMS(recording) / 3;
        NyafterSoundThreshold = GetRMS(recording) / 100000;
        recording = FindIndividualSounds(recording);


        //audioSource.clip = AudioClip.Create("Quantized sound", audioSource.clip.samples, audioSource.clip.channels, 44100, false);
        //audioSource.clip.SetData(recording, 0);
        //audioSource.loop = true;
        //audioSource.Play();
        //Debug.Log("PLAYING QUANTIZED LOOP!");
    }

    public float[] FindIndividualSounds(float[] loopToExtractSoundsFrom)
    {
        int fs = audioSource.clip.frequency / 2;
        double frameDuration = 0.03; // In seconds.
        double frameLengthDouble = System.Math.Floor(frameDuration * fs);
        int frameLength = (int)frameLengthDouble;
        int numSamples = loopToExtractSoundsFrom.Length;
        double numFrames = System.Math.Floor((double)numSamples / frameLength);

        int numConsecutiveFramesWithSound = 0;
        int numFramesThreshold = 10;
        bool hasSoundEnded = false;

        // Debug (remove)
        float[] tempNotQuanLoop = new float[loopToExtractSoundsFrom.Length];
        for (int i = 0; i < tempNotQuanLoop.Length; i++)
            tempNotQuanLoop[i] = 0;

        int tempStartIndex = 0;
        // Iterate through each frame.
        for (int frameIterator = 1; frameIterator < numFrames; frameIterator++)
        {
            // Create frames.
            int startFrameIndex = (frameIterator - 1) * frameLength + 1;
            //Debug.Log("startFrameIndex = " + startFrameIndex);
            int endFrameIndex = frameLength * frameIterator;
            float[] frame = new float[frameLength];
            System.Array.Copy(loopToExtractSoundsFrom, startFrameIndex, frame, 0, frameLength);

            // Get the RMS-value for the frame.
            float frameRMSValue = GetRMS(frame);
            
            // Check if there is sound in the frame.
            if (frameRMSValue > silenceThreshold)
            {
                if (numConsecutiveFramesWithSound == 0) // Keep track of the index of the beginning of the sound.              
                    tempStartIndex = startFrameIndex;

                numConsecutiveFramesWithSound++;
                hasSoundEnded = false;
                Debug.Log("numConsecutiveFramesWithSound = " + numConsecutiveFramesWithSound);
            }
            else if (numConsecutiveFramesWithSound > 0) // If a sound has ended.
                hasSoundEnded = true;

            // Handle a special case when the sound continues to the end of the recording.
            int tempEndIndexSound;
            bool entireRecordingIsSegment = false;
            if (frameIterator == numFrames - 1 && hasSoundEnded == false) 
                entireRecordingIsSegment = true;

            bool segmentIsLargeEnough = (numConsecutiveFramesWithSound >= numFramesThreshold);

            // Extract the segment, then process it and quantize it.
            if ((segmentIsLargeEnough && hasSoundEnded) || entireRecordingIsSegment)
            {
                int startIndexSound = GetStartIndex(tempStartIndex);
                
                if (entireRecordingIsSegment)
                    tempEndIndexSound = tempNotQuanLoop.Length - 1;
                else
                    tempEndIndexSound = tempStartIndex + (numConsecutiveFramesWithSound * frameLength);

                // Find the exact end index.
                int endIndexSound =  tempEndIndexSound;
                for (int i = tempEndIndexSound; i > startIndexSound; i--)
                {
                    if (loopToExtractSoundsFrom[i] > afterSoundThreshold)
                    {
                        endIndexSound = i;
                        break;
                    }
                }

                // Extract the individual sound segment.
                int segmentLength = System.Math.Abs(endIndexSound - startIndexSound);
                Debug.Log("STARTIDX = " + tempStartIndex + " , ENDIDX = " + endIndexSound + " , segmentLength = " + segmentLength);
                float[] segment = new float[segmentLength];
                System.Array.Copy(loopToExtractSoundsFrom, startIndexSound, segment, 0, segmentLength);

                /*
                // Split a segment further.
                int numFramesSegmentThreshold = 15;
                int thresholdInSamples = frameLength * numFramesSegmentThreshold;
                if (segment.Length > thresholdInSamples)
                {
                    float segmentRMSValue = GetRMS(segment);

                    double newFrameDuration = 0.007; // In seconds.
                    double newFrameLengthDouble = System.Math.Floor(newFrameDuration * fs);
                    int newFrameLength = (int)newFrameLengthDouble;
                    int newNumSamples = segment.Length;
                    double newNumFrames = System.Math.Floor((double)newNumSamples / newFrameLength);

                    int newTempEndIndexSound;
                    int newNumConsecutiveFramesWithSound = 0;
                    bool newHasSoundEnded = false;
                    bool newSoundContinuesToEndOfRecording = false;
                    int newTempStartIndex = 0;

                    for (int i = 1; i < newNumFrames; i++)
                    {
                        // Create frames.
                        int newStartFrameIndex = (i - 1) * frameLength + 1;
                        //Debug.Log("startFrameIndex = " + startFrameIndex);
                        int newEndFrameIndex = frameLength * i;
                        float[] newFrame = new float[newFrameLength];
                        System.Array.Copy(segment, newStartFrameIndex, newFrame, 0, newFrameLength);

                        // Get the RMS-value for the frame.
                        float newFrameRMSValue = GetRMS(newFrame);

                        // Check if there is sound in the frame.
                        if (newFrameRMSValue > (segmentRMSValue / 6))
                        {
                            if (newNumConsecutiveFramesWithSound == 0) // Keep track of the index of the beginning of the sound.              
                                newTempEndIndexSound = newStartFrameIndex;

                            newNumConsecutiveFramesWithSound++;
                            newHasSoundEnded = false;
                            Debug.Log("numConsecutiveFramesWithSound = " + numConsecutiveFramesWithSound);
                        }
                        else if (newNumConsecutiveFramesWithSound > 0) // If a sound has ended.
                            newHasSoundEnded = true;

                        // Handle a special case when the sound continues to the end of the recording.
                        if (frameIterator == numFrames - 1 && hasSoundEnded == false)
                            newSoundContinuesToEndOfRecording = true;

                        bool newSegmentIsLargeEnough = (numConsecutiveFramesWithSound >= numFramesThreshold);

                        // Extract the segment and save it.
                        if ((newSegmentIsLargeEnough && hasSoundEnded) || newSoundContinuesToEndOfRecording)
                        {
                            TrimmedSegment trimmedSegment = new TrimmedSegment();

                            // Find the exact start index.
                            trimmedSegment.startIndex = GetStartIndex(newTempStartIndex);

                            // Assign a temporal index for the ending of the segment based on different conditions.
                            if (newSoundContinuesToEndOfRecording)
                                newTempEndIndexSound = segment.Length - 1;
                            else
                                newTempEndIndexSound = newTempStartIndex + (newNumConsecutiveFramesWithSound * newFrameLength);

                            // Find the exact end index.
                            trimmedSegment.endIndex = GetEndIndex(newTempEndIndexSound, trimmedSegment.startIndex);

                            // Extract the individual sound segment.
                            int newSegmentLength = System.Math.Abs(trimmedSegment.endIndex - trimmedSegment.startIndex);
                            Debug.Log("STARTIDX = " + trimmedSegment.startIndex + " , ENDIDX = " + trimmedSegment.endIndex + " , segmentLength = " + newSegmentLength);
                            trimmedSegment.segment = new float[newSegmentLength];
                            System.Array.Copy(segment, trimmedSegment.startIndex, trimmedSegment.segment, 0, newSegmentLength);

                            // Fade in/out segment.
                            trimmedSegment.segment = FadeRecording(trimmedSegment.segment, segmentFadeLength/2);

                            // Save the segment to the list.
                            allTrimmedSegments.Add(trimmedSegment);

                            // Keep track of the number of saved sound segments.
                            numSavedTrimmedSounds = allTrimmedSegments.Count;

                            newNumConsecutiveFramesWithSound = 0; // Reset to default.
                        }
                    }
                        // Skapa kortare frames

                        // Lägre threshold baserat på hela segmentets RMS value

                        // Splitta enligt nya thresholden

                        // Lägg in i Tempquan eleller nåt
                    }*/


                // Tror det är dags att börja implementera koden in i quantization scriptet.

                // Save all the original start and end indices

                // Snap it to a limit by:

                // Generate snaplimits (can be done elsewhere)

                // Find which limit the start indice is closest to

                // Snap/copy the segment over starting from that limit index
                // by using Array.Copy()
                System.Array.Copy(segment, 0, tempNotQuanLoop, startIndexSound, segment.Length);

                numConsecutiveFramesWithSound = 0; // Reset to default.
            }
        }

        // Debug (Remove)
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = AudioClip.Create("segment", tempNotQuanLoop.Length, audioSource.clip.channels, 44100, false);
        audioSource.clip.SetData(tempNotQuanLoop, 0);
        audioSource.loop = true;
        audioSource.Play();

        return loopToExtractSoundsFrom;
    }

    private float GetRMS(float[] frame)
    {
        float sum = 0;

        for (int i = 0; i < frame.Length; i++)
            sum += frame[i] * frame[i]; // sum squared samples

        rmsValue = Mathf.Sqrt(sum / frame.Length); // rms = square root of average
        //DbValue = 20 * Mathf.Log10(RmsValue / RefValue); // calculate dB

        return rmsValue;
    }

    //public float[] Quantize(float[] loopToQuantize)
    //{
    //    rmsValue = GetRMS(); // Get the RMS of the recording.
    //    Debug.Log("rms = " + rmsValue);

    //    Init(loopToQuantize);
    //    Debug.Log("After Init: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

    //    // Perform quantization if the recording was not silent, else send an empty recording back.
    //    float silentThreshold = 0.01f;
    //    if (rmsValue > silentThreshold)
    //    {
    //        DoSampleSplicing();
    //        Debug.Log("After Splicing: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

    //        if(numSavedTrimmedSounds > 0)
    //        {
    //            // Quantization.
    //            newQuantizedLoop = new float[recording.Length];
    //            snapLimits = new int[numSnapLimits]; // Saves the indices where a sound should snap to.
    //            int snapInterval = recording.Length / numSnapLimits;

    //            // Get the indices of the snap limits.
    //            for (int i = 1; i < numSnapLimits; i++)
    //                snapLimits[i] = snapLimits[i - 1] + snapInterval;

    //            for (int i = 0; i < originalStartIndices.Length; i++)
    //            {
    //                for (int j = 1; j < snapLimits.Length; j++)
    //                {
    //                    // Check if the start index of the sound is within two limits.
    //                    if (originalStartIndices[i] > snapLimits[j - 1] && originalStartIndices[i] < snapLimits[j])
    //                    {
    //                        // Compare the two distances to get the closest limit.
    //                        int leftDistanceToLimit = System.Math.Abs(snapLimits[j - 1] - originalStartIndices[i]);
    //                        int rightDistanceToLimit = System.Math.Abs(snapLimits[j] - originalStartIndices[i]);

    //                        if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
    //                        {
    //                            if (leftDistanceToLimit < rightDistanceToLimit) // If closer to the snap limit to the left, else closer to the right.
    //                                SnapToLimit(j - 1);
    //                            else
    //                                SnapToLimit(j);
    //                        }
    //                        else
    //                        {
    //                            break;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            newQuantizedLoop = new float[recording.Length]; // Return empty if no sounds were trimmed.
    //        }

    //    }
    //    else
    //    {
    //        // Return an empty recording if the recording was silent.
    //        newQuantizedLoop = new float[recording.Length];
    //        Debug.Log("Silent recording, returning an empty recording.");
    //    }

    //    Debug.Log("After Quantization: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

    //    return newQuantizedLoop;
    //}

    private void Init(float[] loopToQuantize)
    {
        // KOD FÖR IMPLEMENTATION AV QUANTIZATION SOM SCRIPTABLE OBJECT, ÄNDRA CLIP CHANNELS TILL NÅT MED ATT LÄGGA NUM CHANNELS I RECORDED LOOPS
        //rangeToInvestigateInSamples = 44100 * 1 * (int)recordedLoops.numChannels / 1000; // Ges i samples. tid = 1ms.
        //int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels; // In samples/indices.
        rangeToInvestigateInSamples = 44100 * 1 * (int)audioSource.clip.channels / 1000; // Ges i samples. tid = 1ms.
        int numSamplesInRecording = (int)audioSource.clip.samples * (int)audioSource.clip.channels;
        recording = new float[numSamplesInRecording];
        recording = loopToQuantize;
        

        // Regulate the threshold with the RMS value of the signal.
        beforeSoundThreshold = rmsValue / 10; // för start sound verkar /10 vara bra
        afterSoundThreshold = rmsValue / 100000; // för snare sound verkar /10 000 vara ett bra endvärde. 100 000
        //Debug.Log("RMS = " + rmsValue);
        //Debug.Log("BeforeSoundThresh = " + beforeSoundThreshold);
        //Debug.Log("AfterSoundThresh = " + afterSoundThreshold);

        startIndex = 0;
        endIndex = 0;

        numSavedTrimmedSounds = 0;

        allTrimmedSounds = new float[numSoundsAllowedInLoop][];
        originalStartIndices = new int[numSnapLimits];

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
                if ( (durationOfSegmentInSamples > (minLengthInSamples * recordedLoops.numChannels)) && (numSavedTrimmedSounds < originalStartIndices.Length))
                {
                    Debug.Log("durationOfSegmentInSamples = " + durationOfSegmentInSamples);

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
        Debug.Log("INSIDE SAVETREIMMEDAUDIOSEGMENT");

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
        if (numSnappedSounds < numSoundsAllowedInLoop)
        {
            int lengthOfSound = allTrimmedSounds[numSnappedSounds].Length;

            // Snap the sound to the limit.
            for (int i = snapLimits[limitIndex]; i < lengthOfSound + snapLimits[limitIndex]; i++)
            {
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

    public float GetRMS()
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
