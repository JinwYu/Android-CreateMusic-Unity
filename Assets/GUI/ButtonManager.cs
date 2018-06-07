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
    
    private Text recordButtonText;

    private bool allButtonsActivated = false;
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
    int smallFontSize = 30;

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
    
    // hämta själva gameobject i start, så jag kan göra setActive i switchen.


    private void AssignMethodToRunDuringAnEvent()
    {
        ApplicationProperties.changeEvent += MethodToRun; // Subscribing to the event by adding the method to the "publisher".
    }

    void MethodToRun(State state)
    {
        switch (state)
        {
            case State.Recording:
                {
                    startAnimatingRecordingButton = true; // Starts the recording-animation in "Update".

                    // Animate the circle shaped timer counting down.
                    StartCircleTimer();

                    break;
                }                
            case State.RecordingOver:
                {
                    startAnimatingRecordingButton = false;
                    ShowRecordButton();
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
            case State.ProcessingAudio:
                {
                    animateProcessing = true;
                    dotsGameObject.SetActive(true);

                    break;
                }
            case State.FinishedProcessing:
                {
                    animateProcessing = false;
                    dotsGameObject.SetActive(false);
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

        // Get the components used in this class.
        recordButton = recordButtonGameObject.GetComponentInChildren<Button>();
        recordButtonText = recordButtonGameObject.GetComponentInChildren<Text>();
        circle = circleGameObject.GetComponent<Image>();

        // Assign the duration of a recording (used for the circle bar).
        float delayOfRecordingInSeconds = MicrophoneCapture.LENGTH_OF_DELAY_IN_SAMPLES / recordedLoops.sampleRate;
        maxTime = recordedLoops.secondsDurationRecording + delayOfRecordingInSeconds;
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
            //recordButton.SetActive(false);
            //addButton.SetActive(true);
        }

        // If every button is activated remove the add button.
        int size = allButtons.Count;
        if (i == (size - 1))
        {
            //addButton.SetActive(false);
            recordButtonGameObject.SetActive(false);
            allButtonsActivated = true;
        }

        indexOfCurrentMicButton = i; // Save the index of the current mic button that was added.
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
        recordButtonText.text = "SPELA IN";
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
