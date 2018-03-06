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
