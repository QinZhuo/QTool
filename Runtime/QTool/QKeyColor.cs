using System.Collections.Generic;
using UnityEngine;
namespace QTool
{

	public class QKeyColor : MonoBehaviour
	{
		[SerializeField]
		private Color m_Color;
		private void OnValidate()
		{
			if (graphics.Count > 0)
			{
				var h = m_Color.ToH();
				foreach (var graphic in graphics)
				{
					if (graphic == null) continue;
					graphic.SetColorH(h);
				}
			}
			OnColorChange.Invoke(m_Color);
		}
		public void SetKey(string key)
		{
			m_Color = key.ToColor();
			OnValidate();
		}
		[UnityEngine.Serialization.FormerlySerializedAs("OnKeyChange")]
		public ColorEvent OnColorChange = new ColorEvent();
		public List<UnityEngine.UI.Graphic> graphics = new List<UnityEngine.UI.Graphic>();
	}
}

