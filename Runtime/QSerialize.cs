using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;
public static class QSerialize
{
    public static string SerializeXml<T>(T t, params Type[] extraTypes)
    {
        using (StringWriter sw = new StringWriter())
        {
            if (t == null)
            {
                Debug.LogError("序列化数据为空" + typeof(T));
                return null;
            }
            XmlSerializer xz = new XmlSerializer(t.GetType(), extraTypes);
            xz.Serialize(sw, t);
            return sw.ToString();
        }
    }
    public static T DeserializeXml<T>(string s, params Type[] extraTypes)
    {
        using (StringReader sr = new StringReader(s))
        {
            XmlSerializer xz = new XmlSerializer(typeof(T), extraTypes);
            return (T)xz.Deserialize(sr);
        }
    }
    public static string SerializeJson<T>(T t)
    {
        return JsonConvert.SerializeObject(t);
    }
    public static T DeserializeJson<T>(string s)
    {
        return JsonConvert.DeserializeObject<T>(s);
    }
}
