using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class ButtonManager : MonoBehaviour {

    public Sprite greenPlaySprite;
    public Sprite redStopSprite;
    //public Sprite recSprite;

    [SerializeField]
    private CurrentRecButtonSprite currentRecButtonSprite;
    [SerializeField]
    private PlayOrStopSprite playOrStopSprite;

    public GameObject addButton;
    public GameObject recordButton;
    public List<GameObject> allButtons; // Drag and drop the buttons, that will be shown when the add button is pressed, to this list in the inspector. 

    private bool allButtonsActivated = false;
    int indexOfCurrentMicButton = 0;
    bool aRecordingHasStarted = false;
    bool alreadyAddedButton = false;

    private float nextActionTime = 0.0f;
    public float period = 1.0f;

    public int numBeatsPerSegment = 4;
    public float bpm = 120.0F;
    private int flip = 0;
    private double nextEventTime;

    private void Start()
    {
        // Init the array in the scriptable object "PlayStopSprite".
        playOrStopSprite.showPlaySprite = new bool[allButtons.Capacity];
        for (int i = 0; i < playOrStopSprite.showPlaySprite.Length; i++)
            playOrStopSprite.showPlaySprite[i] = true;
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
            Debug.Log("button activated and disabled rec button");
            //currentRecButtonSprite.UpdateRecordingStatus(false);
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

    public void RecordingHasStarted()
    {
        aRecordingHasStarted = true;
        currentRecButtonSprite.UpdateRecordingStatus(true);
        alreadyAddedButton = false;

        recordButton.GetComponentInChildren<Button>().interactable = false; // Button is not clickable when recording.
        nextEventTime = AudioSettings.dspTime; // Sets up time for the recording animation used in "Update()".
    }

    private void Update()
    {
        // Update the button to a play symbol when a recording is over.
        if (aRecordingHasStarted && !alreadyAddedButton && !currentRecButtonSprite.GetRecordingStatus())
        {
            Debug.Log("updating play symbol in update");
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
        bool shouldButtonShowPlaySprite = playOrStopSprite.ShouldButtonShowPlaySprite(indexOfButton);

        if (shouldButtonShowPlaySprite)
            allButtons[indexOfButton].GetComponent<Image>().sprite = greenPlaySprite;
        else
            allButtons[indexOfButton].GetComponent<Image>().sprite = redStopSprite;
    }



}
