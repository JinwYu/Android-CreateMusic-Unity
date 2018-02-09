using UnityEngine;
using System.Collections;

public class SpawnByLoudness : MonoBehaviour
{

    public GameObject audioInputObject;
    public float threshold = 1.0f;
    public GameObject objectToSpawn;
    MicrophoneInput micIn;
    void Start()
    {
        if (objectToSpawn == null)
            Debug.LogError("You need to set a prefab to Object To Spawn -parameter in the editor!");
        if (audioInputObject == null)
            audioInputObject = GameObject.Find("AudioInputObject");
        micIn = (MicrophoneInput)audioInputObject.GetComponent("MicrophoneInput");
    }

    void Update()
    {
        float l = micIn.loudness;
        if (l > threshold)
        {
            Vector3 scale = new Vector3(l, l, l);
            GameObject newObject = (GameObject)Instantiate(objectToSpawn, this.transform.position, Quaternion.identity);
            newObject.transform.localScale += scale;
        }
    }
}