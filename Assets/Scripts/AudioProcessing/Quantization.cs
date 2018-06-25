using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class takes a recording as input, extracts the relevant segments from the recording,
/// pitchshifts the segments, quantizes the segments while constructing a new loop out of the segments
/// and lastly applies a gate to the depending of the percentage of sound within in the new loop.
/// </summary>

namespace Musikverkstaden
{
    [CreateAssetMenu]
    public class Quantization : ScriptableObject
    {
        [SerializeField]
        private RecordedLoops recordedLoops;
        private float[] recording;

        // General variables.
        private int numSoundsAllowedInLoop = 20;
        private int numSavedTrimmedSounds;
        private int rangeToInvestigateInSamples;
        private float beforeSoundThreshold = 0.1f;
        private float afterSoundThreshold = 0.00006f;
        private float rmsValue;
        private int numSnapLimits;
        private float[] newQuantizedLoop;
        private int numSnappedSounds;
        private int[] snapLimits8Beats;
        private int[] snapLimits16Beats;
        private int numSamplesWithSound;
        private float[] fadeInSegment;
        private float[] fadeOutSegment;
        private float silenceThreshold;

        // Every extracted segment will be a "TrimmedSegment".
        private struct TrimmedSegment
        {
            public float[] segment;
            public int startIndex;
            public int endIndex;
        }

        // The list with all of the trimmed segments.
        List<TrimmedSegment> allTrimmedSegments;

        // The function that creates a new loop by extracting relevant segments, 
        // pitchshifting them, quantizing them and maybe apply a gate to the newly constructed loop.
        public float[] Quantize(float[] loopToQuantize)
        {
            // Get the RMS of the recording.
            rmsValue = GetRMS(loopToQuantize);

            // Initialize variables.
            Init(loopToQuantize);

            int fs = recordedLoops.sampleRate / 2;
            double frameDuration = 0.08; // In seconds.
            double frameLengthDouble = System.Math.Floor(frameDuration * fs);
            int frameLength = (int)frameLengthDouble;
            int numSamples = recording.Length;
            double numFrames = System.Math.Floor((double)numSamples / frameLength);

            int numConsecutiveFramesWithSound = 0;
            int numFramesThreshold = 10; // Default value is 10.
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
                float[] frame = new float[frameLength];
                System.Array.Copy(recording, startFrameIndex, frame, 0, frameLength);

                // Get the RMS-value for the frame.
                float frameRMSValue = GetRMS(frame);

                // Check if there is sound in the frame.
                if (frameRMSValue > silenceThreshold)
                {
                    // Keep track of the index of the beginning of the sound.  
                    if (numConsecutiveFramesWithSound == 0)
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

                // Control if the segment is large enough to save.
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
                    //Debug.Log("STARTIDX = " + trimmedSegment.startIndex + " , ENDIDX = " + trimmedSegment.endIndex + " , segmentLength = " + segmentLength);
                    trimmedSegment.segment = new float[segmentLength];
                    System.Array.Copy(recording, trimmedSegment.startIndex, trimmedSegment.segment, 0, segmentLength);

                    // Fade in/out segment to remove clicks and pops.
                    trimmedSegment.segment = FadeRecordingZeroCrossings(trimmedSegment.segment);

                    // Pitchshift the segment.
                    Pitchshift pitchshift = new Pitchshift();
                    trimmedSegment.segment = pitchshift.PitchshiftSegment(trimmedSegment.segment);
                    Debug.Log("----------------------------- About to add to allTrimmedSegments -----------------------------------");

                    // Add the segment to the list.
                    allTrimmedSegments.Add(trimmedSegment);

                    // Keep track of the number of saved sound segments.
                    numSavedTrimmedSounds = allTrimmedSegments.Count;

                    // Reset to default.
                    numConsecutiveFramesWithSound = 0;
                }
            }


            /* ***************** QUANTIZATION ******************* */

            //Debug.Log("After Splicing: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

