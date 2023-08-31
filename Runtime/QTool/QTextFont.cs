using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	[RequireComponent(typeof(Text))]
	public class QTextFont : MonoBehaviour
	{
		public Text text;
		[QName("数字字体")]
		[UnityEngine.Serialization.FormerlySerializedAs("font")]
		public Font numberFont;
		[QName("默认字体")]
		public Font defualtFont;
		public void Reset()
		{
			text = GetComponent<Text>();
		}
		public void OnChangeText(string value)
		{
			if (text != null&&!value.IsNull())
			{
				value = value.Replace(" ", "");
				value = value.Replace("x", "");
				value = value.Replace("X", "");
				if (float.TryParse(value, out var number))
				{
					if (text.font != numberFont)
					{
						defualtFont = text.font;
					}
					text.font = numberFont;
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

