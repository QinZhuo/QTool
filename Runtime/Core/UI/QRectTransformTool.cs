using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace QTool
{
	public static class QRectTransformTool
	{
		#region 位置点
		public static bool ContainsScreenPoint(this RectTransform rect, Vector2 screenPoint)
		{
			return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint);
		}
		public static Vector2 ScreenPointToLocalPoint(this RectTransform rect, Vector2 screenPoint)
		{
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, Camera.main, out var localPoint))
			{
				return localPoint;
			}
			return Vector2.zero;
		}

		#endregion
		#region 大小
		public static float Height(this RectTransform rect)
		{
			return rect.rect.size.y * rect.lossyScale.y;
		}
		public static float Width(this RectTransform rect)
		{
			return rect.rect.size.x * rect.lossyScale.x;
		}
		public static Vector2 Size(this RectTransform rect)
		{
			return rect.rect.size * rect.lossyScale;
		}
		public static void SetSize(this RectTransform rect, Vector2 newSize)
		{
			Vector2 oldSize = rect.rect.size;
			Vector2 deltaSize = newSize - oldSize;
			rect.offsetMin = rect.offsetMin - new Vector2(deltaSize.x * rect.pivot.x, deltaSize.y * rect.pivot.y);
			rect.offsetMax = rect.offsetMax + new Vector2(deltaSize.x * (1f - rect.pivot.x), deltaSize.y * (1f - rect.pivot.y));
		}

		public static void SetWidth(this RectTransform rect, float newSize)
		{
			SetSize(rect, new Vector2(newSize, rect.rect.size.y));
		}

		public static void SetHeight(this RectTransform rect, float newSize)
		{
			SetSize(rect, new Vector2(rect.rect.size.x, newSize));
		}

		#endregion

		#region 位置



		public static Vector2 Center(this RectTransform rect)
		{
			return rect.LeftDown() + rect.Size() / 2;
		}
		public static float Up(this RectTransform rect)
		{
			return rect.transform.position.y + rect.Height() * (1 - rect.pivot.y);
		}
		public static float Down(this RectTransform rect)
		{
			return rect.transform.position.y - rect.Height() * rect.pivot.y;
		}
		public static float Left(this RectTransform rect)
		{
			return rect.transform.position.x - rect.Width() * rect.pivot.x;
		}

		public static float Right(this RectTransform rect)
		{
			return rect.transform.position.x + rect.Width() * (1 - rect.pivot.x);
		}
		public static Vector2 RightUp(this RectTransform rect)
		{
			return new Vector2(rect.Right(), rect.Up());
		}
		public static Vector2 LeftDown(this RectTransform rect)
		{
			return new Vector2(rect.Left(), rect.Down());
		}

		public static void SetPosition(this RectTransform rect, Vector2 newPos)
		{
			rect.position = new Vector3(newPos.x, newPos.y, rect.position.z);
		}
		public static void SetPivotAndAnchors(this RectTransform rect, Vector2 aVec)
		{
			rect.pivot = aVec;
			rect.anchorMin = aVec;
			rect.anchorMax = aVec;
		}
		public static void SetLeftDownPosition(this RectTransform rect, Vector2 newPos)
		{
			rect.position = new Vector3(newPos.x + rect.Width() * rect.pivot.x, newPos.y + (rect.pivot.y * rect.Height()), rect.position.z);
		}

		public static void SetLeftUpPosition(this RectTransform rect, Vector2 newPos)
		{
			rect.position = new Vector3(newPos.x + (rect.pivot.x * rect.Width()), newPos.y - (1f - rect.pivot.y) * rect.Height(), rect.position.z);
		}

		public static void SetRightDownPosition(this RectTransform rect, Vector2 newPos)
		{
			rect.position = new Vector3(newPos.x - ((1f - rect.pivot.x) * rect.Width()), newPos.y + (rect.pivot.y * rect.Height()), rect.position.z);
		}

		public static void SetRightUpPosition(this RectTransform rect, Vector2 newPos)
		{
			rect.position = new Vector3(newPos.x - ((1f - rect.pivot.x) * rect.Width()), newPos.y - ((1f - rect.pivot.y) * rect.Height()), rect.position.z);
		}

		#endregion
		#region 重置
		public static T ResetRotation<T>(this T comp) where T : Component
		{
			comp.transform.rotation = Quaternion.identity;
			return comp;
		}

		public static T ResetScale<T>(this T comp) where T : Component
		{
			comp.transform.localScale = Vector3.one;
			return comp;
		}

		public static T ResetPosition<T>(this T comp) where T : Component
		{
			comp.transform.localPosition = Vector3.zero;
			return comp;
		}

		public static T ResetLocalPosition<T>(this T comp) where T : Component
		{
			comp.transform.localPosition = Vector3.zero;
			return comp;
		}
		public static T Reset<T>(this T comp) where T : Component
		{
			comp.ResetRotation().ResetPosition().ResetScale();
			return comp;
		}
		#endregion
		private static int CheckIgnoreLayout(this Transform transform, int index)
		{
			for (int i = index; i >= 0; i--)
			{
				var layout = transform.GetChild(i)?.GetComponent<UnityEngine.UI.LayoutElement>();
				if (layout?.ignoreLayout == true)
				{
					index--;
				}
			}
			return index;
		}
		public static Transform GetLayoutChild(this Transform transform, int index)
		{
			for (int i = 0; i <= index; i++)
			{
				var child = transform.GetChild(i);
				if (child?.GetComponent<UnityEngine.UI.LayoutElement>()?.ignoreLayout == true)
				{
					index++;
				}
				if (i == index)
				{
					return child;
				}
			}
			return null;
		}
		public static int GetLayoutCount(this Transform transform)
		{
			return transform.CheckIgnoreLayout(transform.childCount - 1) + 1;
		}
		public static int GetLayoutIndex(this Transform transform)
		{
			return transform.parent.CheckIgnoreLayout(transform.GetSiblingIndex());
		}
		public static RectTransform GetLayoutRect(this RectTransform rectTransform)
		{
			rectTransform.GetComponent<UnityEngine.UI.LayoutElement>(true).ignoreLayout = true;
			var layoutRect = rectTransform.CloneRectTransform();
			layoutRect.SetSiblingIndex(rectTransform.GetSiblingIndex());
			rectTransform.SetAsLastSibling();
			return layoutRect;
		}
		public static void ReleaseLayoutRect(this RectTransform rectTransform, RectTransform layoutRect)
		{
			rectTransform.GetComponent<UnityEngine.UI.LayoutElement>(true).ignoreLayout = false;
			rectTransform.SetSiblingIndex(layoutRect.GetSiblingIndex());
			layoutRect.gameObject.PoolRelease();
		}
		public static RectTransform CloneRectTransform(this RectTransform rectTransform)
		{
			var cloneRect = rectTransform.parent.GetChild(rectTransform.name + "_" + rectTransform.GetHashCode(), true).GetComponent<RectTransform>(true);
			cloneRect.sizeDelta = rectTransform.sizeDelta;
			cloneRect.anchorMin = rectTransform.anchorMin;
			cloneRect.anchorMax = rectTransform.anchorMax;
			cloneRect.pivot = rectTransform.pivot;
			cloneRect.position = rectTransform.position;
			cloneRect.transform.SetParent(rectTransform.parent, true);
			return cloneRect;
		}
		public static RectTransform RectTransform(this Component rectform)
		{
			return rectform.transform as RectTransform;
		}
		public static bool ParentHas(this Transform rectform,params Transform[] targets)
		{
			if (rectform.parent == null)
			{
				return false;
			}
			else if (targets.Contains(rectform.parent))
			{
				return true;
			}
			else
			{
				return rectform.parent.ParentHas(targets);
			}
		}
		public static T RotationEulerAdd<T>(this T comp, Quaternion otherRotation) where T : Component
		{
			comp.transform.rotation = Quaternion.Euler(comp.transform.rotation.eulerAngles + otherRotation.eulerAngles);
			return comp;
		}
	}
}
