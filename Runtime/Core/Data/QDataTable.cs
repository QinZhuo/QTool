using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using QTool.Reflection;
using System.Data;
namespace QTool {
	/// <summary>
	/// 表格式字符存储逻辑 csv格式存取
	/// </summary>
	public class QDataTable : QList<string, QDataTable.Row> {
		public const string Extension = ".csv";
		public const char SplitChar = '\t';
		public const char NewLineChar = '\n';
		public static QDataTable Load(string path, Func<QDataTable> autoCreate = null) {
			QDataTable data = null;
			try {
				data = new QDataTable();
				data.LoadPath = path;
				QFileTool.LoadAll(path, (fileValue, loadPath) => {
					data.Add(new QDataTable(fileValue) { LoadPath = loadPath });
				}, "{}");
			}
			catch (Exception e) {
				Debug.LogError("读取QDataList[" + path + "]出错：\n" + e);
			}
			if (data.Count == 0) {
				if (autoCreate != null) {
					data = autoCreate();
					data.LoadPath = path;
					data.Save();
					Debug.LogWarning("不存在QDataList自动创建[" + path + "]:\n" + data);
				}
			}
			return data;
		}
		public string LoadPath { get; internal set; }
		public Row Titles { get; private set; }
		public void Save(string path = null) {
			if (string.IsNullOrEmpty(path)) {
				path = LoadPath;
			}
			QFileTool.Save(path, ToString(), System.Text.Encoding.Unicode);
		}
		public int GetTitleIndex(string title) {
			var index = Titles.IndexOf(title);
			if (index >= 0) {
				return index;
			}
			else {
				Titles.Add(title);
				return Titles.IndexOf(title);
			}
		}
		public void SetTitles(params string[] titles) {
			for (int i = 0; i < titles.Length; i++) {
				Titles[i] = titles[i];
			}
		}
		public new Row this[int index] {
			get {
				if (index >= Count) {
					var line = new Row(this);
					base[index] = line;
				}
				return base[index];
			}
		}
		public QDataTable() {
			Titles = new Row(this);
			AutoCreate = () => new Row(this);
		}
		public QDataTable(string dataStr) : this() {
			Parse(dataStr);
		}
		private void Parse(string dataStr) {
			Clear();
			using (var reader = new StringReader(dataStr)) {
				int rowIndex = 0;
				int valueIndex = 0;
				var row = Titles;
				while (!reader.IsEnd()) {
					var value = reader.ReadElement(out var newLine);
					row[valueIndex] = value;
					valueIndex++;
					if (newLine) {
						if (row != Titles && row.Count > 0) {
							if (!string.IsNullOrEmpty(row.Key)) {
#if !UNITY_SWITCH
								if (ContainsKey(row.Key)) {
									QDebug.LogWarning("加载覆盖 [" + row.Key + "] 来自文件 " + LoadPath + "\n旧数据: " + this[row.Key] + "\n新数据: " + row);
								}
#endif
								Add(row);
							}
						}
						valueIndex = 0;
						rowIndex++;
						row = new Row(this);
					}

				}
			}
		}
		public void Add(QDataTable addList) {
			if (Titles.Count == 0) {
				for (int i = 0; i < addList.Titles.Count; i++) {
					Titles[i] = addList.Titles[i];
				}
			}
			for (int i = 0; i < addList.Count; i++) {
				var row = addList[i];
				if (ContainsKey(row.Key)) {
					QDebug.LogWarning("加载覆盖 [" + row.Key + "] 来自文件 " + addList.LoadPath + " 原文件 " + LoadPath + "\n旧数据: " + this[row.Key] + "\n新数据: " + row);
				}
				var newRow = this[row.Key];
				for (int j = 1; j < addList.Titles.Count; j++) {
					var title = addList.Titles[j];
					newRow[title] = row[title];
				}
			}

		}
		public override string ToString() {
			using (var writer = new StringWriter()) {
				writer.Write(Titles.ToString());
				for (int i = 0; i < Count; i++) {
					writer.Write(this[i].ToString());
				}
				return writer.ToString();
			}

		}

