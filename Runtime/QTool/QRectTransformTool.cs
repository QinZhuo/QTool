using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace QTool
{
    public static class QRectTransformTool
    {
		public static bool ContainsScreenPoint(this RectTransform rectTransform,Vector2 screenPoint)
		{
			return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint);
		}
		public static Vector2 ScreenPointToLocalPoint(this RectTransform rectTransform, Vector2 screenPoint)
		{
			if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint,Camera.main,out var localPoint))
			{
				return localPoint;
			}
			return Vector2.zero;
		}
		public static Vector2 UpRightRectOffset(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.Width() * (1 - rectTransform.pivot.x), rectTransform.Height() * (1 - rectTransform.pivot.y));
        }
        public static Vector2 DownLeftRectOffset(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.Width() * (rectTransform.pivot.x), rectTransform.Height() * (rectTransform.pivot.y));
        }

        public static float Height(this RectTransform rectTransform)
        {
            return rectTransform.rect.size.y;
        }
        public static float Width(this RectTransform rectTransform)
        {
            return rectTransform.rect.size.x;
        }
        public static Vector2 Size(this RectTransform rectTransform)
        {
            return rectTransform.rect.size;
        }
		public static float ScaleHeight(this RectTransform rectTransform)
		{
			return rectTransform.rect.size.y * rectTransform.lossyScale.y;
		}
		public static float ScaleWidth(this RectTransform rectTransform)
		{
			return rectTransform.rect.size.x * rectTransform.lossyScale.x;
		}
		public static Vector2 ScaleSize(this RectTransform rectTransform)
		{
			return new Vector2(rectTransform.ScaleWidth(), rectTransform.ScaleHeight());
		}
		public static RectTransform RectTransform(this Transform transform)
        {
            return transform as RectTransform;
        }
		public static Vector2 Center(this RectTransform rectTransform)
		{
			return rectTransform.DownLeft() + rectTransform.Size() /2;
		}
		public static Vector2 UpRight(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.position.x, rectTransform.position.y) + rectTransform.UpRightRectOffset();
        }
        public static Vector2 DownLeft(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.position.x, rectTransform.position.y) - rectTransform.DownLeftRectOffset();
        }
		public static void SetDefaultScale(this RectTransform trans)
		{
			trans.localScale = new Vector3(1, 1, 1);
		}

		public static void SetPivotAndAnchors(this RectTransform trans, Vector2 aVec)
		{
			trans.pivot = aVec;
			trans.anchorMin = aVec;
			trans.anchorMax = aVec;
		}

		public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
		}

		public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
		}

		public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
		}

		public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
		}

		public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
		}

		public static void SetSize(this RectTransform trans, Vector2 newSize)
		{
			Vector2 oldSize = trans.rect.size;
			Vector2 deltaSize = newSize - oldSize;
			trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
			trans.offsetMax = trans.offsetMax + new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));
		}

		public static void SetWidth(this RectTransform trans, float newSize)
		{
			SetSize(trans, new Vector2(newSize, trans.rect.size.y));
		}

		public static void SetHeight(this RectTransform trans, float newSize)
		{
			SetSize(trans, new Vector2(trans.rect.size.x, newSize));
		}
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
			comp.transform.position = Vector3.zero;
			return comp;
		}

		public static T ResetLocalPosition<T>(this T comp) where T : Component
		{
			comp.transform.localPosition = Vector3.zero;
			return comp;
		}
		public static T RotationEulerAdd<T>(this T comp,Quaternion otherRotation) where T : Component
		{
			comp.transform.rotation=Quaternion.Euler(comp.transform.rotation.eulerAngles+otherRotation.eulerAngles);
			return comp;
		}
		public static T Reset<T>(this T comp) where T : Component
		{
			comp.ResetRotation().ResetPosition().ResetScale();
			return comp;
		}

	
		public static float Up(this RectTransform rectTransform)
		{
			return rectTransform.transform.position.y + rectTransform.UpRightRectOffset().y;
		}
		public static float Down(this RectTransform rectTransform)
		{
			return rectTransform.transform.position.y - rectTransform.DownLeftRectOffset().y;
		}
		public static float Left(this RectTransform rectTransform)
		{
			return rectTransform.transform.position.x - rectTransform.DownLeftRectOffset().x;
		}

		public static float Right(this RectTransform rectTransform)
		{
			return rectTransform.transform.position.x + rectTransform.UpRightRectOffset().x;
		}
		public static bool ParentHas(this Transform transform,params Transform[] targets)
		{
			if (transform.parent == null)
			{
				return false;
			}
			else if (targets.Contains(transform.parent))
			{
				return true;
			}
			else
			{
				return transform.parent.ParentHas(targets);
			}
		}
	}
}
