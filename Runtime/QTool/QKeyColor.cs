using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QKeyColor : MonoBehaviour
	{
		[SerializeField, QReadonly]
		private Color m_Color;
		[QName("刷新")]
		private void Start()
		{
			SetKey(name);
		}
		public void SetKey(string key)
		{
			if (List.ContainsKey(key))
			{
				m_Color = List.Get(key).color;
			}
			else
			{
				m_Color = key.ToColor();
			}
			if (Application.isPlaying)
			{
				foreach (var graphic in graphics)
				{
					if (graphic == null) continue;
					graphic.SetColorH(m_Color.ToH());
				}
			}
			OnColorChange.Invoke(m_Color);
		}
		[UnityEngine.Serialization.FormerlySerializedAs("OnKeyChange")]
		public ColorEvent OnColorChange = new ColorEvent();
		public List<UnityEngine.UI.Graphic> graphics = new List<UnityEngine.UI.Graphic>();
		public List<QKeyColorValue> List = new List<QKeyColorValue>();
	}
}

