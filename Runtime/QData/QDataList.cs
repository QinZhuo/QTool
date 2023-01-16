using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QTool
{
	/// <summary>
	/// 表格式字符存储逻辑
	/// </summary>
    public class QDataList: QList<string, QDataRow>
	{
		public static string ResourcesPathRoot => QFileManager.ResourcesPathRoot+"/" + nameof(QDataList) +"Asset";
		public static string ModPath=>QFileManager.ModPathRoot + "/" + nameof(QDataList) + "Asset" ;
		public static string GetResourcesDataPath(string name,string child=null)
		{
			return ResourcesPathRoot.ChildPath(name).ChildPath(child)+".txt";
		}
		public static string GetModPath(string name, string child = null)
		{
			return ModPath.ChildPath(name).ChildPath(child) + ".txt";
		}
		public static string GetAssetDataPath(string name)
		{
			return Application.dataPath + "/" + nameof(QDataList) + "Asset/" + name + ".txt";
		}
		public static QDataList GetResourcesData(string name, System.Func<QDataList> autoCreate = null)
		{
			var dataList= GetData(GetResourcesDataPath(name),autoCreate);
			if (QToolSetting.Instance.modeList.Contains(name))
			{
				QFileManager.LoadAll(GetModPath(name), (fileValue, loadPath) =>
				{
					dataList.Add(new QDataList(fileValue) { LoadPath = loadPath });
				}, "{}");
			}
			return dataList;
		}
		public static QDataList GetData(string path,System.Func<QDataList> autoCreate=null)
        {
			QDataList data = null;
			;
			try
			{
				data = new QDataList();
				data.LoadPath = path;
				QFileManager.LoadAll(path, (fileValue, loadPath) =>
				{
					data.Add(new QDataList(fileValue) { LoadPath = loadPath });
				}, "{}");
			}
			catch (System.Exception e)
			{
				Debug.LogError("读取QDataList[" + path + "]出错：\n" + e);

			}
			if (data.Count == 0)
			{
				if (autoCreate != null)
				{
					data = autoCreate();
					data.LoadPath = path;
					data.Save();
					Debug.LogWarning("不存在QDataList自动创建[" + path + "]:\n" + data);
				}
			}
			return data;

		}
	
     
        public string LoadPath { get; private set; }
        public void Save(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = LoadPath;
			}
			QFileManager.Save(path, ToString());
        }
        public int GetTitleIndex(string title)
        {
            var index = TitleRow.IndexOf(title);
			if (index >= 0)
			{
				return index;
			}
			else
			{
				TitleRow.Add(title);
				return TitleRow.IndexOf(title);
			}
        }
        public QDataRow TitleRow
        {
            get
            {
                return this[0];
            }
        }
        public void SetTitles(params string[] titles)
        {
            for (int i = 0; i < titles.Length; i++)
            {
                TitleRow[i] = titles[i];
            }
        }
        public new QDataRow this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    var line=new QDataRow(this);
					var list = new List<float>();
                    base[index] = line;
                }
                return base[index];
            }
        }
       private void Parse(string dataStr)
        {
			Clear();
			using (var keyInfo = new StringWriter())
			{

				using (var reader = new StringReader(dataStr))
				{
					int rowIndex = 0;
					int valueIndex = 0;
					var row = new QDataRow(this);
					while (!reader.IsEnd())
					{
						var value = reader.ReadElement(out var newLine);
						row[valueIndex] = value;
						valueIndex++;
						if (newLine)
						{
							if (row.Count > 0)
							{
								if (!string.IsNullOrEmpty(row.Key))
								{
									if (ContainsKey(row.Key))
									{
										QDebug.LogWarning("加载覆盖 [" + row.Key + "] 来自文件 " + LoadPath + "\n旧数据: " + this[row.Key] + "\n新数据: " + row);
									}
									Add(row);
								}
								keyInfo.Write(row.Key);
								keyInfo.Write('\t');
							}
							valueIndex = 0;
							rowIndex++;
							row = new QDataRow(this);
						}

					}
				}
			}
		}
        public QDataList()
		{
			AutoCreate = () => new QDataRow(this);
		}
		public void Add(QDataList addList)
		{
			if (TitleRow.Count == 0)
			{
				TitleRow[0] = addList.TitleRow[0];
			}
			for (int i = 1; i < addList.Count; i++)
			{
				var row = addList[i];
				if (ContainsKey(row.Key))
				{
					QDebug.LogWarning("加载覆盖 [" + row.Key + "] 来自文件 " + addList.LoadPath +" 原文件 "+LoadPath+ "\n旧数据: " + this[row.Key] + "\n新数据: " + row);
				}
				var newRow = this[row.Key];
				for (int j = 1; j < addList.TitleRow.Count; j++)
				{
					var title = addList.TitleRow[j];
					newRow[title] = row[title];
				}
			}

		}
        public QDataList(string dataStr):this()
        {
            Parse(dataStr);
        }
        public override string ToString()
        {
            using (var writer =new StringWriter())
            {
                for (int i = 0; i < this.Count; i++)
                {
                    writer.Write(this[i].ToString());
                    if (i < Count - 1)
                    {
                        writer.Write('\n');
                    }
                }
                return writer.ToString();
            }
          
        }

    }
	/// <summary>
	/// 运行时静态数据表 从Resouces文件夹加载字符数据 通过静态函数访问 只读
	/// </summary>
	public class QDataList<T> where T : QDataList<T>, IKey<string>, new()
	{
		public static bool ContainsKey(string key)
		{
			return List.ContainsKey(key);
		}
		public static T Get(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				Debug.LogError("key 为空");
				return null;
			}
			key = key.Trim();
			var value = List[key]; ;
			if (value == null)
			{
				Debug.LogError(typeof(T).Name + " 未找到[" + key + "]");
			}
			return value;
		}
		static QList<string, T> _list = null;
		public static QList<string, T> List
		{
			get
			{
				if (_list == null)
				{
					var qdataList = QDataList.GetResourcesData(typeof(T).Name, () => new List<T> { new T { Key = "测试Key" }, }.ToQDataList());
					_list = new QList<string, T>();
					qdataList.ParseQdataList(_list);
				}
				return _list;
			}
		}
	}
	/// <summary>
	/// 轻量级数据库 通过对象访问 速度不会快很多 主要为了数据分块加载 如果对象格式发生更改会读取失败 
	/// </summary>
	public class QDataDB<T> where T : IKey<string> , new()
	{
		public string Path { get; private set; }
		Func<T, string> GetDBPath;  
		public QDataDB(string Path,Func<T,string> GetDBPath)
		{
			this.Path = Path;
			this.GetDBPath = GetDBPath;
			QDictionary<string, List<string>> QDataDBIndex = new QDictionary<string, List<string>>((key) => new List<string>());
			QDataDBIndex.LoadBytes(Path + "/" + nameof(QDataDBIndex) + ".bin");
			foreach (var kv in QDataDBIndex)
			{
				foreach (var key in kv.Value)
				{
					if (!PathIndex.ContainsKey(key))
					{
						PathIndex.Add(key, kv.Key);
					}
				}
			}
		}
		public void Save() 
		{
			PathIndex.SaveBytes(Path + "/" + nameof(PathIndex)+".bin");
			QDictionary<string, List<string>> QDataDBIndex = new QDictionary<string, List<string>>((key)=>new List<string>());
			foreach (var kv in PathIndex)
			{
				QDataDBIndex[kv.Value].Add(kv.Key);
			}
			foreach (var item in Data)
			{
				if (item.Value.Changed)
				{
					item.Value.SaveBytes(item.Key);
				}
			}
		}
		public void Load(string key=null)
		{
			if (key.IsNull())
			{
				foreach (var kv in PathIndex)
				{
					if (!Data.ContainsKey(kv.Value))
					{
						var data= Data[kv.Value];
					}
				}
			}
			else if(PathIndex.ContainsKey(key))
			{
				var data= Data[PathIndex[key]];
			}
		}
		public T Add(T data)
		{
			lock (PathIndex)
			{
				if (!PathIndex.ContainsKey(data.Key))
				{
					PathIndex.Add(data.Key, Path + "/" + GetDBPath(data) + ".bin");
				}
			}
			lock (Data[PathIndex[data.Key]])
			{
				Data[PathIndex[data.Key]].Add(data);
			}
			return data;
		}
		public bool Contains(string key)
		{
			return PathIndex.ContainsKey(key);
		}
		public T Get(string key)
		{
			if (PathIndex.ContainsKey(key))
			{
				var data= Data[PathIndex[key]].Get(key);
				if (data == null)
				{
					Debug.LogError("[" + key + "]数据为空:" + PathIndex[key]);
				}
				return data;
			}
			else
			{
				Debug.LogError(this + " 不包含[" + key + "]");
				return default;
			}
		}
		public void Clear()
		{
			foreach (var kv in PathIndex)
			{
				Data[kv.Value].Clear();
			}
			PathIndex.Clear();
		}
		public T this[string key]
		{
			get
			{ 
				return Get(key);
			}
		}
		public int Count => PathIndex.Count;
		Dictionary<string, string> PathIndex = new Dictionary<string, string>();
		public QDictionary<string, QList<string,T>> Data = new QDictionary<string, QList<string,T>>((path) =>new QList<string,T>().LoadBytes(path));   
	} 
	public class QDataRow:QList<string>,IKey<string>
    {
        public string Key { get => base[0]; set
            {

                base[0] = value;
            }
        }
		public string this[string title]
		{
			get => base[OwnerData.GetTitleIndex(title)];
			set => base[OwnerData.GetTitleIndex(title)] = value;
		}
        public T GetValue<T>(int index=1)
        {
			if (typeof(T) == typeof(string))
			{
				return (T)(object)base[index] ;
			}
			else
			{
				return base[index].ParseQData<T>(default, false);
			}
        }
		public void SetValueType(object value,Type type,int index=1)
		{
			if (type == typeof(string))
			{
				base[index] = (string)value;
			}
			else
			{

				base[index] = value.ToQDataType(type, false);
			}
		}
        public void SetValue<T>(T value, int index=1)
        {
			SetValueType(value, typeof(T), index);
        }
	
		public T GetValue<T>(string title)
		{
			return GetValue<T>(OwnerData.GetTitleIndex(title));
		}
		public bool HasValue(string title)
		{
			return OwnerData.TitleRow.IndexOf(title) >= 0;
		}
		public QDataRow SetValueType(string title, object value,Type type)
		{
			SetValueType(value, type, OwnerData.GetTitleIndex(title));
			return this;
		}
		public QDataRow SetValue<T>(string title,T value)
        {
			SetValueType(title, value, typeof(T));
            return this;
        }
      
        public QDataRow()
        {
        }
        public QDataList OwnerData { get; internal set; }
        public QDataRow(QDataList ownerData)
        {
            OwnerData = ownerData;
        }
        public override string ToString()
        {
            using (var writer=new StringWriter())
            {
                for (int j = 0; j < Count; j++)
                {
                    var qdata = this[j];
                    writer.Write(qdata.ToElement());
                    if (j < Count - 1)
                    {
                        writer.Write('\t');
                    }
                }
                return writer.ToString();
            }
        }
    }
	public static class QDataListTool
	{
		public static string ToElement(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "";
			}
			if (value.Contains("\t"))
			{
				value = value.Replace("\t", " ");
			}

			if (value.Contains("\n"))
			{
				if (value.Contains("\""))
				{
					value = value.Replace("\"", "\"\"");
				}
				value = "\"" + value + "\"";
			}
			return value;
		}
		public static string ParseElement(this string value)
		{
			if (!string.IsNullOrEmpty(value)&&value.StartsWith("\"") && value.EndsWith("\"") && (value.Contains(",")||value.Contains("\n")||value.Contains("\"\"")))
			{
				value = value.Substring(1, value.Length - 2); 
				value = value.Replace("\"\"", "\"");
				return value;
			}
			return value;
		}
		public static string ReadElement(this StringReader reader, out bool newLine)
		{
			newLine = true;
			using (var writer = new StringWriter())
			{
				if (reader.Peek() == '\"')
				{
					var checkExit = true;
					while (!reader.IsEnd())
					{
						var c = reader.Read();
						if (c != '\r')
						{
							writer.Write((char)c);
						}
						if (c == '\"')
						{
							checkExit = !checkExit;
						}
						if (checkExit)
						{
							reader.NextIs('\r');
							if (reader.NextIs('\n')) break;
							if (reader.NextIs('\t'))
							{
								if (!reader.IsEnd())
								{
									newLine = false;
								}
								break;
							}
						}

					}
					var value = ParseElement( writer.ToString());
					return value;
				}
				else
				{
					while (!reader.IsEnd() && !reader.NextIs('\n'))
					{
						if (reader.NextIs('\t'))
						{
							if(!reader.IsEnd())
							{
								newLine = false;
							}
							break;
						}
						var c = (char)reader.Read();
						if (c != '\r')
						{
							writer.Write(c);
						}
					}
					return writer.ToString();
				}

			}
		}

	}
}
