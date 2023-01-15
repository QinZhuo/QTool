using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace QTool
{

	[ExecuteInEditMode]
	public class QShaderUI : MonoBehaviour
	{
		public RectTransform rectTransform => transform as RectTransform;
		[QName("UI位置大小")]
		public string uiPosSize = "";
		[QName("鼠标位置按钮信息")]
		public string mousePosButton = "";
		Material _mat;
		Material Mat
		{
			get
			{
				return _mat ==null?_mat= GetComponent<Graphic>().GetInstanceMaterial():_mat;
			}
		}
		private void OnRectTransformDimensionsChange()
		{
			if (!uiPosSize.IsNull())
			{
				Mat.SetVector(uiPosSize,new Vector4( rectTransform.sizeDelta.x,rectTransform.sizeDelta.y,rectTransform.anchoredPosition.x,rectTransform.anchoredPosition.y));
			}
		}
		Vector2 MousePosition => new Vector2(
			(Event.current.mousePosition.x - rectTransform.DownLeft().x) / rectTransform.GetWidth(),
			(Screen.height - Event.current.mousePosition.y- rectTransform.DownLeft().y) / rectTransform.GetHeight());
		private void OnGUI()
		{
			if (!mousePosButton.IsNull())
			{
				if (Event.current != null)
				{
					var pos = MousePosition;
					Mat.SetVector(mousePosButton, new Vector4(pos.x, pos.y, Event.current.button));
				}
			}
		}
	}
}
