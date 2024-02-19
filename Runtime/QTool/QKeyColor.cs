using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace QTool
{
	public class QKeyColor : MonoBehaviour
	{
		[SerializeField]
		protected Color m_Color;
		[QName("开始时设置颜色")]
		public bool startSetColor = false;
		[QName("只控制色调")]
		public bool onlyHue = true;
		public virtual void Start()
		{
			if (startSetColor)
			{
				SetKey(name);
			}
		}
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
		public UnityEvent<Color> OnColorChange = new UnityEvent<Color>();
		public List<UnityEngine.UI.Graphic> graphics = new List<UnityEngine.UI.Graphic>(); 
	}
}

