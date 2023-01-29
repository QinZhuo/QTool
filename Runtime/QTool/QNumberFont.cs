using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	[RequireComponent(typeof(Text))]
	public class QNumberFont : MonoBehaviour
	{
		public Text text;
		public Font font;
		private Font lastFont;
		public void Reset()
		{
			text = GetComponent<Text>();
		}
		public void OnChangeText(string value)
		{
			if (text != null)
			{
				if (float.TryParse(value, out var number))
				{
					if (text.font != font)
					{
						lastFont = text.font;
					}
					text.font = font;
				}
				else
				{
					if (lastFont != null)
					{
						text.font = lastFont;
					}
				}
			}
		}
	}
}

