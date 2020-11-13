using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using QTool;

public class EventTriggerBase<T>: IKey<string> where T: UnityEventBase
{
    public string EventName;
    public string Key { get => EventName; set => EventName = value; }
    public T eventAction;
}
[System.Serializable]
public class ActionEvent :UnityEvent
{
}
[System.Serializable]
public class BoolEvent : UnityEvent<bool>
{
}
[System.Serializable]
public class IntEvent : UnityEvent<int>
{
}
[System.Serializable]
public class FloatEvent : UnityEvent<float>
{
}
[System.Serializable]
public class StringEvent : UnityEvent<string>
{
}
[System.Serializable]
public class SpriteEvent : UnityEvent<Sprite>
{
}

[System.Serializable]
public class FloatEventTrigger : EventTriggerBase<FloatEvent>
{
}
[System.Serializable]
public class BoolEventTrigger : EventTriggerBase<BoolEvent>
{
}
[System.Serializable]
public class ActionEventTrigger : EventTriggerBase<UnityEvent>
{
}
[System.Serializable]
public class StringEventTrigger :EventTriggerBase<StringEvent>
{
}

[System.Serializable]
public class SpriteEventTrigger : EventTriggerBase<SpriteEvent>
{
}

public class ValueEventTrigger : MonoBehaviour
{
    public List<ActionEventTrigger> actionEventList;
    public List<StringEventTrigger> stringEventList;
    public List<SpriteEventTrigger> spriteEventList;
    public List<BoolEventTrigger> boolEventList;
    public List<FloatEventTrigger> floatEventList;
    public List<ValueEventTrigger> childTrigger;
    public void Set<T>(string eventName, T value) where T:class
    {
        var type = typeof(T);
        if (type == typeof(string))
        {
            stringEventList.Get(eventName)?.eventAction?.Invoke(value as string);
        }
        else if(type == typeof(Sprite))
        {
            spriteEventList.Get(eventName)?.eventAction?.Invoke(value as Sprite);
        }
        foreach (var trigger in childTrigger)
        {
            trigger.Set(eventName, value);
        }
    }
    public void Action(string eventName)
    {
        actionEventList.Get(eventName)?.eventAction.Invoke();
        foreach (var trigger in childTrigger)
        {
            trigger.Action(eventName);
        }
    }
    public void Set(string eventName, bool value)
    {
        boolEventList.Get(eventName)?.eventAction?.Invoke((bool)value);
        foreach (var trigger in childTrigger)
        {
            trigger.Set(eventName, value);
        }
    }
    public void Set(string eventName, float value)
    {
        floatEventList.Get(eventName)?.eventAction?.Invoke(value);
        foreach (var trigger in childTrigger)
        {
            trigger.Set(eventName, value);
        }
    }
}
public static class ValueEventTriggerExtends
{
    public static void InvokeEvent(this GameObject obj, string eventName) 
    {
        obj.GetTrigger()?.Action(eventName.Trim());
    }
    public static void InvokeEvent<T>(this GameObject obj, string eventName,T value=null) where T:class
    {
        obj.GetTrigger()?.Set(eventName.Trim(), value);
    }
    public static void InvokeEvent(this GameObject obj, string eventName, bool value) 
    {
        obj.GetTrigger()?.Set(eventName.Trim(), value);
    }
    public static void InvokeEvent(this GameObject obj, string eventName, float value)
    {
        obj.GetTrigger()?.Set(eventName.Trim(), value);
    }
    public static ValueEventTrigger GetTrigger(this GameObject obj)
    {
        return obj.GetComponentInChildren<ValueEventTrigger>();
    }
}
