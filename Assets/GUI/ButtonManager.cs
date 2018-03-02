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
        recordButton.SetActive(true);
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
            recordButton.SetActive(false);
        }

        // If every button is activated remove the add button.
        if (i == size - 1)
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
    }

    private void Update()
    {
        // Update the button to a play symbol when a recording is over.
        if (aRecordingHasStarted && !alreadyAddedButton && !currentRecButtonSprite.GetRecordingStatus())
            ActivateNewButton(); 
    }

    public void ShowPlayOrStopSprite(int indexOfButton)
    {
        bool shouldButtonShowPlaySprite = playOrStopSprite.ShouldButtonShowPlaySprite(indexOfButton);

        if (shouldButtonShowPlaySprite)
        {
            allButtons[indexOfButton].GetComponent<Image>().sprite = greenPlaySprite;
        }
        else
        {
            allButtons[indexOfButton].GetComponent<Image>().sprite = redStopSprite;
        }
    }



}
