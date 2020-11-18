using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.IO;
namespace QTool
{
    public interface IKey<KeyType>
    {
        KeyType Key { get; set; }
    }
    public class KeyList<KeyType, T> : List<T> where T : class, IKey<KeyType>, new()
    {
        Dictionary<KeyType, T> dic = new Dictionary<KeyType, T>();
        public T this[KeyType key]
        {
            get
            {
                if (dic.ContainsKey(key))
                {
                    return dic[key];
                }
                else
                {
                    var value = this.GetAndCreate<T, KeyType>(key); ;
                    dic.Add(key, value);
                    return value;
                }
            }
            set
            {
                var old = this[key];
                Remove(old);
                if (dic.ContainsKey(key))
                {
                    dic[key] = value;
                }
                if (!key.Equals(value.Key))
                {
                    value.Key = key;
                }
                Add(value);
            }
        }
    }
    public class StringKeyList<T> : KeyList<string, T> where T: class,IKey<string>,new()
    {
    }
    
    public static class ArrayExtend
    {
        public static bool ContainsKey<T, KeyType>(this ICollection<T> array, KeyType key) where T : class, IKey<KeyType>
        {
            foreach (var value in array)
            {
                if (key.Equals(value.Key))
                {
                    return true;
                }
            }
            return false;
        }
        public static T Get<T, KeyType>(this ICollection<T> array, KeyType key) where T : class, IKey<KeyType>
        {
            foreach (var value in array)
            {
                if (key.Equals(value.Key))
                {
                    return value;
                }
            }
            return null;
        }
        public static T Peek<T>(this IList<T> array) where T : class
        {
            return array[array.Count - 1];
        }
        public static void Push<T>(this IList<T> array, T obj) where T : class
        {
            array.Add(obj);
        }
        public static void AddCheckExist<T>(this IList<T> array,params T[] objs) where T : class
        {
            foreach (var obj in objs)
            {
                if (!array.Contains(obj))
                {
                    array.Add(obj);
                }
            }
        }
        public static T Pop<T>(this IList<T> array) where T : class
        {
            var obj = array.Peek();
            array.RemoveAt(array.Count - 1);
            return obj;
        }
        public static void Set<T, KeyType>(this ICollection<T> array, KeyType key, T value) where T : class, IKey<KeyType>
        {
            var old = array.Get(key);
            if (old != null)
            {
                array.Remove(old);
            }
            value.Key = key;
            array.Add(value);
        }
        public static T GetAndCreate<T, KeyType>(this ICollection<T> array, KeyType key) where T : class, IKey<KeyType>, new()
        {
            foreach (var value in array)
            {
                if (value == null) continue;
                    if (key.Equals(value.Key))
                    {
                        return value;
                    }
             
              
            }
            var t = new T { Key = key };
            array.Add(t);
            return t;
        }
    }
    public class FileManager
    {
        public static Dictionary<string, XmlSerializer> xmlSerDic = new Dictionary<string, XmlSerializer>();
        public static XmlSerializer GetSerializer(Type type, params Type[] extraTypes)
        {
            var key = type.FullName;
            foreach (var item in extraTypes)
            {
                key += " " + item.FullName;
            }
            if (xmlSerDic.ContainsKey(key))
            {
                return xmlSerDic[key];
            }
            else
            {
                XmlSerializer xz = new XmlSerializer(type, extraTypes);
                xmlSerDic.Add(key, xz);
                return xz;
            }
        }
        public static string Serialize<T>(T t, params Type[] extraTypes)
        {
            using (StringWriter sw = new StringWriter())
            {
                if (t == null)
                {
                    Debug.LogError("序列化数据为空" + typeof(T));
                    return null;
                }
                GetSerializer(typeof(T), extraTypes).Serialize(sw, t);
                return sw.ToString();
            }
        }
        public static T Deserialize<T>(string s, params Type[] extraTypes)
        {
            using (StringReader sr = new StringReader(s))
            {
                XmlSerializer xz = GetSerializer(typeof(T),extraTypes);
                return (T)xz.Deserialize(sr);
            }
        }
        public static string Load(string path)
        {
            string data = "";
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError("不存在文件：" + path);
                return data;
            }
            using (var file = System.IO.File.Open(path, System.IO.FileMode.Open))
            {
                using (var sw = new System.IO.StreamReader(file))
                {
                    while (!sw.EndOfStream)
                    {

                        data += sw.ReadLine();
                    }
                }
            }
            return data;
        }
        public static byte[] LoadBytes(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError("不存在文件：" + path);
                return null;
            }
            return File.ReadAllBytes(path);
        }
        public static void Save(string path, byte[] bytes)
        {
            var directoryPath = Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllBytes(path, bytes);
        }
        public static void SavePng(Texture2D tex, string path)
        {
            var bytes = tex.EncodeToPNG();
            if (bytes != null)
            {
                File.WriteAllBytes(path, bytes);
            }
        }
        public static Texture2D LoadPng(string path, int width = 128, int height = 128)
        {
            var bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(width, height);
            tex.LoadImage(bytes);
            return tex;
        }
     
        public static string Save(string path, string data)
        {
            try
            {
                using (var file = System.IO.File.Create(path))
                {
                    using (var sw = new System.IO.StreamWriter(file))
                    {
                        sw.Write(data);
                    }
                }
            }
            catch (Exception e)
            {

                Debug.LogError("保存失败【" + path + "】" + e);
            }

            return path;
        }
#if UNITY_EDITOR
        public static string SaveSelectPath(string data, string title = "保存", string name = "temp", string extension = "obj", string directory = "Assets")
        {
            var path = UnityEditor.EditorUtility.SaveFilePanel(
                title,
                directory,
                name,
                extension
                );
            if (path != string.Empty)
            {
                Save(path, data);
            }
            return path;
        }
        public static string LoadSelectPath(string title = "读取", string extension = "obj", string directory = "Assets")
        {
            var path = UnityEditor.EditorUtility.OpenFilePanel(
                title,
                directory,
                extension
                );
            if (System.IO.File.Exists(path))
            {
                return Load(path);
            }
            else
            {
                return "";
            }

        }
#endif
    }

}