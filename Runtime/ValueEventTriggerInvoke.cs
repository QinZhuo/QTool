using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValueEventTriggerInvoke : MonoBehaviour
{

    public string key;
    public void InokeEvent(string value)
    {
        gameObject.InvokeEvent(key, value);
    }
    public void InokeEvent(float value)
    {
        gameObject.InvokeEvent(key, value);
    }
    public void InokeParentEvent(string value)
    {
        gameObject.InvokeParentEvent(key, value);
    }
    public void InokeParentEvent(float value)
    {
        gameObject.InvokeParentEvent(key, value);
    }
}
