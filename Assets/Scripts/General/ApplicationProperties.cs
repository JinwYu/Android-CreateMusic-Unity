using UnityEngine;

// Global enums that describes the different states in the application.
public enum State
{
    Default,
    Recording,
    RecordingOver,
    SavedRecording,
    SilentRecording,
    SilentInQuantization,
    ProcessingAudio,
    FinishedProcessing,
    EditMode,
    FinishedEditing,
    CoroutineFinished
}

/// <summary>
/// Contains basic properties that describes the application and they are reachable from every class.
/// Sets up the event system that alerts all subscribers when the "State" is changed.
/// Also initialises the overall audio volume for the application.
/// </summary>
public class ApplicationProperties : MonoBehaviour {

    // General variables for the application. Accessible from all classes.
    public const int NUM_POSSIBLE_RECORDINGS = 6;
    public const int NUM_PRESET_LOOPS = 2;
    public const int NUM_BEATS_PER_LOOP = 8;
    public const int BPM = 116;

    // Keep track of how many loops that have been gated.
    public static int numGatedLoops = 0;

    // Default volume level for the whole application.
    private const float defaultVolumeLevel = 0.55f;

    // Default volume level for the audio sources that plays each loop.
    public const float DEFAULT_VOLUME_LEVEL = 0.875f;   
    public const float VOLUME_DURING_RECORDING_LEVEL = 0.5f;

    // Event variables.
    public delegate void ChangeEvent(State state);
    public static event ChangeEvent changeEvent;
    public static State state = State.Default;

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
        // If there's a subscriber to the event.
        if (changeEvent != null) 
            changeEvent(state);
    }

    private void Start()
    {
        // Initialize basic variables. 
        Init();       
    }

    private void Init()
    {
        // Set initial default state.
        state = State.Default;

        // Set default volume level for the whole application.
        SetApplicationVolumeLevel(defaultVolumeLevel);
    }

    private void SetApplicationVolumeLevel(float level)
    {
        AudioListener.volume = level;
    }
}
