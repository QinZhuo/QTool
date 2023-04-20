using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
namespace QTool
{
	public abstract class QRuntime<RuntimeT,DataT> where RuntimeT:QRuntime<RuntimeT,DataT>,new() where DataT : QDataList<DataT>, new()
	{
		public string Key { get; private set; }
		public DataT Data { get; private set; }
		protected QRuntime() { }
		public static RuntimeT Get(string key)
		{
			var t= new RuntimeT();
			t.Init(key);
			return t;
		}
		public virtual void Init(string key)
		{
			Key = key;
			Data = QDataList<DataT>.Get(key);
			
		}
	}
	public abstract class QRuntimeObject<RuntimeT, DataT> : MonoBehaviour where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
	{
		public RuntimeT Runtime { get; private set; }
		public DataT Data => Runtime.Data;
		public virtual void Start()
		{
			Runtime = QRuntime<RuntimeT, DataT>.Get(name);
			var dataInfo= QSerializeType.Get(typeof(DataT));
			if (dataInfo != null)
			{
				foreach (var member in dataInfo.Members)
				{
					if (member.Type.IsValueType)
					{
						gameObject.InvokeEvent(member.QName, member.Get(Data)?.ToString());
					}
				}
			}
			var runtimeInfo = QSerializeType.Get(typeof(RuntimeT));
			if (runtimeInfo != null)
			{
				foreach (var member in runtimeInfo.Members)
				{
					if (member.Type.Is(typeof(QRuntimeValue)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += gameObject.InvokeEvent;
						runtimeValue.InvokeOnValueChange();
					}
				}
			}
		}
		private void OnDestroy()
		{
			var typeInfo = QSerializeType.Get(typeof(RuntimeT));
			if (typeInfo != null)
			{
				foreach (var member in typeInfo.Members)
				{
					if (member.Type.Is(typeof(QRuntimeValue)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue>();
						runtimeValue.OnValueChange -= gameObject.InvokeEvent;
					}
				}
			}
		}
	}
}

