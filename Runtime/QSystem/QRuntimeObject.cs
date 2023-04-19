using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public abstract class QRuntimeObject<T,DataT> where T:QRuntimeObject<T,DataT>,new() where DataT : QDataList<DataT>, new()
	{
		public DataT Data { get; private set; }
		protected QRuntimeObject() { }
		public static T Get(string key)
		{
			var t= new T();
			t.Init(key);
			return t;
		}
		public virtual void Init(string key)
		{
			Data = QDataList<DataT>.Get(key);
		}
	}
}
