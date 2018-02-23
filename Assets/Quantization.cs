using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Class to split the audio into smaller parts by finding the transients.
public class Quantization : MonoBehaviour {

    private AudioSource audioSource;
    private float[] recording;

    int numSoundsAllowedInLoop = 10;
    float[][] allTrimmedSounds;
    int numSavedTrimmedSounds = 0;
    int rangeToInvestigateInSamples;
    float BeforeSoundThreshold = 0.001f; // Ideala värdet för kickdrum loopen är iaf 0.00001f.
    float AfterSoundThreshold = 0.00006f; // 0.00001f;

    int numSnapLimits = 8; // 16 är default, men kanske borde ha 8? 4 fungerar inte för den kapar det sista ljudet.
    int[] originalStartIndices;


    // Use this for initialization
    void Start () {

        allTrimmedSounds = new float[numSoundsAllowedInLoop][];
        originalStartIndices = new int[numSnapLimits];

        audioSource = GetComponent<AudioSource>();
        rangeToInvestigateInSamples = 44100 * 1 * audioSource.clip.channels / 1000; // Ges i samples. tid = 1ms.

        int numAudioChannels = (audioSource.clip.channels == 2) ? 2 : 1; // Check if it's stereo (two channels), because it affects the time calculation.

        // Fetch the sound clip which has been assigned in the inspector.
        int numSamplesInRecording = audioSource.clip.samples * audioSource.clip.channels; // In samples/indices.
        recording = new float[numSamplesInRecording];
        audioSource.clip.GetData(recording, 0);
        
        //Debug.Log("REC. SIZE = " + recording.Length);
        
        // Indices of the audio segment to extract from the recording.
        int startIndex = 0;
        int endIndex = 0;

        // The loop below detects relevant sounds in the recording, extracts them and saves them in smaller segments.
        // The loop iterates through the recording and checks if there is audio within a chosen range
        // to see if the values goes from i.e. zero to a value above an threshold. If that occurs, then a sound is beginning.
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

                int minLengthInSamples = 3000; // The allowed minimum length of a trimmed segment in the number of samples/indices.

                // Save the trimmed audio segment if it exceeds the allowed minimum length.
                if (durationOfSegmentInSamples > minLengthInSamples * audioSource.clip.channels)
                {
                    endIndex = tempEndIndex; 
                    idx = endIndex; // The while-loop will keep looping from the index of the end of the  most recently detected sound.
                    idx++;

                    originalStartIndices[numSavedTrimmedSounds] = startIndex; // Save start indices for each saved segment for later use during quantization.

                    SaveTrimmedAudioSegment(startIndex, endIndex);

                    continue;
                }
                else // The audio segment is too short.
                {
                    idx++;
                    continue;
                }               
            }
            idx++;
        }


        // Quantization.

        // Calculate snap limits.
        int[] snapLimits = new int[numSnapLimits]; // Saves the indices where a sound should snap to.
        int snapInterval = recording.Length / numSnapLimits;

        // Get the indices of the snap limits.
        for (int i = 1; i < numSnapLimits; i++)
            snapLimits[i] = snapLimits[i - 1] + snapInterval;

        float[] newQuantizedLoop = new float[recording.Length];

        //List<float> newQuantizedLoop = new List<float>();

        int numSnappedSounds = 0;

        // TODO: ändra alla namn med snap till quantize istället

        // Gå igenom varje start index och kolla om mellan två limits
        for (int i = 0; i < originalStartIndices.Length; i++)
        {
            for (int j = 1; j < snapLimits.Length; j++)
            {
                // Check if the start index of the sound is within two limits.
                if (originalStartIndices[i] > snapLimits[j - 1] && originalStartIndices[i] < snapLimits[j])
                {
                    // Compare the two distances to get the closest limit.
                    int leftDistanceToLimit = System.Math.Abs(snapLimits[j - 1] - originalStartIndices[i]);
                    int rightDistanceToLimit = System.Math.Abs(snapLimits[j] - originalStartIndices[i]);

                    // If closer to the snap limit to the left.
                    if (leftDistanceToLimit < rightDistanceToLimit)
                    {                        
                        // Tar ej hänsyn till sista limiten, typ när ett sound är mellan sista startindexet och 

                        // Snap the sound to the limit.
                        for (int k = snapLimits[j-1]; k < allTrimmedSounds[numSnappedSounds].Length + snapLimits[j-1]; k++)
                        {   
                            // Copy the sound over to the quantized position.
                            newQuantizedLoop[k] = allTrimmedSounds[numSnappedSounds][k-snapLimits[j-1]];
                        }
                        numSnappedSounds++;
                    }
                    else // Closer to the right snap limit.
                    {
                        // Snap the sound to the limit.
                        for (int k = snapLimits[j]; k < allTrimmedSounds[numSnappedSounds].Length + snapLimits[j]; k++)
                        {
                            // Copy the sound over to the quantized position.
                            newQuantizedLoop[k] = allTrimmedSounds[numSnappedSounds][k - snapLimits[j]];
                        }
                        numSnappedSounds++;
                    }
                }
            }
            // Assigna ljudet med en ny forloop? som kopierar ljud över från 

            // KODEN FUNKAR INTE, INFINITY LOOP , DEBUGGA EFTER LUNCH MED PRINTOUTS
        }

        //for(int i = 0; i < newQuantizedLoop.Length; i++)
        //{
        //    Debug.Log("newQuantizedLoop[i] = " + newQuantizedLoop[i]);
        //}

        // Spela upp den
        audioSource.clip = AudioClip.Create("Quantized sound", newQuantizedLoop.Length, audioSource.clip.channels, 44100, false);
        audioSource.clip.SetData(newQuantizedLoop, 0);
        audioSource.loop = true;
        audioSource.Play();
        Debug.Log("¨PLAYING QUANTIZED LOOP!");




        // Dags att bygga upp en ny loop, kolla så den har samma längd, vet inte om bpm är relevant?
        // Ha en forloop där if(idx = en av gränserna) -> lägg dit ljudet i ordning

        // Problem: måste ta hänsyn till hur den låg i originalinspelningen
        // På något sätt jämföra med originalinspelningen, men hur ska man göra det?
        // ... kanskeeeee spara alla deras originalindex i en array, dvs deras startIndex när man cuttar ut
        // dem från originalinspelningen ovan.




        Debug.Log("Kommer du hit så har koden inte fastnat iaf.");
    }








    private bool IsItQuietBeforeTheTransient(int idx)
    {
        return (System.Math.Abs(recording[idx]) < BeforeSoundThreshold) ? true : false;
    }

    private bool DoesSoundExistsAfterTransient(int idx)
    {
        bool soundExistsAfterTransient;

        // Investigate if a sound appears in the recording by looking at the current index and an index further ahead.
        if (idx < recording.Length - rangeToInvestigateInSamples)
        {
            soundExistsAfterTransient = (System.Math.Abs(recording[idx + rangeToInvestigateInSamples]) > AfterSoundThreshold) ? true : false;
        }
        else 
        {
            // Regulating the index that looks ahead of the current index to keep it from exceeding the range of the array.
            int indexAhead = idx + rangeToInvestigateInSamples;
            int howMuchOverTheLength = indexAhead - recording.Length;
            indexAhead = recording.Length - rangeToInvestigateInSamples + howMuchOverTheLength;
  
            soundExistsAfterTransient = (System.Math.Abs(recording[indexAhead]) > AfterSoundThreshold) ? true : false;
        }

        return soundExistsAfterTransient;
    }

    private int GetStartIndex(int idx)
    {
        int startIndex;
        int j = 0;
      
        // Goes through the range and detects when sound begins.
        while ((idx + j) < recording.Length && System.Math.Abs(recording[idx + j]) < BeforeSoundThreshold)
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
        while ((idx < recording.Length && System.Math.Abs(recording[idx]) > AfterSoundThreshold) || (idx < recording.Length && stillSoundAhead))
        {
            int howMuchAheadToLook = 2000; // In samples.
            int indexAhead = idx + howMuchAheadToLook;
            if (indexAhead > recording.Length)
            {
                int howMuchOverTheLength = indexAhead - recording.Length;
                indexAhead = recording.Length - howMuchAheadToLook + howMuchOverTheLength;

                stillSoundAhead = (System.Math.Abs(recording[indexAhead]) > AfterSoundThreshold) ? true : false;
            }
            else
            {
                stillSoundAhead = (System.Math.Abs(recording[idx + howMuchAheadToLook]) > AfterSoundThreshold) ? true : false;
            }
            idx++;
        }
        return idx;
    }

    private void SaveTrimmedAudioSegment(int startIndex, int endIndex)
    {
        if (numSavedTrimmedSounds < numSoundsAllowedInLoop)
        {
            allTrimmedSounds[numSavedTrimmedSounds] = GetSegmentFromRecording(startIndex, endIndex);
            numSavedTrimmedSounds++;
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
        return trimmedIndividualSound;
    }


    void Update () {
		
	}




}
