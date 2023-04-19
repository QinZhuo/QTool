using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public abstract class QRuntimeObject<T> where T:QDataList<T>,new()
	{
		public T Data { get; private set; }
		public virtual void Init(string key)
		{
			Data = QDataList<T>.Get(key);
		}
	}
}
