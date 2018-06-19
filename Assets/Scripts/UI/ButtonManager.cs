﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles all of the UI in the application such as updating the
/// UI according to the "State" the application is in.
/// Uses the classes "CurrentRecButtonSprite" and "PlayOrStopSprite".
/// A lot of assets etc. needs to be assigned in the inspector.
/// This script is attached to the Canvas object.
/// </summary>
public class ButtonManager : MonoBehaviour {

    [SerializeField]
    private CurrentRecButtonSprite currentRecButtonSprite;
    [SerializeField]
    private PlayOrStopSprite playOrStopSprite;
    [SerializeField]
    private RecordedLoops recordedLoops;

    // Default buttons, assign in the inspector.
    public GameObject addButton;
    public GameObject recordButtonGameObject;

    // Drag and drop the buttons, that will be shown when the add button is pressed, to this list in the inspector. 
    public List<GameObject> allButtons; 

    // Variables for the countdown (not in final version).
    public GameObject countdownImage;
    public List<Sprite> countdownSprites;
    private Button recordButton;

    // Sprites.
    public Sprite dottedSquare;
    public Sprite redCross;
    
    private Text recordButtonText;

    // General variables needed for this class.
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
    public CanvasGroup loopButtonsUICanvasGroup;
    public CanvasGroup yesNoCanvasGroup;
    public CanvasGroup buttonsDefaultCanvasGroup;
    public GameObject editButtonGameObject;
    public CanvasGroup RecordButtonCanvasGroup;
    public CanvasGroup RecordButtonAlphaCanvasGroup;
    private int numDeactivatedButtons;
    private float alphaUIValue = 0.2f;

    // Animation variables.
    public Animator animator;

    // Particle system animation.
    public GameObject particleSystemGameObject;

    // Misc. variables.
    public GameObject textSilentRecordingGameObject;

    private void AssignMethodToRunDuringAnEvent()
    {
        // Subscribing to the event by adding the method to the "publisher".
        ApplicationProperties.changeEvent += MethodToRun; 
    }

