using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QInteractUIList : QFollowUIList
	{
		public QInteractUIList Instance { get; private set; }
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
			LastUI.gameObject.InvokeEvent("显示", true);
			LastUI.gameObject.InvokeEvent("交互对象", qInteractObject);
		}
		public async void Remove(QInteractObject qInteractObject)
		{
			var ui = this[qInteractObject.transform];
			LastUI.gameObject.InvokeEvent("显示", false);
			await QTask.Wait(1, true);
			Push(ui.gameObject);
		}
		QFollowUI LastUI= null;
		public void Fresh(QInteractObject qInteractObject)
		{
			var ui = this[qInteractObject.transform];
			if (ui != LastUI)
			{
				if (LastUI != null)
				{
					LastUI.gameObject.InvokeEvent("激活", false);
				}
				ui.gameObject.InvokeEvent("激活", true);
			}
		}
	}
}

