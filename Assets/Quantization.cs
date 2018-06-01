using System.Collections.Generic;
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
    private int segmentFadeLength = 2400;//1500; // In samples.
    private int loopFadeLength = 9600;

    float silenceThreshold;
    private struct TrimmedSegment
    {
        public float[] segment;
        public int startIndex;
        public int endIndex;
    }
    List<TrimmedSegment> allTrimmedSegments;

    public float[] Quantize(float[] loopToQuantize)
    {
        rmsValue = GetRMS(loopToQuantize); // Get the RMS of the recording.

        Init(loopToQuantize); // Initialize variables.

        int fs = recordedLoops.sampleRate / 2;
        double frameDuration = 0.08; // 0.08;//0.03; // In seconds.
        double frameLengthDouble = System.Math.Floor(frameDuration * fs);
        int frameLength = (int)frameLengthDouble;
        int numSamples = recording.Length;
        double numFrames = System.Math.Floor((double)numSamples / frameLength);

        int numConsecutiveFramesWithSound = 0;
        int numFramesThreshold = 10; //10; // Default value is 10.
        bool hasSoundEnded = false;

        int tempEndIndexSound;
        bool soundContinuesToEndOfRecording = false;
        bool segmentIsLargeEnough;

        /* ***************** SAMPLE SPLICING ******************* */
        int tempStartIndex = 0;
        // Iterate through each frame.
        for (int frameIterator = 1; frameIterator < numFrames; frameIterator++)
        {
            // Create frames.
            int startFrameIndex = (frameIterator - 1) * frameLength + 1;
            //Debug.Log("startFrameIndex = " + startFrameIndex);
            int endFrameIndex = frameLength * frameIterator;
            float[] frame = new float[frameLength];
            System.Array.Copy(recording, startFrameIndex, frame, 0, frameLength);

            // Get the RMS-value for the frame.
            float frameRMSValue = GetRMS(frame);

            // Check if there is sound in the frame.
            if (frameRMSValue > silenceThreshold)
            {
                if (numConsecutiveFramesWithSound == 0) // Keep track of the index of the beginning of the sound.              
                    tempStartIndex = startFrameIndex;

                numConsecutiveFramesWithSound++;
                hasSoundEnded = false;
                //Debug.Log("numConsecutiveFramesWithSound = " + numConsecutiveFramesWithSound);
            }
            else if (numConsecutiveFramesWithSound > 0) // If a sound has ended.
                hasSoundEnded = true;

            // Handle a special case when the sound continues to the end of the recording.
            if (frameIterator == numFrames - 1 && hasSoundEnded == false)
                soundContinuesToEndOfRecording = true;            

            segmentIsLargeEnough = (numConsecutiveFramesWithSound >= numFramesThreshold);

            // Extract the segment and save it.
            if ((segmentIsLargeEnough && hasSoundEnded) || soundContinuesToEndOfRecording)
            {
                TrimmedSegment trimmedSegment = new TrimmedSegment();

                // Find the exact start index.
                trimmedSegment.startIndex = GetStartIndex(tempStartIndex); 

                // Assign a temporal index for the ending of the segment based on different conditions.
                if (soundContinuesToEndOfRecording)
                    tempEndIndexSound = recording.Length - 1;
                else
                    tempEndIndexSound = tempStartIndex + (numConsecutiveFramesWithSound * frameLength);

                // Find the exact end index.
                trimmedSegment.endIndex = GetEndIndex(tempEndIndexSound, trimmedSegment.startIndex);                

                // Extract the individual sound segment.
                int segmentLength = System.Math.Abs(trimmedSegment.endIndex - trimmedSegment.startIndex);
                Debug.Log("STARTIDX = " + trimmedSegment.startIndex + " , ENDIDX = " + trimmedSegment.endIndex + " , segmentLength = " + segmentLength);
                trimmedSegment.segment = new float[segmentLength];
                System.Array.Copy(recording, trimmedSegment.startIndex, trimmedSegment.segment, 0, segmentLength);

                // Fade in/out segment.
                //trimmedSegment.segment = FadeRecording(trimmedSegment.segment, trimmedSegment.segment.Length/6);
                trimmedSegment.segment = FadeRecordingZeroCrossings(trimmedSegment.segment);

                Pitchshift pitchshift = new Pitchshift();
                trimmedSegment.segment = pitchshift.PitchshiftSegment(trimmedSegment.segment);
                Debug.Log("----------------------------- About to add to allTrimmedSegments -----------------------------------");

                allTrimmedSegments.Add(trimmedSegment);

                // Keep track of the number of saved sound segments.
                numSavedTrimmedSounds = allTrimmedSegments.Count;

                numConsecutiveFramesWithSound = 0; // Reset to default.
            }
        }

        





        /* ***************** QUANTIZATION ******************* */

        Debug.Log("After Splicing: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

        if (numSavedTrimmedSounds > 0) // If there are any sounds to be quantized.
        {
            recordedLoops.silentRecording = false; ;

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
            int thresholdDistance = 12000; //12000 / 2; // 1 beat är 12000, vi brukar snappa till 8 beats för bra ljud, men 1/2 beat om vi ska snappa ord till 16:delar.
            int distance;

            // Iterates through each saved segment.
            for (int soundIndex = 0; soundIndex < allTrimmedSegments.Count; soundIndex++) 
            {   
                // Calculate the distance between two sounds next to each other, measured in samples.
                if (soundIndex == 0)
                    distance = allTrimmedSegments[soundIndex].startIndex;  // Distance from zero to the first sound.
                else
                {
                    int prevEndIndex = allTrimmedSegments[soundIndex - 1].startIndex;
                    int currentStartIndex = allTrimmedSegments[soundIndex].startIndex;
                    distance = System.Math.Abs(prevEndIndex - currentStartIndex); // Distance between two sounds next to eachother measured in indices/samples.
                }

                //Debug.Log("distance = " + distance); // should be bigger than 6000

                // Check if the sound should be snapped to 8 beats or 8 beats / 2.
                if (distance > thresholdDistance)
                {
                    snapTo8beats = true;
                    //Debug.Log("snapTo8Beats = " + true);
                }
                else
                {
                    snapTo8beats = false;
                    Debug.Log("snapTo8Beats = " + false);
                }

                // Snap to 8 beats or 16 beats.
                if (snapTo8beats)
                    FindLimitToSnapThenSnap(soundIndex, snapLimits16Beats); // Ska vara 8 beats som ska skickas egentligen, men till clownlåten passar 16 beats bättre för det är dubbeltakt i den.
                else
                    FindLimitToSnapThenSnap(soundIndex, snapLimits16Beats);             
            }
        }
        else // If no segments where saved.
        {
            ApplicationProperties.State = State.SilentInQuantization;

            Debug.Log("Returning empty loop from quantization script.");
            recordedLoops.silentRecording = true;
            float[] emptyResetLoop = new float[(int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels];
            for (int i = 0; i < emptyResetLoop.Length; i++)
                emptyResetLoop[i] = 0.0f;

            return emptyResetLoop;
        }

        double percentageSoundInLoop = 0;
        if (newQuantizedLoop.Length != 0)
        {
            // Calculate the percentage of sound in the quantized loop. Borde egentligen bara ljud som snappats.
            //Debug.Log("NUMSAMPLESWITHSOUND = " + numSamplesWithSound);
            //Debug.Log("NEW LOOP LENGTH = " + newQuantizedLoop.Length);

            percentageSoundInLoop = (double)numSamplesWithSound / newQuantizedLoop.Length; // 2 är numChannels, vet inte varför jag måste ha det här, men numSamplesWithSound blir annars alltid 355200 typ
            Debug.Log("PERCENTAGE SOUND IN LOOP = " + percentageSoundInLoop);

            double percentageThreshold = 0.45;//0.55; // DEBUG
            if (percentageSoundInLoop > percentageThreshold)
            {
                Debug.Log("Exceeded percentage threshold, sending loop to be gated.");
                ApplyGatingToLoop();
            }

            // Fade-in the newly quantized loop to remove the thump sound at the beginning of a recording on a mobile phone.
            //newQuantizedLoop = FadeRecording(newQuantizedLoop, loopFadeLength);
        }

        Debug.Log("After Quantization: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

        return newQuantizedLoop;
    }

    private void CountSamplesWithAudio()
    {
        foreach(var item in allTrimmedSegments)
        {
            numSamplesWithSound += System.Math.Abs(item.endIndex - item.startIndex);
        }
    }

    private void Init(float[] loopToQuantize)
    {
        rangeToInvestigateInSamples = recordedLoops.sampleRate * 2000 * (int)recordedLoops.numChannels / 1000; // Ges i samples. tid = 1ms.
        int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels; // In samples/indices.

        recording = new float[numSamplesInRecording];
        recording = loopToQuantize;

        // Regulate the threshold with the RMS value of the signal.
        silenceThreshold = GetRMS(recording) / 3; /// 2;//3; 3 var default och funkade bra
        beforeSoundThreshold = rmsValue / 10; // för start sound verkar /10 vara bra
        afterSoundThreshold = rmsValue / 100000; // för snare sound verkar /10 000 vara ett bra endvärde. 100 000

        // Temporary indices for a spliced segment.
        startIndex = 0;
        endIndex = 0;

        numSnapLimits = 8;
        numSavedTrimmedSounds = 0;
        numSnappedSounds = 0;
        numSamplesWithSound = 0; // Used for calculating percentage of sound in the final loop.

        allTrimmedSounds = new float[numSoundsAllowedInLoop][];
        originalStartIndices = new int[numSoundsAllowedInLoop];
        originalEndIndices = new int[numSoundsAllowedInLoop];

        allTrimmedSegments = new List<TrimmedSegment>();

        Debug.Log("INIT DONE");
    }

    private void FindLimitToSnapThenSnap(int soundIndex, int[] snapLimits)
    {
        //Debug.Log("Inside FINDLIMITTOSNAPTHENSNAP");
        // Iterates through the snap limits.
        for (int j = 1; j < snapLimits.Length; j++)
        {
            int currentSegmentIndex = allTrimmedSegments[soundIndex].startIndex;

            // Check if the start index of the sound is within two limits to the left and the right.
            if (currentSegmentIndex > snapLimits[j - 1] && currentSegmentIndex < snapLimits[j])
            {
                // Compare the two distances to get the closest limit.
                int leftDistanceToLimit = System.Math.Abs(snapLimits[j - 1] - currentSegmentIndex);
                int rightDistanceToLimit = System.Math.Abs(snapLimits[j] - currentSegmentIndex);

                if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
                {
                    //Debug.Log("Inside FindlimitToSnapThenSnap, numsavedtrimmedsounds < numsoundsallowedloop");
                    if (leftDistanceToLimit < rightDistanceToLimit) // If closer to the snap limit to the left, else closer to the right.
                        SnapToLimit(j - 1, snapLimits, soundIndex); // Snap to the left limit.
                    else
                        SnapToLimit(j, snapLimits, soundIndex); // Snap to the right limit.
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
        // Search through the first beat to find the index with the max value within it.
        int oneBeatInSamples = 9600; // One beat is 9600 samples.
        float maxValue = 0;
        int transientIndex = 0;
        for (int k = 0; k < oneBeatInSamples/6 && (startIndex + k) < recording.Length; k++) 
        {
            if (recording[startIndex + k] > maxValue)
            {
                maxValue = recording[startIndex + k];                
                transientIndex = startIndex + k; // Save the index with the max value.
            }
        }
        return transientIndex;
    }

    private int GetEndIndex(int tempEndIndexSound, int startIndexSound)
    {
        int endIndexSound =  tempEndIndexSound;

        for (int i = tempEndIndexSound; i > startIndexSound; i--)
        {
            if (recording[i] > afterSoundThreshold)
            {
                endIndexSound = i;
                break;
            }
        }
        return endIndexSound;
    }

    private float[] FadeRecordingZeroCrossings(float[] segment)
    {
        // Start from the beginning and find the first zero crossing.
        int indexForFirstZeroCrossing = 0;
        for(int i = 1; i < segment.Length; i++)
        {
            // Find the index where there's a zero-crossing.
            bool zeroCrossingOccurred1 = (segment[i - 1] > 0 && segment[i] < 0);
            bool zeroCrossingOccurred2 = (segment[i - 1] < 0 && segment[i] > 0);

            if (zeroCrossingOccurred1 || zeroCrossingOccurred2)
            {
                indexForFirstZeroCrossing = i;
                break;
            }
        }
        Debug.Log("indexForFirstZeroCrossing = " + indexForFirstZeroCrossing);

        // Do the same but start from the end.
        int indexForLastZeroCrossing = 0;
        for(int j = segment.Length - 1; j >= 0; j--)
        {
            bool zeroCrossingOccurred1 = (segment[j - 1] > 0 && segment[j] < 0);
            bool zeroCrossingOccurred2 = (segment[j - 1] < 0 && segment[j] > 0);

            if (zeroCrossingOccurred1 || zeroCrossingOccurred2)
            {
                indexForLastZeroCrossing = j - 1;
                break;
            }
        }
        Debug.Log("indexForLastZeroCrossing = " + indexForLastZeroCrossing);

        // Trim the clip.
        int trimmedLength = segment.Length - indexForFirstZeroCrossing - (segment.Length - indexForLastZeroCrossing);
        float[] trimmedSegment = new float[trimmedLength];
        System.Array.Copy(segment, indexForFirstZeroCrossing, trimmedSegment, 0, trimmedLength);

        Debug.Log("trimmedLength = " + trimmedLength);

        // Fade in and out with a linear line.
        trimmedSegment = FadeRecording(trimmedSegment, trimmedSegment.Length / 8);

        return trimmedSegment;
    }

    private float[] FadeRecording(float[] segment, int lengthOfFade)
    {
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
        int factor = 1;
        // Gate differently each time.
        switch (ApplicationProperties.numGatedLoops)
        {
            case 0:
                factor = 1;
                Debug.Log("||||||||||||||||||||||||||||<<numGatedLoops = " + ApplicationProperties.numGatedLoops);
                ApplicationProperties.numGatedLoops++;
                break;
            case 1:
                factor = 2;
                Debug.Log("||||||||||||||||||||||||||||<<numGatedLoops = " + ApplicationProperties.numGatedLoops);
                ApplicationProperties.numGatedLoops++;
                break;
            case 2:
                factor = 3;
                Debug.Log("||||||||||||||||||||||||||||<<numGatedLoops = " + ApplicationProperties.numGatedLoops);
                ApplicationProperties.numGatedLoops = 0; // Reset, so it gates with a factor of 1 again.
                break;
            default:
                break;
        }        

        Debug.Log("Starting to GATE.");
        float[] beatGate = new float[newQuantizedLoop.Length];

        //int gateInterval = 1*12000; // Default value here is 12000, which corresponds to beat/4. 48000 samples is one beat.
        int gateInterval = (factor == 1) ? factor * 12000 / 2: factor * 12000; // Default value here is 12000, which corresponds to beat/4. 48000 samples is one beat.
        int divideBy = 8;
        int fadeDistance = gateInterval / divideBy;
        float[] fadeIn = new float[fadeDistance];
        float[] fadeOut = new float[fadeDistance];

        // Create the fade in array.
        int x = fadeDistance; // The max value is the same as the fade distance.
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
                if (k < gateInterval / divideBy)
                {
                    beatGate[i] = fadeIn[k];
                }
                else if (k > ((divideBy - 1) * gateInterval / divideBy)) // Fade out the gate.
                {
                    int adjustedIndexToPreventOutofBounds = (divideBy - 1) * gateInterval / divideBy;
                    beatGate[i] = fadeOut[k - adjustedIndexToPreventOutofBounds];
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
    private void SnapToLimit(int limitIndex, int[] snapLimits, int soundToSnapIndex)
    {
        Debug.Log("snapped limit for sound " + soundToSnapIndex + " , snapLimits[limitIndex] = " + snapLimits[limitIndex]);

        if (numSnappedSounds < numSoundsAllowedInLoop && numSnappedSounds <= numSavedTrimmedSounds)
        {
            // Write the current segment to the newly constructed loop at the quantized position.
            float[] sourceArray = allTrimmedSegments[soundToSnapIndex].segment;
            int sourceStartIndex = allTrimmedSegments[soundToSnapIndex].startIndex;
            
            // Control if the segment will exceed the array length.
            int tempEndIndex = snapLimits[limitIndex] + sourceArray.Length;
            bool exceedsArrayLength = (tempEndIndex > newQuantizedLoop.Length);

            // Either adjust the length of the segment, or use the original length.
            int lengthToCopy = (exceedsArrayLength) ? tempEndIndex - newQuantizedLoop.Length: sourceArray.Length;

            // Move the transient to the snap limit.
            int startIndexOfSoundToBeSnapped = allTrimmedSegments[soundToSnapIndex].startIndex;
            // This code didn't work everytime, it snapped all the sounds to zero because of the "indexToSnapTo" variable.
            /*
            int transientIndex = GetTransientIndex(startIndexOfSoundToBeSnapped); // Get transient Index.
            Debug.Log("transientIndex = " + transientIndex);
            int d = (snapLimits[limitIndex] - transientIndex);
            Debug.Log("(snapLimits[limitIndex] - transientIndex) = " + d);
            int indexToSnapTo = ((snapLimits[limitIndex] - transientIndex) < 0) ? 0 : snapLimits[limitIndex] - transientIndex;
            Debug.Log("Index to snap to = " + indexToSnapTo);
            */
            int indexToSnapTo = snapLimits[limitIndex];

            // Copy the segment to the newly quantized loop at the snap limit.
            System.Array.Copy(sourceArray, 0, newQuantizedLoop, indexToSnapTo, lengthToCopy);

            // Calculate the num of samples with sound.
            numSamplesWithSound += lengthToCopy;        

            numSnappedSounds++;
            //Debug.Log("numSnappedSounds = " + numSnappedSounds);
        }
        else
            Debug.Log("Exceeds number of allowed sounds!");
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