            if (numSavedTrimmedSounds > 0) // If there are any sounds to be quantized.
            {
                recordedLoops.silentRecording = false; ;
                newQuantizedLoop = new float[recording.Length];

                // Saves the indices where a sound should snap to.
                snapLimits8Beats = new int[numSnapLimits];
                snapLimits16Beats = new int[16];
                int snapInterval8 = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels / numSnapLimits;
                int snapInterval16 = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels / 16;

                // Generate the indices of the snap limits.
                snapLimits8Beats = GenerateSnapLimits(snapInterval8, snapLimits8Beats);
                snapLimits16Beats = GenerateSnapLimits(snapInterval16, snapLimits16Beats);

                bool snapTo8beats;
                // One beat is 12 000 samples. Snapping to 8 beats for a 2 bar long loop sounds the best. For snapping to 1/16 then use 1/2 beat instead.
                int thresholdDistance = 12000;
                int distance;

                // Iterates through each saved segment.
                for (int soundIndex = 0; soundIndex < allTrimmedSegments.Count; soundIndex++)
                {
                    // Calculate the distance between two sounds next to each other, measured in samples.
                    if (soundIndex == 0)
                    {
                        // Distance from zero to the first sound.
                        distance = allTrimmedSegments[soundIndex].startIndex;
                    }
                    else
                    {
                        int prevEndIndex = allTrimmedSegments[soundIndex - 1].startIndex;
                        int currentStartIndex = allTrimmedSegments[soundIndex].startIndex;

                        // Distance between two sounds next to eachother measured in indices/samples.
                        distance = System.Math.Abs(prevEndIndex - currentStartIndex);
                    }

                    //Debug.Log("distance = " + distance); // Should be larger than 6000.

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
                    // Should be 8 beats, but for the current song 16 beats is better.
                    if (snapTo8beats)
                        FindLimitToSnapThenSnap(soundIndex, snapLimits16Beats);
                    else
                        FindLimitToSnapThenSnap(soundIndex, snapLimits16Beats);
                }
            }
            else // If no segments where saved.
            {
                // Update the state.
                ApplicationProperties.State = State.SilentInQuantization;

                // Return an empty loop filled with zeroes.
                Debug.Log("Returning empty loop from quantization script.");
                recordedLoops.silentRecording = true;
                float[] emptyResetLoop = new float[(int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels];
                for (int i = 0; i < emptyResetLoop.Length; i++)
                    emptyResetLoop[i] = 0.0f;

                return emptyResetLoop;
            }

            // Check if a gate should be applied to the loop.
            double percentageSoundInLoop = 0;
            if (newQuantizedLoop.Length != 0)
            {
                // Calculate the percentage of sound in the quantized loop.
                percentageSoundInLoop = (double)numSamplesWithSound / newQuantizedLoop.Length;
                Debug.Log("PERCENTAGE SOUND IN LOOP = " + percentageSoundInLoop);

                // Send the loop to be gated if there's enough sound in the loop.
                double percentageThreshold = 0.45;
                if (percentageSoundInLoop > percentageThreshold)
                {
                    Debug.Log("Exceeded percentage threshold, sending loop to be gated.");
                    ApplyGatingToLoop();
                }
            }

            Debug.Log("After Quantization: numSavedTrimmedSounds = " + numSavedTrimmedSounds);

            return newQuantizedLoop;
        }

        // Calculate the number of samples with audio.
        private void CountSamplesWithAudio()
        {
            foreach (var item in allTrimmedSegments)
            {
                numSamplesWithSound += System.Math.Abs(item.endIndex - item.startIndex);
            }
        }

