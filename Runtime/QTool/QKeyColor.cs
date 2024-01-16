using System.Collections.Generic;
using UnityEngine;
namespace QTool
{

	public class QKeyColor : MonoBehaviour
	{
		[SerializeField]
		protected Color m_Color;
		[QName("只控制色调")]
		public bool onlyHue = true;
		public void SetKey(string key)
		{
			m_Color = key.ToColor();
			OnValidate();
		}
		protected virtual void OnValidate()
		{
			if (graphics.Count > 0)
			{
				var h = m_Color.ToH();
				foreach (var graphic in graphics)
				{
					if (graphic == null) continue;
					if (onlyHue)
					{
						graphic.SetColorH(h);
					}
					else
					{
						graphic.color = m_Color;
					}
				}
			}
			OnColorChange.Invoke(m_Color);
		}
		[UnityEngine.Serialization.FormerlySerializedAs("OnKeyChange")]
		public ColorEvent OnColorChange = new ColorEvent();
		public List<UnityEngine.UI.Graphic> graphics = new List<UnityEngine.UI.Graphic>();
	}
}

