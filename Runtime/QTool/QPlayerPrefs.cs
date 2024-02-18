using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public static class QPlayerPrefs
	{
		private static QDataList Data { get; set; } = QDataList.Load(QFileTool.SaveDataPathRoot + "/" + nameof(QPlayerPrefs) + QFileTool.DataExtension, () => new QDataList());
#if UNITY_EDITOR
		static QPlayerPrefs()
		{
			QDebug.Log("读取预设\n" + Data.ToString());
		}
#endif
		private static void Save()
		{
			Data.Save();
		}
		public static bool HasKey(string key)
		{
			return Data.ContainsKey(key);
		}
		public static void DeleteKey(string key)
		{  
			Data.RemoveKey(key);
			Save();
		}
		public static void DeleteAll()
		{
			Data.Clear();
			Save();
		}
	
		public static void Set<T>(string key,T value)
		{
			if (key.IsNull())
			{
				Debug.LogError("key 为空");
				return;
			}
			Data[key].SetValue(value);
			Save();
		}
		public static void SetInt(string key, int value)
		{
			Set(key, value);
		}
		public static void SetFloat(string key, float value)
		{
			Set(key, value);
		}
		public static void SetString(string key, string value)
		{
			Set(key, value);
		}

		public static T Get<T>(string key, T defaultValue=default)
		{
			if (key.IsNull())
			{
				Debug.LogError("key 为空");
				return defaultValue;
			}
			if (!HasKey(key))
			{
				return defaultValue;
			}
			else
			{
				var data= Data[key].GetValue(1,defaultValue);
				if (data.IsNull())
				{
					return defaultValue;
				}
				return data;
			}
		}
		public static int GetInt(string key, int defaultValue = 0)
		{
			return Get(key, defaultValue);
		}
		public static float GetFloat(string key, float defaultValue = 0)
		{
			return Get(key, defaultValue);
		}
		public static string GetString(string key,string defaultValue="")
		{
			return Get(key, defaultValue);
		}
	}
}

