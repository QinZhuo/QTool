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
		public Font defualtFont;
		public void Reset()
		{
			text = GetComponent<Text>();
		}
		public void OnChangeText(string value)
		{
			if (text != null&&!value.IsNull())
			{
				value = value.Replace("x", "");
				value = value.Replace(" ", "");
				if (float.TryParse(value, out var number))
				{
					if (text.font != font)
					{
						defualtFont = text.font;
					}
					text.font = font;
				}
				else
				{
					if (defualtFont != null)
					{
						text.font = defualtFont;
					}
				}
			}
		}
	}
}

