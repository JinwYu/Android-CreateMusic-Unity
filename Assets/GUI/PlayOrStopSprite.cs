using UnityEngine;

[CreateAssetMenu]
public class PlayOrStopSprite : ScriptableObject
{
    public Sprite greenPlaySprite;
    public Sprite redStopSprite;
    public bool[] showPlaySprite;

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
