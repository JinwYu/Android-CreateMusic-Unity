using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu] // Allows to create the object in the project view.
public class PhaseInversion : ScriptableObject
{
    private PlayMultipleAudioSources playMultipleAudioSources;
    private int presetLoop1StartIndex;
    private int presetLoop2StartIndex;
    private float[] presetLoop1;
    private float[] presetLoop2;

    public void Init()
    {
        presetLoop1StartIndex = playMultipleAudioSources.GetCurrentTimeInSamplesForPresetLoops(0);
        presetLoop2StartIndex = playMultipleAudioSources.GetCurrentTimeInSamplesForPresetLoops(1);
    }

    // För att aligna dom, så ska jag börja från filteredPresetLoop[presetLoop1StartIndex].

    void Start ()
    {
        // Hämta preset looparna, trumloopen och hihatsen

        // Högpassfiltrera med en hög cutofffrekvens

        // Sänka intensiteten på den väldigt mycket

        // Phase inversion genom punktvis multiplicera "-1" med hela trumloopen och hihatloopen

        // Summera med själva inspelningen

        // Frågor: Kommer det bli problem att timingen inte alltid är samma
        //         på inspelningen eftersom det är användaren själv som bestämmer
        //         själv när den vill spela in, och därför är inte alltid timingen
        //         samma. Vilket jag hade antagit.

        // På något sätt måste jag matcha ljuden så de har exakt samma timing

        // Använda sig utav de högpassfiltrerade looparna
        // Högpassfiltrera inspelningen exakt lika mycket
        // Jämföra dessa med den inspelade loopen
        // Se ifall värdena är inom ett godtyckligt intervall i närheten av värdena från högpasfiltrerade

        // Hålla reda på vilket index/sampel de värdena är på
        // Kan man gå frame by frame, typ använda sig av en frame från trumloopen som går över hela inspelningen
        // Den hittar sen en matchning där typ en mega ifsats där ca 12000 samples ska kollas (för att identifiera mönstret)
        // Eller låta framen gå igenom inspelningen, och ha en ifsats som jämför ifall värdena från inspelningen är inom
        // ett godtyckligt intervall och ha en counter på hur många i rad som är inom ett godtyckligt intervall
        // Om det värdet på counter går över ett threshold, så hitta indexet som den startar på och gör en phase inversion därifrån

        // Kan jag extract vilket skede som loopen befinner sig i när inspelningen sker?

        /*
         *  
         * 
         * 
         * 
         * */



    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
