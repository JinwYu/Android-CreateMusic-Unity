using UnityEngine;
using System.Collections;

public class YieldingBehaviour : MonoBehaviour
{
    void Start()
    {
        // Begin our heavy work in a coroutine.
        StartCoroutine(YieldingWork());
    }

    IEnumerator YieldingWork()
    {
        bool workDone = false;

        while (!workDone)
        {
            // Let the engine run for a frame.
            yield return null;

            // Do Work...
        }
    }
}