		public class Row : QList<string>, IKey<string> {
			public string Key {
				get => this[nameof(Key)]; set {

					this[nameof(Key)] = value;
				}
			}
			public string this[string title] {
				get => base[Table.GetTitleIndex(title)];
				set => base[Table.GetTitleIndex(title)] = value;
			}
			public T GetValue<T>(int index = 1, T defaultValue = default) {
				if (typeof(T) == typeof(string)) {
					return (T)(object)base[index];
				}
				else {
					return base[index].ParseQData(defaultValue);
				}
			}
			public void SetValueType(object value, Type type, int index = 1) {
				if (type == typeof(string)) {
					base[index] = (string)value;
				}
				else {

					base[index] = value.ToQDataType(type);
				}
			}
			public void SetValue<T>(T value, int index = 1) {
				SetValueType(value, typeof(T), index);
			}

			public T GetValue<T>(string title, T defaultValue = default) {
				return GetValue(Table.GetTitleIndex(title), defaultValue);
			}
			public bool HasValue(string title) {
				return Table.Titles.IndexOf(title) >= 0;
			}
			public Row SetValueType(string title, object value, Type type) {
				SetValueType(value, type, Table.GetTitleIndex(title));
				return this;
			}
			public Row SetValue<T>(string title, T value) {
				SetValueType(title, value, typeof(T));
				return this;
			}

			public Row() {
			}
			public QDataTable Table { get; internal set; }
			public Row(QDataTable ownerData) {
				Table = ownerData;
			}
			public override string ToString() {
				using (var writer = new StringWriter()) {
					for (int j = 0; j < Count; j++) {
						var qdata = this[j];
						writer.Write(qdata.ToElement());
						if (j < Count - 1) {
							writer.Write(SplitChar);
						}
					}
					writer.Write('\r');
					writer.Write(NewLineChar);
					return writer.ToString();
				}
			}
		}
		public static void UnLoadAll() {
			foreach (var type in typeof(QDataTable<>).GetAllTypes()) {
				type.InvokeStaticFunction("UnLoad");
			}
		}
	}
	/// <summary>
	/// 运行时静态数据表 从Resouces文件夹加载字符数据 通过静态函数访问 只读
	/// </summary>
	public static class QDataTable<T> where T :IKey<string>, new() {
		//public string Key { get; set; }
		//[QIgnore]
		//public QDataTable.Row Row { get; private set; }

		public static bool ContainsKey(string key) {
			if (DataTable == null) {
				Load();
			}
			return DataTable?.ContainsKey(key) == true;
		}
		public static T Get(string key) {
			if (string.IsNullOrEmpty(key)) {
				Debug.LogError("key 为空");
				return default;
			}
			key = key.Trim();
			if (ContainsKey(key)) {
				return DataCache[key];
			}
			else {
				Debug.LogError(typeof(T).Name + " 未找到[" + key + "]");
				return default;
			}
		}
		private static QDictionary<string, T> DataCache = new(key => {
			var row = DataTable[key];
			var data= row.Parse<T>();
			return data;
		});
		public static QDataTable DataTable { internal set; get; } 
		#region 加载数据
		public static string GetResourcesPath(string key = null) {
			return QFileTool.ResourcesPathRoot.Combine(typeof(T).Name).Combine(key) + QDataTable.Extension; 
		}
		public static QDataTable LoadQDataList(string key = null) {
			QDebug.Begin("加载 QDataList<" + typeof(T) + "> 数据 ");
			var dataList = QDataTable.Load(GetResourcesPath(key), () => new List<T> { new T { Key = Application.productName }, }.ToQDataList());
			QDebug.End("加载 QDataList<" + typeof(T) + "> 数据 ", dataList.Count + " 条");
			return dataList;
		}
		//public static async Task PreLoadAsync() {
		//	if (_List == null) {
		//		_List = new QList<string, T>();
		//		await LoadQDataList().ParseQDataListAsync(_List).Run();
		//	}
		//}
		public static void Load(string key = null) {
			DataCache.Clear();
			DataTable = LoadQDataList(key);
		}
		public static void UnLoad() {
			DataCache.Clear();
			DataTable = null;
		}
		#endregion
	}

