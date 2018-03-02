using UnityEngine;

[CreateAssetMenu]
public class PlayOrStopSprite : ScriptableObject
{
    public bool[] showPlaySprite;

    public bool ShouldButtonShowPlaySprite(int indexOfButton)
    {
        return showPlaySprite[indexOfButton];
    }

    public void SetIfButtonShouldShowPlaySprite(int indexOfButton, bool temp)
    {
        showPlaySprite[indexOfButton] = temp;
    }
}
