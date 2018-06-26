using UnityEngine;

/// <summary>
/// Determines whether a button should display a sprite for play, 
/// or a sprite for stop. The sprites for play and stop can be
/// retrieved from this class. Sprites are assigned in the inspector.
/// </summary>

namespace Musikverkstaden
{
    [CreateAssetMenu]
    public class PlayOrStopSprite : ScriptableObject
    {
        // Assign the sprites in the inspector.
        public Sprite greenPlaySprite;
        public Sprite redStopSprite;

        public System.Collections.Generic.List<Sprite> playSprites;
        public System.Collections.Generic.List<Sprite> stopSprites;

        // Has the same size as the number of buttons.
        // Used to determine which sprite to show.
        public bool[] showPlaySprite;

        // Return the play sprite.
        public Sprite GetPlaySprite(int index)
        {
            return playSprites[index];
        }

        // Return the stop sprite.
        public Sprite GetStopSprite(int index)
        {
            return stopSprites[index];
        }

        // Returns a bool that describes whether a sprite for play or stop should be displayed.
        public bool ShouldButtonShowPlaySprite(int indexOfButton)
        {
            return showPlaySprite[indexOfButton];
        }

        // Updates the status of the button.
        public void SetIfButtonShouldShowPlaySprite(int indexOfButton, bool temp)
        {
            showPlaySprite[indexOfButton] = temp;
        }
    }
}
