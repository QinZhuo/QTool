using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QInteractUIList : QFollowList
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
					transform.ClearChild();
				}
				if (value != null)
				{
					_Target = value;
					_Target.OnInteractAdd += Add;
					_Target.OnInteractRemove += Remove;
					_Target.OnInteractFresh += Fresh;
					foreach (var item in _Target.InteractList)
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
		public void Add(QTriggerObject qInteractObject)
		{
			var ui= this[qInteractObject.transform];
			ui.gameObject.InvokeEvent("显示");
			ui.gameObject.InvokeEvent("交互对象", qInteractObject);
		}
		public void Remove(QTriggerObject qInteractObject)
		{
			var ui = this[qInteractObject.transform];
			ui.gameObject.InvokeEvent("隐藏");
			Push(ui.gameObject);
		}
		QFollowUI LastUI= null;
		public void Fresh(QTriggerObject qInteractObject)
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