    void MethodToRun(State state)
    {
        // Change UI when the state changes.
        switch (state)
        {
            case State.Default:
                {
                    ShowDefaultUI();
                    break;
                }
            case State.Recording:
                {
                    // Play recording animation for the recordButton.
                    animator.Play("recordAnim");

                    // Starts the recording-animation in "Update".
                    startAnimatingRecordingButton = true; 

                    // Animate the circle shaped timer counting down.
                    StartCircleTimer();

                    HighlightRecordingButtonPanel();
                    break;
                }                
            case State.RecordingOver:
                {
                    // Reset the record button's animation.
                    animator.Play("idleAnim");

                    // Disable the record button.
                    recordButtonGameObject.SetActive(false);

                    // Show the animated dots.
                    animateProcessing = true;

                    startAnimatingRecordingButton = false;

                    // Show the default UI again.
                    ShowRecordButton();
                    ShowDefaultUI();
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
                    // Show feedback text that the recording was silent.
                    DisplaySilentRecordingText();

                    // Show the record-button again.
                    ShowRecordButton();

                    DisplaySilentRecordingText();

                    // Hide the animated dots.
                    dotsGameObject.SetActive(false);

                    break;
                }
            case State.ProcessingAudio:
                {
                    // Disable record button.
                    recordButtonGameObject.SetActive(false);

                    // Show the animated dots.
                    dotsGameObject.SetActive(true);
                    animateProcessing = true;

                    // Animate loading text in the dots animation.
                    dotsGameObject.GetComponentInChildren<Animator>().Play("recordAnim");

                    break;
                }
            case State.FinishedProcessing:
                {
                    // Hide the animated dots.
                    dotsGameObject.SetActive(false);
                    animateProcessing = false;                    

                    // Show the record button again.
                    recordButtonGameObject.SetActive(true);
                    break;
                }
            case State.EditMode:
                {
                    // Show the edit mode.
                    ShowEditMode();
                    break;
                }
            case State.FinishedEditing:
                {
                    // Call the function to update the GUI with the updated amount of recordings.
                    UpdateLoopButtonsAndRecordedLoops();
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

    // Play the animation that is shown when the record button is pressed.
    public void AnimateRecordButtonPressed()
    {
        animator.Play("recordPressedAnim");
    }

    // Show the sprite for "play" on all of the buttons for the recorded loops that has a recording.
    // Called when exiting the "edit mode".
    public void DisplayPlaySpritesOnInteractableButtons(bool cameFromEditMode)
    {
        // Display the play sprites when editing is completed.
        if (!(ApplicationProperties.State == State.EditMode) && cameFromEditMode)
        {
            int numButtonsToAddPlaySpriteTo = allButtons.Count - numDeactivatedButtons;
            for (int i = ApplicationProperties.NUM_PRESET_LOOPS; i < numButtonsToAddPlaySpriteTo; i++)
            {
                // Find all buttons that has a recording assigned to it.
                if (allButtons[i].GetComponent<Button>().interactable)
                    allButtons[i].GetComponent<Image>().sprite = playOrStopSprite.GetPlaySprite();
            }
        }
    }

    // Show or hide the edit button.
    private void ShowOrHideEditButton()
    {
        if (recordedLoops.recordings.Count > 0)
            editButtonGameObject.SetActive(true);
        else
            editButtonGameObject.SetActive(false);
    }

    // Displays the default UI.
    public void ShowDefaultUI()
    {
        // Show edit button if recordings exist, else hide it.
        ShowOrHideEditButton();

        // Enable the normal UI.
        EnableLoopButtonsUI();
        EnableDefaultButtons();
        ResetRecordingButtonPanel();

        // Disable the confirmation quit UI.
        DisableYesNoUI();

        // If all slots for recordings have been used then disable the recording button.
        if(recordedLoops.recordings.Count == ApplicationProperties.NUM_POSSIBLE_RECORDINGS)
            DisableRecordingButtonPanel();
    }

    // Show the loop buttons and make them interactable.
    private void EnableLoopButtonsUI()
    {
        loopButtonsUICanvasGroup.alpha = 1;
        loopButtonsUICanvasGroup.interactable = true;
        loopButtonsUICanvasGroup.blocksRaycasts = true;
    }

    // Lower the alpha for the loop buttons and make them non-interactable.
    private void DisableLoopButtonsUI()
    {
        loopButtonsUICanvasGroup.alpha = alphaUIValue;
        loopButtonsUICanvasGroup.interactable = false;
        loopButtonsUICanvasGroup.blocksRaycasts = false;
    }

    // Show the default buttons like preset buttons and the record button.
    private void EnableDefaultButtons()
    {
        buttonsDefaultCanvasGroup.alpha = 1;
        buttonsDefaultCanvasGroup.interactable = true;
        buttonsDefaultCanvasGroup.blocksRaycasts = true;
    }

    // Disable the default buttons.
    private void DisableDefaultButtons()
    {
        buttonsDefaultCanvasGroup.alpha = alphaUIValue;
        buttonsDefaultCanvasGroup.interactable = false;
        buttonsDefaultCanvasGroup.blocksRaycasts = false;
    }

    // Disable the yes/no-popup.
    private void DisableYesNoUI()
    {
        // Disable yes/no popup.
        yesNoPopupGameObject.SetActive(false);

        yesNoCanvasGroup.alpha = 0;
        yesNoCanvasGroup.interactable = false;
        yesNoCanvasGroup.blocksRaycasts = false;
    }

    // Highlight the recording button, this is called during a recording.
    private void HighlightRecordingButtonPanel()
    {
        // Disable everything else except the panel for the recording button.
        DisableDefaultButtons();
        DisableLoopButtonsUI();

        // Set up the recording button during a recording.
        PrepareRecordButtonPanelDuringRecording();
    }

    // Prepares how the UI-elements should look during a recording.
    // Called from the function "HighlightRecordingButtonPanel".
    private void PrepareRecordButtonPanelDuringRecording()
    {
        // Show the record button canvas group 
        // and make them non-interactable.
        RecordButtonCanvasGroup.alpha = 1;
        RecordButtonCanvasGroup.interactable = false;
        RecordButtonCanvasGroup.blocksRaycasts = false;

        // Lower the alpha value for the recording button.
        RecordButtonAlphaCanvasGroup.alpha = 0.5f;
    }

    // Reset the panel for the recording button.
    private void ResetRecordingButtonPanel()
    {
        RecordButtonCanvasGroup.alpha = 1;
        RecordButtonCanvasGroup.interactable = true;
        RecordButtonCanvasGroup.blocksRaycasts = true;

        RecordButtonAlphaCanvasGroup.alpha = 1.0f;
    }

    // Disable the panel for the recording button.
    private void DisableRecordingButtonPanel()
    {
        RecordButtonCanvasGroup.alpha = 0.0f;
        RecordButtonCanvasGroup.interactable = false;
        RecordButtonCanvasGroup.blocksRaycasts = true;
    }

    // Assign the sprite for dotted lines around all buttons for the recorded loops.
    private void MakeAllButtonsDotted()
    {
        int startIndex = ApplicationProperties.NUM_PRESET_LOOPS;
        for(int i = startIndex; i < allButtons.Count; i++)
        {
            allButtons[i].GetComponent<Image>().sprite = dottedSquare;
            allButtons[i].GetComponent<Button>().interactable = false;
        }
    }

    // Show the edit mode.
    private void ShowEditMode()
    {
        Debug.Log("Showing edit mode.");

        // Stop the "glow animation" on all preset buttons from playing.
        for(int i = ApplicationProperties.NUM_PRESET_LOOPS; i < allButtons.Capacity; i++)
        {
            Animator animator = GetGlowAnimator(i);
            animator.Play("idle");
        }

        // Reset variables below, that keep track of the editing.
        // Reset number of deactivated buttons-counter.
        numDeactivatedButtons = 0;

        // Clear the list of saved indices for removed recordings.
        indicesForRemovedRecordings.Clear(); 

        // Show red cross sprites on recording buttons.
        ShowRedCrossSprites();

        // Disable the default buttons UI.
        DisableDefaultButtons();
        DisableRecordingButtonPanel();
    }

    // Assign the sprite for red crosses. 
    // Called when entering the edit mode.
    private void ShowRedCrossSprites()
    {
        int skipPresetLoops = ApplicationProperties.NUM_PRESET_LOOPS;
        for (int i = skipPresetLoops; i < allButtons.Count; i++)
        {
            // Find all buttons that actually has a recording.
            if (allButtons[i].GetComponent<Button>().interactable)
            {
                allButtons[i].GetComponent<Image>().sprite = redCross;

                // Play alpha transition animation.
                allButtons[i].GetComponent<Animator>().Play("editModeTransitionAnim");
            }
        }
    }

    // Show the yes/no-popup.
    public void ShowYesNoPopup()
    {
        if (ApplicationProperties.State == State.EditMode)
        {
            // Activate the Canvas group.
            yesNoPopupGameObject.SetActive(true);

            yesNoPopupGameObject.GetComponent<Animator>().Play("yesNoAnim");

            // Reduce the visibility of normal UI, and disable all interraction.
            DisableLoopButtonsUI();
            DisableDefaultButtons();

            // Enable interraction with confirmation gui and make visible.
            yesNoCanvasGroup.alpha = 1;
            yesNoCanvasGroup.interactable = true;
            yesNoCanvasGroup.blocksRaycasts = true;
        }
    }

    // While still in edit mode.
    // Called when pressing "no" in the yes/no-popup.
    public void KeepEditModeAlphaForCanvasGroups()
    {
        // Disable the confirmation quit UI.
        DisableYesNoUI();

        DisableDefaultButtons();
        EnableLoopButtonsUI();
    } 

    // Save the index of a deleted recording.
    public void SaveIndexOfDeletedRecording(int index)
    {
        if(ApplicationProperties.State == State.EditMode)
            indicesForRemovedRecordings.Add(index - ApplicationProperties.NUM_PRESET_LOOPS);
    }

    // Remove the last saved indes from the list that keeps track of removed indices.
    public void RemoveLastSavedIndexFromKeepTrackOfIndicesList()
    {
        if (ApplicationProperties.State == State.EditMode)
            indicesForRemovedRecordings.RemoveAt(indicesForRemovedRecordings.Count - 1);
    }

    // Deactivates the last button in the gridlayout for the buttons for the recorded loops.
    public void DeactivateLastButton()
    {
        // Only execute this code if the app is in the edit mode state.
        if(ApplicationProperties.State == State.EditMode)
        {
            // Hide/Show UI components accordingly.
            DisableYesNoUI();
            EnableLoopButtonsUI();

            int indexForButton = indicesForRemovedRecordings[indicesForRemovedRecordings.Count - 1] + ApplicationProperties.NUM_PRESET_LOOPS;

            // Play the animation for removing a button.
            allButtons[indexForButton].GetComponent<Animator>().Play("removeLoopAnim");

            // Get the index of the last button.
            int indexOfLastButton = 0;

            // If all of the buttons have been activated, then get the index of the last button.
            if (allButtonsAreInteractable)
            {
                indexOfLastButton = allButtons.Count - 1;
                allButtonsAreInteractable = false;
            }
            else // If not all buttons have been activated, find the index of the last active button.
            {
                // Get the index to the left of the first inactive button.
                indexOfLastButton = FindFirstNonInteractableButton() - 1; 
            }

            // Set non interactable because the buttons should show the dotted lined sprite later.
            Button tempButton = allButtons[indexOfLastButton].GetComponent<Button>();
            tempButton.interactable = false;

            // Set dotted line sprite to the non interactable button.
            tempButton.GetComponent<Image>().sprite = dottedSquare;

            // Keep track of the number of deactivated buttons.
            numDeactivatedButtons++; 

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

    // Update the list that keeps all of the recordings in the scriptable object "RecordedLoops".
    // Called after editing has been performed.
    private void UpdateLoopButtonsAndRecordedLoops()
    {
        // Remove recordings from the list with the indices that have been saved in the list 
        // that keeps tracks of the indices of recordings to remove.
        for(int i = 0; i < indicesForRemovedRecordings.Count; i++)
        {
            recordedLoops.recordings.RemoveAt(indicesForRemovedRecordings[i]);
        }
    }

    // 
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
        //textSilentRecordingGameObject.SetActive(false);

        // Sätt blå record sprite här
        currentRecButtonSprite.SetToStartRecSprite();
        GetCurrentRecButtonSprite();
        recordButtonGameObject.SetActive(true);
        //recordButtonGameObject.GetComponent<Animator>().Play("recordButtonAppearAnim");
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

    private void PlayParticleSystemAnimation(int indexOfButton)
    {
        // Get the coordinates for the button.
        Vector3[] worldCorners = new Vector3[4];
        allButtons[indexOfButton].GetComponent<Button>().GetComponent<RectTransform>().GetWorldCorners(worldCorners);
        Quaternion rotationForButton = allButtons[indexOfButton].transform.rotation;

        // Calculate the position for the particle animation game object.
        // worldCorners[1] = Top Left, worldCorners[2] == Top right
        float distanceXaxis = System.Math.Abs((worldCorners[2].x - worldCorners[1].x ) / 2.0f);
        worldCorners[1].x = worldCorners[1].x + distanceXaxis;
        Vector3 centerPointForButton = worldCorners[1];

        // Assign the coordinates.
        particleSystemGameObject.transform.SetPositionAndRotation(centerPointForButton, rotationForButton);

        // Trigger the animation.
        particleSystemGameObject.SetActive(true);

        // Disabling the particle game object after it has played.
        Invoke("DisableParticleGameObject", 2.0f);        
    }

    private void DisableParticleGameObject()
    {
        particleSystemGameObject.SetActive(false);
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

            // Play sparkle particle system.
            PlayParticleSystemAnimation(indexForButton);          
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

                // If a button for a recorded loop.
                if (indexOfButton > ApplicationProperties.NUM_PRESET_LOOPS - 1)
                {
                    Animator animator = GetGlowAnimator(indexOfButton);
                    animator.Play("idle");
                }
                else // If a preset loop button.
                {
                    // Stop glow animation. Preset buttons only have one animator component.
                    allButtons[indexOfButton].GetComponentInChildren<Animator>().Play("idle");
                }               
            }
            else
            {
                playOrStopSprite.SetIfButtonShouldShowPlaySprite(indexOfButton, true);
                allButtons[indexOfButton].GetComponent<Image>().sprite = playOrStopSprite.GetStopSprite();

                // If a button for a recorded loop.
                if(indexOfButton > ApplicationProperties.NUM_PRESET_LOOPS - 1)
                {
                    Animator animator = GetGlowAnimator(indexOfButton);
                    animator.Play("glowAnimation");
                }
                else // If a preset loop button.
                {
                    allButtons[indexOfButton].GetComponentInChildren<Animator>().Play("glowAnimation");
                }
            }
        }
    }

    // Get the glow animator in buttons for recorded loops, needed because they have two animators.
    private Animator GetGlowAnimator(int indexForButton)
    {
        Component[] animatorControllers;

        // Since there are two animators in the gameobject for each button,
        // we need to get the last animator which controls the glow animation.
        animatorControllers = allButtons[indexForButton].GetComponentsInChildren(typeof(Animator));
        int indexOfLastController = animatorControllers.Length - 1;

        return animatorControllers[indexOfLastController].GetComponent<Animator>();
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
        recordButtonText.text = "Spela in dig själv";
    }

    private void DisplaySilentRecordingText()
    {
        ChangeTextAlignmentForRecordButton(TextAnchor.LowerCenter);
        recordButtonText.fontSize = smallFontSize;
        recordButtonText.text = "Tyst inspelning, spela in igen";
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

        // Record button is not clickable when recording.
        recordButton.interactable = false;

        // Sets up time for the recording animation used in "Update()".
        //nextEventTime = AudioSettings.dspTime; 
    }
}