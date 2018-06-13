using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GateData : ScriptableObject {

    public float[] gate1;
    public float[] gate2;
    public float[] gate3;

    public void AssignGate(int index, float[] indata)
    {
        switch (index)
        {
            case 0:
                gate1 = indata;
                break;
            case 1:
                gate2 = indata;
                break;
            case 2:
                gate3 = indata;
                break;
            default:
                break;
        }        
    }
}
