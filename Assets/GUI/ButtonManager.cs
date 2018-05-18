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
    bool aRecordingHasStarted = false;
    bool alreadyAddedButton = false;
    private int numBeatsPerSegment;
    private float bpm;
    private int flip = 0;
    private double nextEventTime;
    private float currentCountdownValue;

    private void Start()
    {
        // Init the array in the scriptable object "PlayStopSprite".
        playOrStopSprite.showPlaySprite = new bool[allButtons.Capacity];
        for (int i = 0; i < playOrStopSprite.showPlaySprite.Length; i++)
            playOrStopSprite.showPlaySprite[i] = true;

        numBeatsPerSegment = recordedLoops.numBeatsPerSegment;
        bpm = recordedLoops.bpm;
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

    public void ActivateNewButton()
    {
        // Find the first inactive button.
        int i = 0;
        int size = allButtons.Count;
        while (!allButtonsActivated && i < size)
        {
            if (!allButtons[i].activeSelf)
                break;

            i++;
        }

        if (!allButtonsActivated)
        {
            allButtons[i].SetActive(true); // Activate the first inactive button.
            alreadyAddedButton = true;
            aRecordingHasStarted = false;
            recordButton.SetActive(false);
            addButton.SetActive(true);
        }

        // If every button is activated remove the add button.
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
        if (aRecordingHasStarted && !alreadyAddedButton && !currentRecButtonSprite.GetRecordingStatus())
        {
            alreadyAddedButton = false;
            ActivateNewButton();      
        }

        // Animate that a recording is in progress by switching between two sprites each beat.
        double time = AudioSettings.dspTime;       
        if (aRecordingHasStarted)
        {
            if (time + 1.0F > nextEventTime)
            {
                if(flip == 0)
                    currentRecButtonSprite.SetToRecInProgSprite2();
                else
                    currentRecButtonSprite.SetToRecInProgSprite1();

                GetCurrentRecButtonSprite();
                flip = 1 - flip;

                nextEventTime += 60.0F / bpm * numBeatsPerSegment / 8;
            }
        }
    }

    public void ShowPlayOrStopSprite(int indexOfButton)
    {
        if (playOrStopSprite.showPlaySprite[indexOfButton])
        {
            playOrStopSprite.showPlaySprite[indexOfButton] = false;
            allButtons[indexOfButton].GetComponent<Image>().sprite = playOrStopSprite.GetPlaySprite();
        }
        else
        {
            playOrStopSprite.SetIfButtonShouldShowPlaySprite(indexOfButton, true);
            allButtons[indexOfButton].GetComponent<Image>().sprite = playOrStopSprite.GetStopSprite();
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
        aRecordingHasStarted = true;
        currentRecButtonSprite.UpdateRecordingStatus(true);
        alreadyAddedButton = false;

        //microphoneCapture.StartRecording(); // Ugly solution calling another script like this but it works for now.

        recordButton.GetComponentInChildren<Button>().interactable = false; // Button is not clickable when recording.
        nextEventTime = AudioSettings.dspTime; // Sets up time for the recording animation used in "Update()".
    }



}
