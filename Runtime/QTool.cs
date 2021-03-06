using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace QTool
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class FileDialog
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public String filter = null;
        public String customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public String file = null;
        public int maxFile = 0;
        public String fileTitle = null;
        public int maxFileTitle = 0;
        public String initialDir = null;
        public String title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public String defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public String templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
        public FileDialog()
        {
            structSize = Marshal.SizeOf(this);
            file = new string(new char[256]);
            maxFile = file.Length;
            fileTitle = new string(new char[64]);
            maxFileTitle = fileTitle.Length;
            flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
        }
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] FileDialog ofd);
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] FileDialog ofd);
    }
    public interface IKey<KeyType>
    {
        KeyType Key { get; set; }
    }
    [System.Serializable]
    public class QKeyValue<TKey, T> : IKey<TKey>
    {
        public TKey Key { get;set; }
        public T Value { get;set; }
        public QKeyValue()
        {
           
        }
        public QKeyValue(TKey key,T value)
        {
            Key = key;
            Value = value;
        }
        public override string ToString()
        {
            return "{" + Key + ":" + Value + "}";
        }
    }
    public class QDcitionary<TKey, T> : QAutoList<TKey, QKeyValue<TKey, T>>
    {
        public new T this[TKey key]
        {
            get
            {
                var keyValue= base[key];
                return keyValue.Value;
            }
            set
            {
                base[key].Value=value;
            }
        }
    }
    public class QList<TKey,T>:List<T> where T : IKey<TKey>
    {
        Dictionary<TKey, T> dic = new Dictionary<TKey, T>();
        public virtual T Get(TKey key)
        {
            return this.Get<T, TKey>(key); 
        }
        public T this[TKey key]
        {
            get
            {
                if (dic.ContainsKey(key))
                {
                    return dic[key];
                }
                else
                {
                    var value = Get(key);
                    dic.Add(key, value);
                    return value;
                }
            }
            set
            {
                this.RemoveKey(key);
                if (dic.ContainsKey(key))
                {
                    dic[key] = value;
                }
                this.Set(key, value);
            }
        }
        public new void Remove(T obj)
        {
            base.Remove(obj);
            dic.Remove(obj.Key);
        }
        public void RemoveKey(TKey key)
        {
            Remove(this[key]);
        }
        public new void Clear()
        {
            base.Clear();
            dic.Clear();
        }
    }
    public class QAutoList<KeyType, T> : QList<KeyType,T> where T :IKey<KeyType>, new()
    {
        public override T Get(KeyType key)
        {
            return this.GetAndCreate<T, KeyType>(key, creatCallback);
        }
        public event System.Action<T> creatCallback;
    }
    public class WaitTime
    {
        public float Time { get; protected set; }
        public float CurTime { get; protected set; }

        public void Clear()
        {
            CurTime = 0;
        }
        public void Over()
        {
            CurTime = Time;
        }
        public void Reset(float time,bool startOver=false)
        {
            this.Time = time;
            CurTime = 0;
            if (startOver) Over();
        }
        public WaitTime(float time, bool startOver = false)
        {
            Reset(time, startOver);
        }

        public bool Check(float deltaTime, bool autoClear = true)
        {
            CurTime += deltaTime;
            var subTime = CurTime - Time;
            if (subTime >= 0)
            {
                if (autoClear) { CurTime = subTime; }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public static class Tool
    {
        public static void RunTimeCheck(string name, System.Action action, Func<int> getLength = null)
        {
            var last = System.DateTime.Now;
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("【" + name + "】运行出错:"+e);
                return;
            }
            Debug.LogError("【" + name + "】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds + (getLength == null ? "" : " 长度" + getLength().ComputeScale()));
        }
    }
    public static class ArrayExtend
    {
        public static string ComputeScale(this string array)
        {
            return array.Length.ComputeScale();
        }
        public static string ComputeScale(this IList array)
        {
            return array.Count.ComputeScale();
        }

        public static string ComputeScale(this int byteLength)
        {
            return ComputeScale((long)byteLength);
        }

        public static string ComputeScale(this long longLength)
        {
            string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = longLength;
            if (longLength > 1024)
                for (i = 0; (longLength / 1024) > 0; i++, longLength /= 1024)
                    dblSByte = longLength / 1024.0;
            return String.Format("{0:0.##}{1}", dblSByte, Suffix[i]);
        }
        public static string ToOneString<T>(this ICollection<T> array,string splitChar="\n")
        {
            var str = "";
            if (array == null)
            {
                return str;
            }
            int i=0;
            foreach (var item in array)
            {
                str += item +(i!=array.Count-1? splitChar:"");
                i++;
            }
            return str;
        }
        public static bool ContainsKey<T, KeyType>(this ICollection<T> array, KeyType key) where T :  IKey<KeyType>
        {
            return array.ContainsKey(key, (item) =>item.Key);
        }
        public static bool ContainsKey<T, KeyType>(this ICollection<T> array, KeyType key, Func<T, KeyType> keyGetter) 
        {
            if (key==null)
            {
                return false;
            }
            foreach (var value in array)
            {
                if (key.Equals(keyGetter(value)))
                {
                    return true;
                }
            }
            return false;
        }
        public static T Get<T, KeyType>(this ICollection<T> array, KeyType key) where T : IKey<KeyType>
        {
            return array.Get(key,(item)=>item.Key);
        }
        public static T Get<T, KeyType>(this ICollection<T> array, KeyType key,Func<T,KeyType> keyGetter) 
        {
            if (key == null)
            {
                return default;
            }
            foreach (var value in array)
            {
                if (value == null) continue;
                if (key.Equals(keyGetter(value)))
                {
                    return value;
                }
            }
            return default;
        }
        public static List<T> GetList<T, KeyType>(this ICollection<T> array, KeyType key, List<T> tempList = null) where T :IKey<KeyType>
        {
            var list = tempList == null ? new List<T>() : tempList;
            foreach (var value in array)
            {
                if (key.Equals(value.Key))
                {
                    list.Add(value);
                }
            }
            return list;
        }
        public static T StackPeek<T>(this IList<T> array) 
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            return array[array.Count - 1];
        }
        public static T QueuePeek<T>(this IList<T> array) 
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            return array[0];
        }
        public static void Enqueue<T>(this IList<T> array, T obj) 
        {
            array.Add(obj);
        }
        public static void Push<T>(this IList<T> array, T obj) 
        {
            array.Add(obj);
        }
        public static T Pop<T>(this IList<T> array)
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            var obj = array.StackPeek();
            array.RemoveAt(array.Count - 1);
            return obj;
        }
        public static T Dequeue<T>(this IList<T> array)
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            var obj = array.QueuePeek();
            array.RemoveAt(0);
            return obj;
        }
        public static void AddCheckExist<T>(this IList<T> array, params T[] objs)
        {
            foreach (var obj in objs)
            {
                if (!array.Contains(obj))
                {
                    array.Add(obj);
                }
            }
        }
      
        public static void RemoveKey<T, KeyType>(this ICollection<T> array, KeyType key) where T : IKey<KeyType>
        {
            var old = array.Get(key);
            if (old != null)
            {
                array.Remove(old);
            }
        }
        public static void Set<T, KeyType>(this ICollection<T> array, KeyType key, T value) where T : IKey<KeyType>
        {
            array.RemoveKey(key);
            value.Key = key;
            array.Add(value);
        }

        public static T GetAndCreate<T, KeyType>(this ICollection<T> array, KeyType key, System.Action<T> creatCallback = null) where T :IKey<KeyType>, new()
        {
           var value= array.Get(key);
            if (value != null)
            {
                return value;
            }
            else
            {
                var t = new T { Key = key };
                creatCallback?.Invoke(t);
                array.Add(t);
                return t;
            }
        }
    }
    public class FileManager
    {
        public static T Copy<T>(T target)
        {
            return Deserialize<T>(Serialize(target));
        }
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
                XmlSerializer xz = GetSerializer(typeof(T), extraTypes);
                return (T)xz.Deserialize(sr);
            }
        }
        public static bool ExistsFile(string path)
        {
            return File.Exists(path);
        }
        public static string Load(string path)
        {
            string data = "";
            if (!ExistsFile(path))
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
            CheckFolder(path);
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
        public static void CheckFolder(string path)
        {
            var directoryPath = Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        public static string Save(string path, string data)
        {
            try
            {
                CheckFolder(path);
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
        public static string SelectOpenPath(string title = "打开文件", string extension = "obj", string directory = "Assets")
        {
            var dialog = new FileDialog
            {
                title = title,
                initialDir = directory,
                // defExt = extension,
                filter = "(." + extension + ")\0*." + extension + "\0",
            };
            if (FileDialog.GetOpenFileName(dialog))
            {
                return dialog.file;
            }
            return "";
        }
        public static string SelectSavePath(string title = "保存文件", string extension = "obj", string directory = "Assets")
        {
            var dialog = new FileDialog
            {
                title = title,
                initialDir = directory,
                //  defExt = extension,
                filter = "(." + extension + ")\0*." + extension + "\0",
            };
            if (FileDialog.GetSaveFileName(dialog))
            {
                return dialog.file;
            }
            return "";
        }
        //#if UNITY_EDITOR
        //        public static string SaveSelectPath(string data, string title = "保存", string name = "temp", string extension = "obj", string directory = "Assets")
        //        {
        //            var path = UnityEditor.EditorUtility.SaveFilePanel(
        //                title,
        //                directory,
        //                name,
        //                extension
        //                );
        //            if (path != string.Empty)
        //            {
        //                Save(path, data);
        //            }
        //            return path;
        //        }
        //        public static string LoadSelectPath(string title = "读取", string extension = "obj", string directory = "Assets")
        //        {
        //            var path = UnityEditor.EditorUtility.OpenFilePanel(
        //                title,
        //                directory,
        //                extension
        //                );
        //            if (System.IO.File.Exists(path))
        //            {
        //                return Load(path);
        //            }
        //            else
        //            {
        //                return "";
        //            }

        //        }
        //#endif
    }

}