	public static class QDataTableTool {
		public static string ToElement(this string value) {
			if (string.IsNullOrEmpty(value)) {
				return "";
			}
			if (value.Contains("\t")) {
				value = value.Replace("\t", " ");
			}
			if (value.Contains("\n") || value.Contains(",")) {
				if (value.Contains("\"")) {
					value = value.Replace("\"", "\"\"");
				}
				value = "\"" + value + "\"";
			}
			return value;
		}
		public static string ParseElement(this string value) {
			if (value.IsNull()) return value;
			if (value.StartsWith("\"") && value.EndsWith("\"")) {
				if (value.Contains("\n") || value.Contains("\"\"") || value.Contains(",")) {
					value = value.Substring(1, value.Length - 2);
					value = value.Replace("\"\"", "\"");
				}
			}
			return value;
		}
		public static string ReadElement(this StringReader reader, out bool newLine) {
			newLine = true;
			using (var writer = new StringWriter()) {
				if (reader.Peek() == '\"') {
					var checkExit = true;
					while (!reader.IsEnd()) {
						var c = reader.Read();
						if (c != '\r') {
							writer.Write((char)c);
						}
						if (c == '\"') {
							checkExit = !checkExit;
						}
						if (checkExit) {
							reader.NextIs('\r');
							if (reader.NextIs(QDataTable.NewLineChar)) break;
							if (reader.NextIs(QDataTable.SplitChar)) {
								if (!reader.IsEnd()) {
									newLine = false;
								}
								break;
							}
						}

					}
					var value = ParseElement(writer.ToString());
					return value;
				}
				else {
					while (!reader.IsEnd() && !reader.NextIs(QDataTable.NewLineChar)) {
						if (reader.NextIs(QDataTable.SplitChar)) {
							if (!reader.IsEnd()) {
								newLine = false;
							}
							break;
						}
						var c = (char)reader.Read();
						if (c != '\r') {
							writer.Write(c);
						}
					}
					return writer.ToString();
				}

			}
		}

