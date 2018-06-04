using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

// Global enums.
public enum State
{
    Default,
    Recording,
    RecordingOver,
    SavedRecording,
    SilentRecording,
    SilentInQuantization,
    ProcessingAudio,
    FinishedProcessing
}

public enum Command
{
    None,
    Restart,
    SilentRecording
}

public class ApplicationProperties : MonoBehaviour {

    // General variables.
    public const int NUM_POSSIBLE_RECORDINGS = 6;
    public const int NUM_PRESET_LOOPS = 2;
    public const int NUM_BEATS_PER_LOOP = 8;
    public const int BPM = 116;
    public static int numGatedLoops = 0; // Keep track of how many loops that have been gated.
    private const float defaultVolumeLevel = 0.65f;

    // Event variables.
    public delegate void ChangeEvent(State state); //I do declare!
    public static event ChangeEvent changeEvent;  // create an event variable 
    public static State state = State.Default;

    //private Command command;

    public static State State
    {
        get { return state; }
        set
        {
            // Alert the subscribers if the value has changed.
            if(state != value)
            {
                state = value;
                OnStateChanged();
            }
        }
    }

    protected static void OnStateChanged()
    {
        if (changeEvent != null) // If there's a subscriber to the event.
            changeEvent(state);
    }

    private void Start()
    {
        Init(); // Initialize basic variables.       
    }

    private void Init()
    {
        // Set initial default state.
        state = State.Default;

        // Set default volume level for the whole application.
        SetVolumeLevel(defaultVolumeLevel);
    }

    private void SetVolumeLevel(float level)
    {
        AudioListener.volume = level;
    }
}
