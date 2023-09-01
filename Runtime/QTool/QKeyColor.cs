using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QKeyColor : MonoBehaviour
	{
		[SerializeField]
		private string m_Key;
		[SerializeField]
		private Color m_Color;
		public string Key
		{
			get => m_Key; set
			{
				m_Key = value;
				Fresh();
			}
		}
		private void Fresh()
		{
			if (List.ContainsKey(Key))
			{
				m_Color = List.Get(Key).color;
			}
			else
			{
				m_Color = Key.ToColor();
			}
			OnKeyChange.Invoke(m_Color);
		}
		private void OnValidate()
		{
			Fresh();
		}
		public ColorEvent OnKeyChange = new ColorEvent();
		public List<QKeyColorValue> List = new List<QKeyColorValue>();
	}
}

