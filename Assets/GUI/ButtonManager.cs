using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour {

    public GameObject addButton;
    public List<GameObject> buttonsToBeAdded; // Drag and drop the buttons, that will be shown when the add button is pressed, to this list in the inspector. 
    private bool allButtonsActivated = false;
    

    public void ShowAddedButton()
    {
        addButton.transform.SetAsLastSibling(); // Since the add button has been clicked on, move it to the end of the gridlayout.

        // Find the first inactive button.
        int i = 0;
        int size = buttonsToBeAdded.Count;
        while(!allButtonsActivated && i < size)
        {
            if (!buttonsToBeAdded[i].activeSelf)
                break;

            i++;
        }

        if (!allButtonsActivated)
            buttonsToBeAdded[i].SetActive(true); // Activate the first inactive button.

        // If all every button is activated remove the add button.
        if (i == size - 1)
        {
            addButton.SetActive(false);
            allButtonsActivated = true;
        }            
    }

    void Start()
    {
        
    }

    // Funktion för att spela drumljudet, eventuellt ändra färg till röd

    // Funktion för att spela hihats, eventuellt ändra färg till röd

    // Funktion för att lägga till en ny knapp innan addknappen

}
