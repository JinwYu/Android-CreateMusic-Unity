using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class ButtonManager : MonoBehaviour {

    //public Sprite greenPlaySprite;
    //public Sprite redStopSprite;
    //public Sprite recSprite;

    public MicrophoneCapture microphoneCapture; // Don't like calling the script like this but it works for now.

    [SerializeField]
    private CurrentRecButtonSprite currentRecButtonSprite;
    [SerializeField]
    private PlayOrStopSprite playOrStopSprite;
    [SerializeField]
    private RecordedLoops recordedLoops;

    public GameObject addButton;
    public GameObject recordButton;
    public List<GameObject> allButtons; // Drag and drop the buttons, that will be shown when the add button is pressed, to this list in the inspector. 
    public GameObject countdownImage;
    public List<Sprite> countdownSprites;

    private bool allButtonsActivated = false;
    int indexOfCurrentMicButton = 0;
    bool startAnimatingRecordingButton = false;
    bool alreadyAddedButton = false;
    private int flip = 0;
    private double nextEventTime;
    private float currentCountdownValue;

    private void AssignMethodToRunDuringAnEvent()
    {
        ApplicationProperties.changeEvent += MethodToRun; // Subscribing to the event by adding the method to the "publisher".
    }

    void MethodToRun(State state)
    {
        //Debug.Log("New state in ButtonManager = " + state);  // This will trigger anytime you call MoreApples() on ClassA

        switch (state)
        {
            case State.Recording:
                {
                    startAnimatingRecordingButton = true; // Starts the recording-animation in "Update".
                    break;
                }                
            case State.RecordingOver:
                {
                    startAnimatingRecordingButton = false;
                    break;
                }                
            case State.SavedRecording:
                {
                    currentRecButtonSprite.SetToPlaySprite();
                    currentRecButtonSprite.UpdateRecordingStatus(false);
                    ActivateNewButton();
                    break;
                }
            case State.SilentRecording:
                {
                    // Disable the button last button to be added.
                    int lastButtonIndex = FindFirstInactiveButton();
                    allButtons[lastButtonIndex].SetActive(false);

                    // Disable add-button.
                    addButton.SetActive(false);

                    // Show the record-button again.
                    ShowRecordButton();
                    break;
                }
            default:
                break;
        }
    }

    private void Start()
    {
        AssignMethodToRunDuringAnEvent();

        // Init the array in the scriptable object "PlayStopSprite".
        playOrStopSprite.showPlaySprite = new bool[allButtons.Capacity];
        for (int i = 0; i < playOrStopSprite.showPlaySprite.Length; i++)
            playOrStopSprite.showPlaySprite[i] = false;
    }

    public void ShowRecordButton()
    {
        addButton.transform.SetAsLastSibling(); // Since the add button has been clicked on, move it to the end of the gridlayout.

        addButton.SetActive(false);

        // Sätt blå record sprite här
        currentRecButtonSprite.SetToStartRecSprite();
        GetCurrentRecButtonSprite();
        recordButton.SetActive(true);
        recordButton.GetComponentInChildren<Button>().interactable = true;
        recordButton.GetComponentInChildren<Text>().text = "SPELA IN";
    }

    public int FindFirstInactiveButton()
    {
        int i = 0;

        int size = allButtons.Count;
        while (!allButtonsActivated && i < size)
        {
            if (!allButtons[i].activeSelf)
                break;

            i++;
        }

        return i;
    }

    public void ActivateNewButton()
    {
        // Find the first inactive button.
        int i = FindFirstInactiveButton();

        if (!allButtonsActivated)
        {
            allButtons[i].SetActive(true); // Activate the first inactive button.
            alreadyAddedButton = true;
            startAnimatingRecordingButton = false;
            recordButton.SetActive(false);
            addButton.SetActive(true);
        }

        // If every button is activated remove the add button.
        int size = allButtons.Count;
        if (i == (size - 1))
        {
            addButton.SetActive(false);
            allButtonsActivated = true;
        }

        indexOfCurrentMicButton = i; // Save the index of the current mic button that was added.
    }

    public void GetCurrentRecButtonSprite()
    {
        recordButton.GetComponent<Image>().sprite = currentRecButtonSprite.GetCurrentSprite();
    }

    private void Update()
    {
        // Update the button to a play symbol when a recording is over.
        //if (aRecordingHasStarted && !alreadyAddedButton && !currentRecButtonSprite.GetRecordingStatus())
        //{
        //    alreadyAddedButton = false;
        //    ActivateNewButton();      
        //}

        // Animate that a recording is in progress by switching between two sprites each beat.
        double time = AudioSettings.dspTime;       
        if (startAnimatingRecordingButton)
        {
            if (time + 1.0F > nextEventTime)
            {
                if(flip == 0)
                    currentRecButtonSprite.SetToRecInProgSprite2();
                else
                    currentRecButtonSprite.SetToRecInProgSprite1();

                GetCurrentRecButtonSprite();
                flip = 1 - flip;

                nextEventTime += 60.0F / ApplicationProperties.BPM * ApplicationProperties.NUM_BEATS_PER_LOOP / 8;
            }
        }
    }

    public void ShowPlayOrStopSprite(int indexOfButton)
    {
        if (playOrStopSprite.showPlaySprite[indexOfButton])
        {
            playOrStopSprite.showPlaySprite[indexOfButton] = false;
            allButtons[indexOfButton].GetComponent<Image>().sprite = playOrStopSprite.GetPlaySprite();
            //Debug.Log("assign GEREEEEEEN STRIPE");
        }
        else
        {
            playOrStopSprite.SetIfButtonShouldShowPlaySprite(indexOfButton, true);
            allButtons[indexOfButton].GetComponent<Image>().sprite = playOrStopSprite.GetStopSprite();
            //Debug.Log("else, assign RED STRIPE");
        }
    }

    public void StartCountdown()
    {
        StartCoroutine(StartCountingDown());
        countdownImage.SetActive(true);
        recordButton.SetActive(false);
    }

    public IEnumerator StartCountingDown(float countdownValue = 3)
    {
        currentCountdownValue = countdownValue;
        while (currentCountdownValue >= 0)
        {
            int currentSecond = (int)currentCountdownValue;
           
            // Change sprite accordingly.
            switch (currentSecond)
            {
                case 0:
                    countdownImage.SetActive(false); // Hide countdown.
                    recordButton.SetActive(true);
                    RecordingHasStarted(); // Start recording.
                    break;
                case 1:
                    countdownImage.GetComponentInChildren<Image>().sprite = countdownSprites[0];
                    break;
                case 2:
                    countdownImage.GetComponentInChildren<Image>().sprite = countdownSprites[1];
                    break;
                case 3:
                    countdownImage.GetComponentInChildren<Image>().sprite = countdownSprites[2];
                    break;
                default:
                    break;
            }
            yield return new WaitForSeconds(1.0f);
            currentCountdownValue--;
        }
    }

    public void StartRecording()
    {
        //countdownImage.SetActive(false); // Hide countdown.
        recordButton.GetComponentInChildren<Text>().text = "";
        recordButton.SetActive(true);
        RecordingHasStarted(); // Start recording.
    }

    public void RecordingHasStarted()
    {
        //aRecordingHasStarted = true;
        currentRecButtonSprite.UpdateRecordingStatus(true);
        alreadyAddedButton = false;

        //microphoneCapture.StartRecording(); // Ugly solution calling another script like this but it works for now.

        recordButton.GetComponentInChildren<Button>().interactable = false; // Button is not clickable when recording.
        nextEventTime = AudioSettings.dspTime; // Sets up time for the recording animation used in "Update()".
    }



}
