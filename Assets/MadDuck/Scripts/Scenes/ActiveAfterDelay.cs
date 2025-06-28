using UnityEngine;
using System.Collections;

public class ActivateAfterDelay : MonoBehaviour
{
    public GameObject target;
    public float delayTime = 3f;

    void OnEnable()
    {
        StartCoroutine(ActivateTargetAfterDelay());
    }

    IEnumerator ActivateTargetAfterDelay()
    {
        yield return new WaitForSeconds(delayTime);
        
            target.SetActive(true);
            
    }
}