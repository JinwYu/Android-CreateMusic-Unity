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
    public GameObject recordButtonGameObject;
    public List<GameObject> allButtons; // Drag and drop the buttons, that will be shown when the add button is pressed, to this list in the inspector. 
    public GameObject countdownImage;
    public List<Sprite> countdownSprites;
    private Button recordButton;

    // Sprites.
    public Sprite dottedSquare;
    public Sprite redCross;
    
    private Text recordButtonText;

    private bool allButtonsAreInteractable = false;
    int indexOfCurrentMicButton = 0;
    bool startAnimatingRecordingButton = false;
    bool alreadyAddedButton = false;
    private int flip = 0;
    private double nextEventTime;
    private float currentCountdownValue;

    // Circle bar timer variables.
    public GameObject circleGameObject;
    private Image circle;
    private float maxTime;
    private float delayFromMicrophoneCapture;
    private float timeLeftCircle;

    // Record-button text variables.
    int largeFontSize = 60;
    int smallFontSize = 60;

    // Processing variables.
    private float timeLeftProcessingCircle;
    private float maxTimeProcessingCircle = 4.0f;
    bool animateProcessing = false;

    // Loading dots animation variables.    
    private float repeatTime = 0.8f;   // The total time of the animation.
    private float bounceTime = 0.22f;   // The time for a dot to bounce up and come back down.
    private float bounceHeight = 40;    // How far does each dot move.
    public Transform[] dots;    // Assigned in the inspector.
    public GameObject dotsGameObject;

    // Edit/Remove mode variables.
    private List<int> indicesForRemovedRecordings;
    public GameObject yesNoPopupGameObject;
    public CanvasGroup gameUICanvasGroup;
    public CanvasGroup yesNoCanvasGroup;
    public CanvasGroup buttonsDefaultCanvasGroup;
    public GameObject editButtonGameObject;
    public CanvasGroup RecordButtonCanvasGroup;
    public CanvasGroup RecordButtonAlphaCanvasGroup;
    private int numDeactivatedButtons;
    private float alphaUIValue = 0.2f;

    // Animation variables.
    public Animator animator;
    public Animator loopButtonsAnimator;

    private void AssignMethodToRunDuringAnEvent()
    {
        ApplicationProperties.changeEvent += MethodToRun; // Subscribing to the event by adding the method to the "publisher".
    }

    void MethodToRun(State state)
    {
        switch (state)
        {
            case State.Default:
                {
                    ShowDefaultUI();
                    break;
                }
            case State.Recording:
                {
                    animator.Play("recordAnim");

                    startAnimatingRecordingButton = true; // Starts the recording-animation in "Update".

                    // Animate the circle shaped timer counting down.
                    StartCircleTimer();

                    HighlightRecordingButtonPanel();

                    break;
                }                
            case State.RecordingOver:
                {
                    // Disable the record button.
                    recordButtonGameObject.SetActive(false);

                    // Show the animated dots.
                    animateProcessing = true;

                    animator.Play("idleAnim");

                    startAnimatingRecordingButton = false;
                    ShowRecordButton();
                    ShowDefaultUI();

                    // Enable the record button again.
                    //recordButtonGameObject.SetActive(true);

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
                    //int lastButtonIndex = FindFirstInactiveButton();
                    //allButtons[lastButtonIndex].SetActive(false);

                    //// Disable add-button.
                    //addButton.SetActive(false);

                    // Show the record-button again.
                    ShowRecordButton();

                    Debug.Log("SILENTRECORDING IN EVENTHANDLER IN BUTTONMANAGER!!!!!!!!!!!!");

                    break;
                }
            case State.ProcessingAudio:
                {
                    // Disable record button.
                    recordButtonGameObject.SetActive(false);

                    dotsGameObject.SetActive(true);
                    animateProcessing = true;

                    // Animate loading text in the dots animation.
                    dotsGameObject.GetComponentInChildren<Animator>().Play("recordAnim");

                    break;
                }
            case State.FinishedProcessing:
                {
                    dotsGameObject.SetActive(false);
                    animateProcessing = false;                    

                    recordButtonGameObject.SetActive(true);
                    break;
                }
            case State.EditMode:
                {
                    Debug.Log("Edit mode state.");

                    // Call edit function.
                    ShowEditMode();

                    break;
                }
            case State.FinishedEditing:
                {
                    // Call the function to update the GUI with the updated amount of recordings.
                    UpdateLoopButtonsAndRecordedLoops();
                    Debug.Log("Updating recorded loops");

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
        playOrStopSprite.showPlaySprite = new bool[allButtons.Count];
        for (int i = 0; i < playOrStopSprite.showPlaySprite.Length; i++)
            playOrStopSprite.showPlaySprite[i] = false;

        // Get the components used in this class.
        recordButton = recordButtonGameObject.GetComponentInChildren<Button>();
        recordButtonText = recordButtonGameObject.GetComponentInChildren<Text>();
        circle = circleGameObject.GetComponent<Image>();

        // Assign the duration of a recording (used for the circle bar).
        float delayOfRecordingInSeconds = MicrophoneCapture.LENGTH_OF_DELAY_IN_SAMPLES / recordedLoops.sampleRate;
        maxTime = recordedLoops.secondsDurationRecording + delayOfRecordingInSeconds;

        // Init variables needed in this class.
        indicesForRemovedRecordings = new List<int>();
        numDeactivatedButtons = 0;

        // Make all of the buttons for the recordings have the dotted line sprite (The initial state).
        MakeAllButtonsDotted();

        // Hide the edit button.
        ShowOrHideEditButton();
    }

    public void AnimateRecordButtonPressed()
    {
        animator.Play("recordPressedAnim");
    }

    public void DisplayPlaySpritesOnInteractableButtons(bool cameFromEditMode)
    {
        // Display the play sprites when editing is completed.
        if (!(ApplicationProperties.State == State.EditMode) && cameFromEditMode)
        {
            int numButtonsToAddPlaySpriteTo = allButtons.Count - numDeactivatedButtons;
            for (int i = ApplicationProperties.NUM_PRESET_LOOPS; i < numButtonsToAddPlaySpriteTo; i++)
            {
                if (allButtons[i].GetComponent<Button>().interactable)
                    allButtons[i].GetComponent<Image>().sprite = playOrStopSprite.GetPlaySprite();
            }
        }
    }

    private void ShowOrHideEditButton()
    {
        if (recordedLoops.recordings.Count > 0)
            editButtonGameObject.SetActive(true);
        else
            editButtonGameObject.SetActive(false);
    }

    public void ShowDefaultUI()
    {
        // Show edit button if recordings exist, else hide it.
        ShowOrHideEditButton();

        // Display play sprites on active buttons for recordings.
        //DisplayPlaySpritesOnInteractableButtons(false);

        // Enable the normal UI.
        EnableGameUI();
        EnableDefaultButtons();
        ResetRecordingButtonPanel();

        // Disable the confirmation quit UI.
        DisableYesNoUI();

        // If all slots for recordings have been used then disable the recording button.
        if(recordedLoops.recordings.Count == ApplicationProperties.NUM_POSSIBLE_RECORDINGS)
        {
            DisableRecordingButtonPanel();
        }
    }

    private void EnableGameUI()
    {
        gameUICanvasGroup.alpha = 1;
        gameUICanvasGroup.interactable = true;
        gameUICanvasGroup.blocksRaycasts = true;
    }

    private void DisableGameUI()
    {
        gameUICanvasGroup.alpha = alphaUIValue;
        gameUICanvasGroup.interactable = false;
        gameUICanvasGroup.blocksRaycasts = false;
    }

    private void EnableDefaultButtons()
    {
        buttonsDefaultCanvasGroup.alpha = 1;
        buttonsDefaultCanvasGroup.interactable = true;
        buttonsDefaultCanvasGroup.blocksRaycasts = true;
    }

    private void DisableDefaultButtons()
    {
        buttonsDefaultCanvasGroup.alpha = alphaUIValue;
        buttonsDefaultCanvasGroup.interactable = false;
        buttonsDefaultCanvasGroup.blocksRaycasts = false;
    }

    private void DisableYesNoUI()
    {
        // Disable yes/no popup.
        yesNoPopupGameObject.SetActive(false);

        yesNoCanvasGroup.alpha = 0;
        yesNoCanvasGroup.interactable = false;
        yesNoCanvasGroup.blocksRaycasts = false;
    }

    private void HighlightRecordingButtonPanel()
    {
        // Disable everything else except the panel for the recording button.
        DisableDefaultButtons();
        DisableGameUI();

        // Set up the recording button during a recording.
        PrepareRecordButtonPanelDuringRecording();
    }

    private void PrepareRecordButtonPanelDuringRecording()
    {
        RecordButtonCanvasGroup.alpha = 1;
        RecordButtonCanvasGroup.interactable = false;
        RecordButtonCanvasGroup.blocksRaycasts = false;

        // Lower the alpha value for the recording button.
        RecordButtonAlphaCanvasGroup.alpha = 0.5f;
    }

    private void ResetRecordingButtonPanel()
    {
        RecordButtonCanvasGroup.alpha = 1;
        RecordButtonCanvasGroup.interactable = true;
        RecordButtonCanvasGroup.blocksRaycasts = true;

        RecordButtonAlphaCanvasGroup.alpha = 1.0f;
    }

    private void DisableRecordingButtonPanel()
    {
        RecordButtonCanvasGroup.alpha = 0.0f;
        RecordButtonCanvasGroup.interactable = false;
        RecordButtonCanvasGroup.blocksRaycasts = true;
    }

    private void MakeAllButtonsDotted()
    {
        int startIndex = ApplicationProperties.NUM_PRESET_LOOPS;
        for(int i = startIndex; i < allButtons.Count; i++)
        {
            allButtons[i].GetComponent<Image>().sprite = dottedSquare;
            allButtons[i].GetComponent<Button>().interactable = false;
        }
    }

    private void ShowEditMode()
    {
        Debug.Log("Showing edit mode.");

        // Reset variables that keep track of the editing.
        numDeactivatedButtons = 0; // Reset number of deactivated buttons-counter.
        indicesForRemovedRecordings.Clear(); // Clear the list of saved indices for removed recordings.

        // Show red cross sprites on recording buttons.
        ShowRedCrossSprites();

        // Disable the default buttons UI.
        DisableDefaultButtons();
        DisableRecordingButtonPanel();
    }

    private void ShowRedCrossSprites()
    {
        int skipPresetLoops = ApplicationProperties.NUM_PRESET_LOOPS;
        for (int i = skipPresetLoops; i < allButtons.Count; i++)
        {
            if (allButtons[i].GetComponent<Button>().interactable)
            {
                allButtons[i].GetComponent<Image>().sprite = redCross; // TODO: Sätt en placeholder kryss sprite.

                // Play alpha transition animation.
                allButtons[i].GetComponent<Animator>().Play("editModeTransitionAnim");
            }
        }
    }

    public void ShowYesNoPopup()
    {
        if (ApplicationProperties.State == State.EditMode)
        {
            // Activate the Canvas group.
            yesNoPopupGameObject.SetActive(true);

            yesNoPopupGameObject.GetComponent<Animator>().Play("yesNoAnim");

            // Reduce the visibility of normal UI, and disable all interraction.
            DisableGameUI();
            DisableDefaultButtons();

            // Enable interraction with confirmation gui and make visible.
            yesNoCanvasGroup.alpha = 1;
            yesNoCanvasGroup.interactable = true;
            yesNoCanvasGroup.blocksRaycasts = true;
        }
    }

    public void KeepEditModeAlphaForCanvasGroups()
    {
        // Disable the confirmation quit UI.
        DisableYesNoUI();

        DisableDefaultButtons();
        EnableGameUI();
    } 

    public void SaveIndexOfDeletedRecording(int index)
    {
        if(ApplicationProperties.State == State.EditMode)
            indicesForRemovedRecordings.Add(index - ApplicationProperties.NUM_PRESET_LOOPS);
    }

    public void RemoveLastSavedIndexFromKeepTrackOfIndicesList()
    {
        if (ApplicationProperties.State == State.EditMode)
            indicesForRemovedRecordings.RemoveAt(indicesForRemovedRecordings.Count - 1);
    }

    public void DeactivateLastButton()
    {
        // Only execute this code if the app is in the edit mode state.
        if(ApplicationProperties.State == State.EditMode)
        {
            // Hide/Show UI components accordingly.
            DisableYesNoUI();
            EnableGameUI();

            int indexForButton = indicesForRemovedRecordings[indicesForRemovedRecordings.Count - 1] + ApplicationProperties.NUM_PRESET_LOOPS;
            allButtons[indexForButton].GetComponent<Animator>().Play("removeLoopAnim");

            // Get the index of the last button.
            int indexOfLastButton = 0;

            if (allButtonsAreInteractable)
            {
                indexOfLastButton = allButtons.Count - 1;
                allButtonsAreInteractable = false;
            }
            else // If not all buttons have been activated.
            {
                indexOfLastButton = FindFirstNonInteractableButton() - 1; // Get the index to the left of the first inactive button.
            }

            // Set non interactable because the buttons should show the dotted lined sprite later.
            Button tempButton = allButtons[indexOfLastButton].GetComponent<Button>();
            tempButton.interactable = false;

            // Set dotted line sprite to the non interactable button.
            tempButton.GetComponent<Image>().sprite = dottedSquare;

            numDeactivatedButtons++; // Keep track of the number of deactivated buttons.

            // Get how many recording buttons that are interactable that are left.
            int startIndex = ApplicationProperties.NUM_PRESET_LOOPS;
            int numInteractableButtons = 0;
            for (int i = startIndex; i < allButtons.Count; i++)
            {
                if (allButtons[i].GetComponent<Button>().interactable)
                    numInteractableButtons++;
            }

            // Exit Edit mode if the last recording was removed and update the list in RecordedLoops.
            if (numInteractableButtons == 0)
            {
                // Update recordings in RecordedLoops.
                UpdateLoopButtonsAndRecordedLoops();

                // Change state because the last recording has been deleted.
                ApplicationProperties.State = State.Default;
            }            
        }
    }

    private void UpdateLoopButtonsAndRecordedLoops()
    {
        // RemoveAt alla som finns i indicesSavedRemoved bla bla.
        for(int i = 0; i < indicesForRemovedRecordings.Count; i++)
        {
            recordedLoops.recordings.RemoveAt(indicesForRemovedRecordings[i]);
        }
    }

    private bool IndexHasBeenRemoved(int index)
    {
        for (int j = 0; j < indicesForRemovedRecordings.Count; j++)
        {
            if (indicesForRemovedRecordings[j] == index)
            {
                return true;
            }
        }

        return false;
    }

    // Set the state when the edit-button has been pressed.
    public void ToggleEditState()
    {
        // Already in edit state, so changing to default state.
        if(ApplicationProperties.State == State.EditMode)
        {
            ApplicationProperties.State = State.FinishedEditing;  // Trigger to update the current GUI.
            DisplayPlaySpritesOnInteractableButtons(true);
            ApplicationProperties.State = State.Default;
        }
        else // Change to edit state.
        {
            ApplicationProperties.State = State.EditMode;
        }
    }

    private void StartCircleTimer()
    {
        circleGameObject.SetActive(true);
        timeLeftCircle = maxTime;
    }

    // Shows the recording button again.
    private void ResetCircleTimer()
    {
        circleGameObject.SetActive(false);
        circle.fillAmount = 1.0f;
    }

    public void ShowRecordButton()
    {
        //addButton.transform.SetAsLastSibling(); // Since the add button has been clicked on, move it to the end of the gridlayout.

        //addButton.SetActive(false);

        // Sätt blå record sprite här
        currentRecButtonSprite.SetToStartRecSprite();
        GetCurrentRecButtonSprite();
        recordButtonGameObject.SetActive(true);
        ResetCircleTimer();
        recordButton.interactable = true;
        DisplayNotRecordingText();
    }

    public int FindFirstInactiveButton()
    {
        int i = 0;

        int size = allButtons.Count;
        while (!allButtonsAreInteractable && i < size)
        {
            if (!allButtons[i].activeSelf)
                break;

            i++;
        }

        return i;
    }

    private int FindFirstNonInteractableButton()
    {
        int i = ApplicationProperties.NUM_PRESET_LOOPS - 1;

        int size = allButtons.Count;
        while (!allButtonsAreInteractable && i < size)
        {
            // Kan det vara för att ingen av knapparna är aktiverade som inte if-satsen nedan fungerar, därför
            // Den aldrig bryts.

            if (allButtons[i].GetComponent<Button>().interactable == false)
                break;

            i++;
        }

        return i;
    }

    public void ActivateNewButton()
    {
        // Find the first non interactable button.
        int indexForButton = FindFirstNonInteractableButton();

        if (!allButtonsAreInteractable)
        {
            allButtons[indexForButton].GetComponent<Button>().interactable = true; // Activate the first inactive button.
            allButtons[indexForButton].GetComponent<Image>().sprite = playOrStopSprite.GetPlaySprite();
            alreadyAddedButton = true;
            startAnimatingRecordingButton = false;
            //recordButton.SetActive(false);
            //addButton.SetActive(true);

            // Animate the transition when a new recording appears.
            allButtons[indexForButton].GetComponent<Animator>().Play("loopButtonHighlightAnim");
        }

        // Find the first inactive button.
        //int i = FindFirstInactiveButton();

        //if (!allButtonsActivated)
        //{
        //    allButtons[i].SetActive(true); // Activate the first inactive button.
        //    alreadyAddedButton = true;
        //    startAnimatingRecordingButton = false;
        //    //recordButton.SetActive(false);
        //    //addButton.SetActive(true);
        //}

        // If every button is activated remove the add button.
        //int size = allButtons.Count;
        //if (i == (size - 1))
        //{
        //    //addButton.SetActive(false);
        //    recordButtonGameObject.SetActive(false);
        //    allButtonsActivated = true;
        //}

        indexOfCurrentMicButton = indexForButton; // Save the index of the current mic button that was added.
    }

    public void GetCurrentRecButtonSprite()
    {
        recordButtonGameObject.GetComponent<Image>().sprite = currentRecButtonSprite.GetCurrentSprite();
    }

    private void AnimateDotsForProcessing()
    {
        // Change each dot's transform property.
        for (int i = 0; i < dots.Length; i++)
        {
            var p = dots[i].localPosition;
            var t = Time.time * 1 / repeatTime * Mathf.PI - p.x;
            var y = (Mathf.Cos(t) - bounceTime) / (1f - bounceTime);
            p.y = Mathf.Max(0, y * bounceHeight);
            dots[i].localPosition = p;
        }
    }

    private void Update()
    {
        // Update the button to a play symbol when a recording is over.
        //if (aRecordingHasStarted && !alreadyAddedButton && !currentRecButtonSprite.GetRecordingStatus())
        //{
        //    alreadyAddedButton = false;
        //    ActivateNewButton();      
        //}

        // Animate processing circle.
        if (animateProcessing)
        {
            AnimateDotsForProcessing();
        }

        // Animate that a recording is in progress by switching between two sprites each beat.
        double time = AudioSettings.dspTime;       
        if (startAnimatingRecordingButton)
        {
            // Animate the circle bar counting down.
            if (timeLeftCircle > 0)
            {
                timeLeftCircle -= Time.deltaTime;
                circle.fillAmount = timeLeftCircle / maxTime;
            }

            // Change Sprites while recording.
            //if (time + 1.0F > nextEventTime)
            //{
            //    if(flip == 0)
            //        currentRecButtonSprite.SetToRecInProgSprite2();
            //    else
            //        currentRecButtonSprite.SetToRecInProgSprite1();

            //    GetCurrentRecButtonSprite();
            //    flip = 1 - flip;

            //    nextEventTime += 60.0F / ApplicationProperties.BPM * ApplicationProperties.NUM_BEATS_PER_LOOP / 8;
            //}
        }
    }

    public void ShowPlayOrStopSprite(int indexOfButton)
    {
        // Show play or stop sprite if the application is not in Edit mode.
        if(!(ApplicationProperties.State == State.EditMode))
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
        else // Keep the red cross sprite;
        {
            //allButtons[indexOfButton].GetComponent<Image>().sprite = redCross;
        }
    }

    public void StartCountdown()
    {
        StartCoroutine(StartCountingDown());
        countdownImage.SetActive(true);
        recordButtonGameObject.SetActive(false);
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
                    recordButtonGameObject.SetActive(true);
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
        DisplayRecordingText();
        recordButtonGameObject.SetActive(true);
        RecordingHasStarted(); // Start recording.
    }

    private void DisplayRecordingText()
    {
        ChangeTextAlignmentForRecordButton(TextAnchor.UpperCenter);
        recordButtonText.fontSize = largeFontSize;
        recordButtonText.text = "SPELAR IN";
    }

    private void DisplayNotRecordingText()
    {
        ChangeTextAlignmentForRecordButton(TextAnchor.LowerCenter);
        recordButtonText.fontSize = smallFontSize;
        recordButtonText.text = "Spela in";
    }

    private void ChangeTextAlignmentForRecordButton(TextAnchor textAnchor)
    {
        recordButtonText.alignment = textAnchor;
    }

    public void RecordingHasStarted()
    {
        //aRecordingHasStarted = true;
        currentRecButtonSprite.UpdateRecordingStatus(true);
        alreadyAddedButton = false;

        //microphoneCapture.StartRecording(); // Ugly solution calling another script like this but it works for now.

        recordButton.interactable = false; // Button is not clickable when recording.
        nextEventTime = AudioSettings.dspTime; // Sets up time for the recording animation used in "Update()".
    }
}
