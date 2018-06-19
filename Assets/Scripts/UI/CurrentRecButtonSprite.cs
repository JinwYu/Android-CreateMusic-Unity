using UnityEngine;

/// <summary>
/// Keeps track of the current sprite for the record button.
/// The variable "currentSprite" has the current sprite of the record button.
/// </summary>
[CreateAssetMenu]
public class CurrentRecButtonSprite : ScriptableObject
{
    // Assign in the inspector.
    public Sprite greenPlaySprite;
    public Sprite startRecordingSprite;
    public Sprite recInProgSprite;
    public Sprite recInProgSprite2;

    private Sprite currentSprite;
    private bool recordingInProgress = false;    
    
    public void SetToPlaySprite()
    {
        currentSprite = greenPlaySprite;
    }

    public void SetToStartRecSprite()
    {
        currentSprite = startRecordingSprite;
    }

    public void SetToRecInProgSprite1()
    {
        currentSprite = recInProgSprite;
    }

    public void SetToRecInProgSprite2()
    {
        currentSprite = recInProgSprite2;
    }

    public Sprite GetCurrentSprite()
    {
        return currentSprite;
    }

    public void UpdateRecordingStatus(bool status)
    {
        recordingInProgress = status;
    }

    public bool GetRecordingStatus()
    {
        return recordingInProgress;
    }


}
