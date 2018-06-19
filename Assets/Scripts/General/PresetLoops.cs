using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PresetLoops : ScriptableObject {

    public AudioClip[] originalPresetLoops = new AudioClip[ApplicationProperties.NUM_PRESET_LOOPS]; // Assign in inspector.
}
