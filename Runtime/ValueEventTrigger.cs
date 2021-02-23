using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using QTool;

public class EventTriggerBase<T>: IKey<string> where T: UnityEventBase
{
    public string EventName;
    public string Key { get => EventName; set => EventName = value; }
    public T eventAction=default;
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
public class ObjectEvent : UnityEvent<object>
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

//[System.Serializable]
//public class SpriteEventTrigger : EventTriggerBase<SpriteEvent>
//{
//}
[System.Serializable]
public class ObjectEventTrigger : EventTriggerBase<ObjectEvent>
{
}
[System.Serializable]
public class GameObjectKeyValue : IKey<string>
{
    public string name;
    public string Key { get => name; set => name = value; }
    public GameObject gameObject;
}
[System.Serializable]
public class ValueEventTrigger : MonoBehaviour
{
    public List<ActionEventTrigger> actionEventList=new List<ActionEventTrigger>();
    public List<StringEventTrigger> stringEventList=new List<StringEventTrigger>();
  //  public List<SpriteEventTrigger> spriteEventList=new List<SpriteEventTrigger>();
    public List<BoolEventTrigger> boolEventList=new List<BoolEventTrigger>();
    public List<FloatEventTrigger> floatEventList=new List<FloatEventTrigger>();
    public List<ObjectEventTrigger> objectEventList=new List<ObjectEventTrigger>();
    public List<GameObjectKeyValue> objectList = new List<GameObjectKeyValue>();
  
    
    public GameObject GetObject(string name)
    {
        var kv = objectList.Get(name);
        return kv == null ? null : kv.gameObject;
    }
    public void Invoke<T>(string eventName, T value) where T: class
    {
        var type = typeof(T);
        if (type == typeof(string))
        {
            stringEventList.Get(eventName)?.eventAction?.Invoke(value as string);
        }
        //else if(type == typeof(Sprite))
        //{
        //    spriteEventList.Get(eventName)?.eventAction?.Invoke(value as Sprite);
        //}
        else
        {
            objectEventList.Get(eventName)?.eventAction?.Invoke(value);
        }

    }
    public void Invoke(string eventName)
    {
        actionEventList.Get(eventName)?.eventAction.Invoke();
    }
    public void Invoke(string eventName, bool value)
    {
        boolEventList.Get(eventName)?.eventAction?.Invoke((bool)value);
    }
    public new void Invoke(string eventName, float value)
    {
        floatEventList.Get(eventName)?.eventAction?.Invoke(value);
    }
}
public static class ValueEventTriggerExtends
{

    public static ValueEventTrigger GetTrigger(this GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }
        var tigger= obj.GetComponentInChildren<ValueEventTrigger>(true);
        return tigger;
    }

    public static ValueEventTrigger GetParentTrigger(this GameObject obj)
    {
        if (obj.transform.parent == null||obj==null)
        {
            return null;
        }
        var tigger = obj.transform.parent.GetComponentInParent<ValueEventTrigger>();
        return tigger;
    }
    public static GameObject GetObject(this GameObject obj, string objName) {
        var trigger = obj.GetTrigger();
        if (trigger == null)
        {
            return null;
        }
        else
        {
            return trigger.GetObject(objName);
        }
    }
    public static void InvokeEvent(this GameObject obj, string eventName) 
    {
        obj.GetTrigger()?.Invoke(eventName.Trim());
    }
    public static void InvokeEvent<T>(this GameObject obj, string eventName,T value) where T:class
    {
        obj.GetTrigger()?.Invoke(eventName.Trim(), value);
    }
    public static void InvokeEvent(this GameObject obj, string eventName, bool value) 
    {
        obj.GetTrigger()?.Invoke(eventName.Trim(), value);
    }
    public static void InvokeEvent(this GameObject obj, string eventName, float value)
    {
        obj.GetTrigger()?.Invoke(eventName.Trim(), value);
    }
  
    public static void InvokeParentEvent(this GameObject obj, string eventName)
    {
        obj.GetParentTrigger()?.Invoke(eventName.Trim());
    }
    public static void InvokeParentEvent<T>(this GameObject obj, string eventName, T value) where T : class
    {
        obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
    }
    public static void InvokeParentEvent(this GameObject obj, string eventName, bool value)
    {
        obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
    }
    public static void InvokeParentEvent(this GameObject obj, string eventName, float value)
    {
        obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
    }
}
