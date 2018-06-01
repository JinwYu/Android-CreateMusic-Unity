using UnityEngine;

[CreateAssetMenu]
public class PlayOrStopSprite : ScriptableObject
{
    public Sprite greenPlaySprite;
    public Sprite redStopSprite;
    public bool[] showPlaySprite;

    //private void Awake()
    //{
    //    for(int i = 0; i < showPlaySprite.Length; i++)
    //    {
    //        showPlaySprite[i] = false;
    //    }
    //}

    public Sprite GetPlaySprite()
    {
        return greenPlaySprite;
    }

    public Sprite GetStopSprite()
    {
        return redStopSprite;
    }

    public bool ShouldButtonShowPlaySprite(int indexOfButton)
    {
        return showPlaySprite[indexOfButton];
    }

    public void SetIfButtonShouldShowPlaySprite(int indexOfButton, bool temp)
    {
        showPlaySprite[indexOfButton] = temp;
    }
}
