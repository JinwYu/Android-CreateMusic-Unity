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
    }
}

// Then you subscribe to the even from any class. and if the event is triggered (changed) then your other class will be notified.

public class ClassB
{
    void OnEnable()
    {
        ApplicationProperties.changeEvent += StateChanged; // subscribing to the event. 
    }

    void StateChanged(State state)
    {
        Debug.Log("New state = " + state);  // This will trigger anytime you call MoreApples() on ClassA
    }
}