		public static QDataTable ToQDataList<T>(this IList<T> list, QDataTable qdataList = null, Type type = null) {
			if (type == null) {
				type = typeof(T);
				if (type == typeof(object)) {
					throw new Exception(nameof(QDataTable) + "类型出错 " + type);
				}
			}
			if (qdataList == null) {
				qdataList = new QDataTable();
			}
			else {
				qdataList.Titles.Clear();
				qdataList.Clear();
			}

			var typeInfo = QSerializeType.Get(type);
			foreach (var member in typeInfo.Members) {
				qdataList.Titles.Add(member.QName);
				for (int i = 0; i < list.Count; i++) {
					qdataList[i].SetValueType(member.QName, member.Get(list[i]), member.Type);
				}
			}
			return qdataList;
		}
		public static List<T> ParseQDataList<T>(this QDataTable qdataList, List<T> list, Type type = null) {
			if (type == null) {
				type = typeof(T);
				if (type == typeof(object)) {
					throw new Exception(nameof(QDataTable) + "类型出错 " + type);
				}
			}
			QDebug.Begin("解析QDataList<" + type.Name + ">数据");
			var typeInfo = QSerializeType.Get(type);
			list.Clear();
			var memeberList = new List<QMemeberInfo>();
			foreach (var title in qdataList.Titles) {
				var member = typeInfo.GetMemberInfo(title);
				if (member == null) {
					Debug.LogWarning("读取 " + type.Name + "出错 不存在属性 " + title);
				}
				memeberList.Add(member);
			}
			foreach (var row in qdataList) {
				var t = type.CreateInstance();
				for (int i = 0; i < qdataList.Titles.Count; i++) {
					var member = memeberList[i];
					if (member?.Set != null) {
						try {
							var value = row[i].ParseElement();

							if (member.Type == typeof(string) &&(value==null|| !(value.StartsWith('\"') && value.EndsWith('\"')))) {
								member.Set(t, value);
							}
							else {
								member.Set(t, value.ParseQDataType(member.Type));
							}
						}
						catch (Exception e) {
							Debug.LogException(new Exception("读取 " + type.Name + "出错 设置[" + row.Key + "]属性 " + member.Key + "(" + member.Type + ")异常：\n", e));
						}

					}
				}
				list.Add((T)t);
			}
			//QDebug.End("解析QDataList<" + type.Name + ">数据", list.Count + " 条 ");
			return list;
		}
		public static T Parse<T>(this QDataTable.Row row, Type type = null) {
			if (type == null) {
				type = typeof(T);
			}
			var typeInfo = QSerializeType.Get(type);
			var memeberList = new List<QMemeberInfo>();
			var result = type.CreateInstance();
			for (int i = 0; i < row.Table.Titles.Count; i++) {
				var member = typeInfo.GetMemberInfo(row.Table.Titles[i]);
				if (member?.Set != null) {
					try {
						var value = row[i].ParseElement();

						if (member.Type == typeof(string) && (value == null || !(value.StartsWith('\"') && value.EndsWith('\"')))) {
							member.Set(result, value);
						}
						else {
							member.Set(result, value.ParseQDataType(member.Type));
						}
					}
					catch (Exception e) {
						Debug.LogException(new Exception("读取 " + type.Name + "出错 设置[" + row.Key + "]属性 " + member.Key + "(" + member.Type + ")异常：\n", e));
					}

				}
			}
			return (T)result;
		}
		//public static async Task<List<T>> ParseQDataListAsync<T>(this QDataTable qdataList, List<T> list, Type type = null) {
		//	if (type == null) {
		//		type = typeof(T);
		//		if (type == typeof(object)) {
		//			throw new Exception(nameof(QDataTable) + "类型出错 " + type);
		//		}
		//	}
		//	QDebug.Begin("异步解析QDataList<" + type.Name + ">数据");
		//	var typeInfo = QSerializeType.Get(type);
		//	list.Clear();
		//	var memeberList = new List<QMemeberInfo>();
		//	foreach (var title in qdataList.Titles) {
		//		var member = typeInfo.GetMemberInfo(title);
		//		if (member == null) {
		//			Debug.LogWarning("读取 " + type.Name + "出错 不存在属性 " + title);
		//		}
		//		memeberList.Add(member);
		//	}
		//	foreach (var row in qdataList) {
		//		await Task.Yield();
		//		var t = type.CreateInstance();
		//		for (int i = 0; i < qdataList.Titles.Count; i++) {
		//			var member = memeberList[i];
		//			if (member != null) {
		//				var value = row[i].ParseElement();
		//				try {
		//					if (member.IsUnityObject) {
		//						var obj = await QObjectTool.LoadObjectAsync(value, member.Type);
		//						member.Set(t, obj);
		//					}
		//					else {
		//						if (member.Type == typeof(string) && !(value.StartsWith('\"') && value.EndsWith('\"'))) {
		//							member.Set(t, value);
		//						}
		//						else {
		//							member.Set(t, value.ParseQDataType(member.Type));
		//						}
		//					}
		//				}
		//				catch (System.Exception e) {
		//					Debug.LogError("读取 " + type.Name + "出错 设置[" + row.Key + "]属性 " + member.Key + "(" + member.Type + ")异常[" + value + "]：\n" + e);
		//				}

		//			}
		//		}
		//		list.Add((T)t);
		//	}
		//	QDebug.End("异步解析QDataList<" + type.Name + ">数据", list.Count + " 条 ");
		//	return list;
		//}
	}
}
