using UnityEngine;

/// <summary>
/// Stores the preset loops.
/// </summary>
[CreateAssetMenu]
public class PresetLoops : ScriptableObject {

    // Assign in inspector.
    public AudioClip[] originalPresetLoops = new AudioClip[ApplicationProperties.NUM_PRESET_LOOPS]; 
}
