using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
# endif
namespace QTool
{
    public static partial class Tool
    {
		/// <summary>
		/// 设置时间并演出
		/// </summary>
		public static void SetTime(this PlayableDirector playableDirector,float value)
		{
			playableDirector.time = value;
			playableDirector.Evaluate();
		}
		/// <summary>
		/// 立即完成当前演出
		/// </summary>
		public static void Complete(this PlayableDirector playableDirector)
		{
			if (playableDirector.playableAsset != null&& playableDirector.state== PlayState.Playing)
			{
				SetTime(playableDirector, (float)playableDirector.playableAsset.duration);
			}
		}
		/// <summary>
		/// 完成上一个演出并播放新的
		/// </summary>
		public static void CompleteAndPlay(this PlayableDirector playableDirector,PlayableAsset value)
		{
			if (playableDirector.playableAsset != value)
			{
				playableDirector.Complete();
			}
			playableDirector.Play(value);
		}
		public static async void DelayInvoke(this float time,System.Action action,bool ignoreGameTime=true)
        {
			if(!await QTask.Wait(time, ignoreGameTime).IsCancel())
			{
				action?.Invoke();
			}
        }
        public static Vector3 RayCastPlane(this Ray ray, Vector3 planeNormal, Vector3 planePoint)
        {
            float d = Vector3.Dot(planePoint - ray.origin, planeNormal) / Vector3.Dot(ray.direction, planeNormal);
            return d * ray.direction + ray.origin;
		}
		public static Vector3 RayCastPlane(this Ray ray,Vector3 planePoint)
		{
			var normal = Vector3.up;
			var angle = Vector3.Angle(ray.direction, Vector3.right);
			if (angle < 40 || angle >= 140)
			{
				normal = Vector3.right;
			}
			angle = Vector3.Angle(ray.direction, Vector3.forward);
			if (angle < 40 || angle >= 140)
			{
				normal = Vector3.forward;
			}
			return RayCastPlane(ray, normal, planePoint);
		}
		public static Vector3 RayCast(this Ray ray)
		{
			if (Physics.Raycast(ray, out var hitInfo))
			{
				return hitInfo.point;
			}
			return RayCastPlane(ray, Vector3.up, Vector3.zero);
		}
		public static Bounds GetBounds(this GameObject obj)
        {
            return obj.transform.GetBounds();
        }
        public static Bounds GetBounds(this Component com)
        {
            var bounds = new Bounds(com.transform.position, Vector3.zero);
            Renderer[] meshs = com.GetComponentsInChildren<Renderer>();
            foreach (var mesh in meshs)
            {
                if (mesh is MeshRenderer ||mesh is SpriteRenderer ||mesh is SkinnedMeshRenderer)
                {
                    if (bounds.extents == Vector3.zero)
                    {
                        bounds = mesh.bounds;
                    }
                    else
                    {
                        bounds.Encapsulate(mesh.bounds);
                    }
                }
            }
            return bounds;
        }

#if UNITY_EDITOR
        static void StartUpdateEditorTime()
        {
            if (!updateEditorTime)
            {
                updateEditorTime = true;
                UnityEditor.EditorApplication.update += () =>
                {
                    editorDeltaTime = (float)(UnityEditor.EditorApplication.timeSinceStartup - lastTime);
                    lastTime = UnityEditor.EditorApplication.timeSinceStartup;
                };
            }
        }
       
        static bool updateEditorTime = false;
        static double lastTime;
#endif
        static float editorDeltaTime;
        public static float EditorDeltaTime
        {
            get
            {
#if UNITY_EDITOR
                StartUpdateEditorTime();
#endif
                return editorDeltaTime>1? 0:editorDeltaTime;
            }
        }
		public static void AddAssetObject(this Object obj,Object childObj)
		{
#if UNITY_EDITOR
			if (obj != null&&!Application.IsPlaying(obj)&&obj.IsAsset())
			{
				AssetDatabase.AddObjectToAsset(childObj, obj);
				SetDirty(obj);
				AssetDatabase.SaveAssetIfDirty(obj);
			}
#endif
		}
		public static void RemoveAssetObject(this Object obj, Object childObj)
		{
#if UNITY_EDITOR
			if (obj != null && !Application.IsPlaying(obj) && obj.IsAsset())
			{
				AssetDatabase.RemoveObjectFromAsset(childObj);
				SetDirty(obj);
				AssetDatabase.SaveAssetIfDirty(obj);
			}
#endif
		}
		public static void SetDirty(this Object obj)
        {
#if UNITY_EDITOR
            if (obj != null&&!Application.IsPlaying(obj))
            {
                UnityEditor.EditorUtility.SetDirty(obj);
            }
# endif
        }
        public static void Record(this Object obj)
        {
#if UNITY_EDITOR
            Undo.RecordObject(obj, "RecordObj"+obj.GetHashCode());
#endif
        }

        public static T CheckInstantiate<T>(this T prefab, Transform parent)where T: Object
        {

#if UNITY_EDITOR
            var obj = PrefabUtility.InstantiatePrefab(prefab, parent) as T;
#else
            var obj = GameObject.Instantiate(prefab, parent);
#endif
            return obj ;
        }
      
        public static void CheckDestory(this Object obj)
        {
#if UNITY_EDITOR
            if (obj != null)
            {
				try
				{
					GameObject.DestroyImmediate(obj);
				}
				catch (System.Exception e)
				{
					if(obj is GameObject gameObject)
					{
						Debug.LogError("删除物体出错 " + gameObject.transform.GetPath() + "  " + e);
					}
				}
            }
#else
              GameObject.Destroy(obj);
#endif
        }
    }
    public static class RectTransformExtend
    {
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

        public static RectTransform RectTransform(this Transform transform)
        {
            return transform as RectTransform;
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

		public static Vector2 GetSize(this RectTransform trans)
		{
			return trans.rect.size;
		}

		public static float GetWidth(this RectTransform trans)
		{
			return trans.rect.width;
		}

		public static float GetHeight(this RectTransform trans)
		{
			return trans.rect.height;
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

		public static T Reset<T>(this T comp) where T : Component
		{
			comp.ResetRotation().ResetPosition().ResetScale();
			return comp;
		}
	}
}