        // Initialise the variables needed in this class.
        private void Init(float[] loopToQuantize)
        {
            rangeToInvestigateInSamples = recordedLoops.sampleRate * 2000 * (int)recordedLoops.numChannels / 1000; // Ges i samples. tid = 1ms.
            int numSamplesInRecording = (int)recordedLoops.numSamplesInRecording * (int)recordedLoops.numChannels; // In samples/indices.

            recording = new float[numSamplesInRecording];
            recording = loopToQuantize;

            allTrimmedSegments = new List<TrimmedSegment>();

            // Regulate the threshold with the RMS value of the signal.
            // The denominators "3", "10" and "100 000" were found by testing.
            silenceThreshold = GetRMS(recording) / 3;
            beforeSoundThreshold = rmsValue / 10;
            afterSoundThreshold = rmsValue / 100000;

            numSnapLimits = 8;
            numSavedTrimmedSounds = 0;
            numSnappedSounds = 0;

            // This variable is used for calculating percentage of sound in the final loop.
            numSamplesWithSound = 0;
        }

        // Finds which limit to quantize/snap to, then sends the segment to be snapped.
        private void FindLimitToSnapThenSnap(int soundIndex, int[] snapLimits)
        {
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

                        // If closer to the snap limit to the left, else closer to the right.
                        if (leftDistanceToLimit < rightDistanceToLimit)
                            SnapToLimit(j - 1, snapLimits, soundIndex); // Snap to the left limit.
                        else
                            SnapToLimit(j, snapLimits, soundIndex); // Snap to the right limit.
                    }
                    else
                    {
                        recordedLoops.silentRecording = false;

                        // Break, exceeds number of allowed sounds in the loop.
                        break;
                    }
                }
            }
        }

        // Generate the limits to quantize/snap to.
        private int[] GenerateSnapLimits(int snapInterval, int[] snapLimits)
        {
            for (int i = 1; i < snapLimits.Length; i++)
                snapLimits[i] = snapLimits[i - 1] + snapInterval - 1;

            return snapLimits;
        }

        // Check if it was quiet for the transient.
        private bool IsItQuietBeforeTheTransient(int idx)
        {
            return (System.Math.Abs(recording[idx]) < beforeSoundThreshold) ? true : false;
        }

        // Check if sound exists after the transient.
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

        // Find the start index.
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

        // Finds the index for the transient.
        private int GetTransientIndex(int startIndex)
        {
            // Search through the first beat to find the index with the max value within it.
            int oneBeatInSamples = 9600; // One beat is 9600 samples.
            float maxValue = 0;
            int transientIndex = 0;
            for (int k = 0; k < oneBeatInSamples / 6 && (startIndex + k) < recording.Length; k++)
            {
                if (recording[startIndex + k] > maxValue)
                {
                    maxValue = recording[startIndex + k];

                    // Save the index with the max value.
                    transientIndex = startIndex + k;
                }
            }
            return transientIndex;
        }

        // Find the index for the ending.
        private int GetEndIndex(int tempEndIndexSound, int startIndexSound)
        {
            int endIndexSound = tempEndIndexSound;

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

        // Fade the recording by finding the zero crossings before and after a segment.
        private float[] FadeRecordingZeroCrossings(float[] segment)
        {
            // Start from the beginning and find the first zero crossing.
            int indexForFirstZeroCrossing = 0;
            for (int i = 1; i < segment.Length; i++)
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
            //Debug.Log("indexForFirstZeroCrossing = " + indexForFirstZeroCrossing);

            // Do the same but start from the end.
            int indexForLastZeroCrossing = 0;
            for (int j = segment.Length - 1; j >= 0; j--)
            {
                bool zeroCrossingOccurred1 = (segment[j - 1] > 0 && segment[j] < 0);
                bool zeroCrossingOccurred2 = (segment[j - 1] < 0 && segment[j] > 0);

                if (zeroCrossingOccurred1 || zeroCrossingOccurred2)
                {
                    indexForLastZeroCrossing = j - 1;
                    break;
                }
            }
            //Debug.Log("indexForLastZeroCrossing = " + indexForLastZeroCrossing);

            // Trim the clip.
            int trimmedLength = segment.Length - indexForFirstZeroCrossing - (segment.Length - indexForLastZeroCrossing);
            float[] trimmedSegment = new float[trimmedLength];
            System.Array.Copy(segment, indexForFirstZeroCrossing, trimmedSegment, 0, trimmedLength);

            //Debug.Log("trimmedLength = " + trimmedLength);

            // Fade in and out with a linear line.
            trimmedSegment = FadeRecording(trimmedSegment, trimmedSegment.Length / 8);

            return trimmedSegment;
        }

        // Fade the beginning and the end of a segment.
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

        // Apply a gate to the newly constructed loop.
        // There are 3 different gates. 
        // Every consecutive loop will be gated with a different gate.
        private void ApplyGatingToLoop()
        {
            int factor = 1;

            // Gate differently each time.
            switch (ApplicationProperties.numGatedLoops)
            {
                case 0:
                    factor = 1;
                    //Debug.Log("numGatedLoops = " + ApplicationProperties.numGatedLoops);
                    ApplicationProperties.numGatedLoops++;
                    break;
                case 1:
                    factor = 2;
                    //Debug.Log("numGatedLoops = " + ApplicationProperties.numGatedLoops);
                    ApplicationProperties.numGatedLoops++;
                    break;
                case 2:
                    factor = 3;
                    //Debug.Log("numGatedLoops = " + ApplicationProperties.numGatedLoops);

                    // Reset, so it gates with a factor of 1 again.
                    ApplicationProperties.numGatedLoops = 0;
                    break;
                default:
                    break;
            }

            //Debug.Log("Starting to GATE.");

            float[] beatGate = new float[newQuantizedLoop.Length];

            // Calculate the interval for the gate using the factor.
            // Default value here is 12000, which corresponds to beat/4. 48000 samples is one beat.
            int gateInterval = (factor == 1) ? factor * 12000 / 2 : factor * 12000;

            int divideBy = 8;
            int fadeDistance = gateInterval / divideBy;
            float[] fadeIn = new float[fadeDistance];
            float[] fadeOut = new float[fadeDistance];

            // Create the fade in array.
            // The max value is the same as the fade distance.
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

            //Debug.Log("Generated the gate-array");

            // Gate the loop.
            int length = (newQuantizedLoop.Length > beatGate.Length) ? beatGate.Length : newQuantizedLoop.Length;
            for (int j = 0; j < length; j++)
                newQuantizedLoop[j] = newQuantizedLoop[j] * beatGate[j];

            //Debug.Log("GATING DONE");
        }

        // Snaps one sound at the time, and "numSnappedSounds" is the current index of the sound which will be snapped.
        private void SnapToLimit(int limitIndex, int[] snapLimits, int soundToSnapIndex)
        {
            //Debug.Log("snapped limit for sound " + soundToSnapIndex + " , snapLimits[limitIndex] = " + snapLimits[limitIndex]);

            if (numSnappedSounds < numSoundsAllowedInLoop && numSnappedSounds <= numSavedTrimmedSounds)
            {
                // Write the current segment to the newly constructed loop at the quantized position.
                float[] sourceArray = allTrimmedSegments[soundToSnapIndex].segment;

                // Control if the segment will exceed the array length.
                int tempEndIndex = snapLimits[limitIndex] + sourceArray.Length;
                bool exceedsArrayLength = (tempEndIndex > newQuantizedLoop.Length);

                // Either adjust the length of the segment, or use the original length.
                int lengthToCopy = (exceedsArrayLength) ? tempEndIndex - newQuantizedLoop.Length : sourceArray.Length;

                /*
                // Move the transient to the snap limit. This is not in the final version because it needs some tweaking.
                // But it might be worth to further look into this later.
                // This code didn't work everytime, it snapped all the sounds to zero because of the "indexToSnapTo" variable.

                //int startIndexOfSoundToBeSnapped = allTrimmedSegments[soundToSnapIndex].startIndex;
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

        // Calculate the RMS value of the recording.
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
}