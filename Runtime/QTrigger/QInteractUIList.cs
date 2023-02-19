using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QInteractUIList : QFollowUIList
	{
		public static QInteractUIList Instance { get; private set; }
		private QInteractSystem _Target=null;
		public QInteractSystem Target
		{
			get => _Target;
			set
			{
				if (_Target != null)
				{
					_Target.OnInteractAdd -= Add;
					_Target.OnInteractRemove -= Remove;
					_Target.OnInteractFresh -= Fresh;
				}
				if (value != null)
				{
					_Target = value;
					_Target.OnInteractAdd += Add;
					_Target.OnInteractRemove += Remove;
					_Target.OnInteractFresh += Fresh;
					foreach (var item in _Target.objectList)
					{
						Add(item);
					}
				}
			}
		}
		private void Awake()
		{
			Instance = this;
		}
		public void Add(QInteractObject qInteractObject)
		{
			var ui= this[qInteractObject.transform];
			ui.gameObject.InvokeEvent("显示");
			ui.gameObject.InvokeEvent("交互对象", qInteractObject);
		}
		public void Remove(QInteractObject qInteractObject)
		{
			var ui = this[qInteractObject.transform];
			ui.gameObject.InvokeEvent("隐藏");
			Push(ui.gameObject);
		}
		QFollowUI LastUI= null;
		public void Fresh(QInteractObject qInteractObject)
		{
			var ui = qInteractObject==null?null:this[qInteractObject.transform];
			if (ui != LastUI)
			{
				if (LastUI != null)
				{
					LastUI.gameObject.InvokeEvent("可交互");
				}
				if (ui != null)
				{
					ui.gameObject.InvokeEvent("不可交互");
				}
			}
		}
	}
}

