using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CurrentRecButtonSprite : ScriptableObject
{
    public Sprite greenPlaySprite;
    //public Sprite redStopSprite;
    public Sprite startRecordingSprite;
    public Sprite recInProgSprite;

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

    public void SetToRecInProgSprite()
    {
        currentSprite = recInProgSprite;
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
