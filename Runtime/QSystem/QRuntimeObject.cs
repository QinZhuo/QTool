using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
namespace QTool
{
	public abstract class QRuntime<RuntimeT,DataT> where RuntimeT:QRuntime<RuntimeT,DataT>,new() where DataT : QDataList<DataT>, new()
	{
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
			var typeInfo = QSerializeType.Get(typeof(RuntimeT));
			if (typeInfo != null)
			{
				foreach (var member in typeInfo.Members)
				{
					if (member.Type.Is(typeof(QRuntimeValue)))
					{
						var runtimeValue = member.Get(this).As<QRuntimeValue>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += gameObject.InvokeEvent;
						runtimeValue.InvokeOnValueChange();
					}
				}
			}
		}
	}
}

