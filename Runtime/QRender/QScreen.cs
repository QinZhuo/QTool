using System;
using System.Reflection;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
    public static class QScreen
    {
		static Texture2D CaptureTexture2d=null;
		public static Texture2D Capture()
		{
			if (CaptureTexture2d == null || CaptureTexture2d.width != Screen.width || CaptureTexture2d.height != Screen.height)
			{
				CaptureTexture2d = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
			}
			return Camera.main.Capture(Screen.width,Screen.height,CaptureTexture2d);
		}
		public static Texture2D Capture(this Camera camera,int width, int height, Texture2D texture=null,int desX=0,int desY=0)
		{
			if (texture == null)
			{
				texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
			}
			var rt = new RenderTexture(width, height, 32);
			rt.autoGenerateMips = false;
			camera.targetTexture = rt;
			camera.Render();
			camera.targetTexture = null;
			RenderTexture.active = rt;
			texture.ReadPixels(new Rect(0, 0, width, height), desX, desY);
			RenderTexture.active = null;
			texture.Apply();
			rt.Release();
			return texture;
		}
		public static Texture2D CaptureAround(this GameObject gameObject,int size=512,int count=8)
		{
			var xCount = count == 1 ? count : Mathf.CeilToInt(Mathf.Sqrt(count));
			var yCount = count == 1 ? count : Mathf.CeilToInt(Mathf.Sqrt(count));
			var texture = new Texture2D(size * xCount, size* yCount, TextureFormat.BGRA32,false);
			var camera= gameObject.transform.GetChild(nameof(Capture) + nameof(Camera), true).GetComponent<Camera>(true);
			camera.CopyFrom(Camera.main);
			camera.orthographic = true;
			camera.clearFlags = CameraClearFlags.Color;
			camera.backgroundColor = Color.clear;
			Bounds bounds = gameObject.GetBounds();
			float maxSize = Mathf.Max(bounds.size.x,bounds.size.y, bounds.size.z)*1.05f;
			camera.nearClipPlane = 0.0f;
			camera.farClipPlane = maxSize;
			camera.orthographicSize = maxSize / 2;
			if (count == 1)
			{
				camera.transform.position = bounds.center + -Camera.main.transform.forward * maxSize / 2;
				camera.transform.LookAt(bounds.center);
				camera.Capture(size, size, texture);
			}
			else
			{
				camera.transform.position = bounds.center + Vector3.right * maxSize / 2;
				camera.transform.LookAt(bounds.center);
				var angle = 360f / count;
				for (int i = 0; i < count; i++)
				{
					var x = i % xCount;
					var y = i / xCount;
					camera.Capture(size, size, texture, size * x,size* y);
					camera.transform.RotateAround(gameObject.transform.position, Vector3.up, -angle);
				}
			}
			camera.gameObject.CheckDestory();
			return texture;
		}
		static bool IsDrag = false;
		static void OnGUI()
		{
			IsDrag = Event.current.mousePosition.y < 40 && Event.current.isMouse;
		}
		static void OnUpdate()
		{
			if(IsDrag&&CurWindow != default)
			{
#if PLATFORM_STANDALONE_WIN
				ReleaseCapture();
				SendMessage(CurWindow, 0xA1, 0x02, 0);
				SendMessage(CurWindow, 0x0202, 0, 0);
#endif
			}
		}
		public static void SetResolution(int width, int height, bool fullScreen)
		{
			

			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
				case RuntimePlatform.LinuxPlayer:
					Screen.SetResolution(width, height, fullScreen);
					break;
				default:
					Screen.SetResolution(width, height, true);
					break;
			}
#if UNITY_EDITOR
			SetSize(width, height);
#else
			if (coroutine != null)
			{
				QToolManager.Instance.StopCoroutine(coroutine);
				coroutine = null;
			}
			QToolManager.Instance.StartCoroutine(SetNoBorder(width, height));
#endif
		}
#if !UNITY_EDITOR
		static Coroutine coroutine = null;
#endif
		static IntPtr CurWindow = default;
		static IEnumerator SetNoBorder(int width,int height)
		{
			CurWindow = default;
			yield return new WaitForEndOfFrame();
			if (Time.timeScale > 0)
			{
				yield return new WaitForFixedUpdate();
			}
			else
			{
				Time.timeScale = 1;
				yield return new WaitForFixedUpdate();
				Time.timeScale = 0;
			}
			
			if (!Screen.fullScreen)
			{
#if PLATFORM_STANDALONE_WIN
				CurWindow = GetForegroundWindow();
				SetWindowLong(CurWindow, GWL_STYLE, WS_POPUP);
				SetWindowPos(CurWindow, 0, (Screen.currentResolution.width - width) / 2, (Screen.currentResolution.height - height) / 2, width, height, SWP_SHOWWINDOW);
#endif
				QToolManager.Instance.OnGUIEvent += OnGUI;
				QToolManager.Instance.OnUpdateEvent += OnUpdate;
			}
			else
			{
				QToolManager.Instance.OnGUIEvent -= OnGUI;
				QToolManager.Instance.OnUpdateEvent -= OnUpdate;
			}
		}
#region 分辨率设置逻辑

#if PLATFORM_STANDALONE_WIN
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);

		[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint =nameof(GetForegroundWindow))]
		static extern IntPtr GetForegroundWindow();

		[System.Runtime.InteropServices.DllImport("user32.DLL")]
		static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		//设置窗口位置，大小
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool ReleaseCapture();
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

		const uint SWP_SHOWWINDOW = 0x0040;
		const int GWL_STYLE = -16;
		const int WS_BORDER = 1; //window with border
		const int WS_POPUP = 0x800000;
#endif


#if UNITY_EDITOR

		static object gameViewSizesInstance;
        static MethodInfo getGroup;

        static QScreen()
        {
            // gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            getGroup = sizesType.GetMethod("GetGroup");
            gameViewSizesInstance = instanceProp.GetValue(null, null);
        }

        private enum GameViewSizeType
        {
            AspectRatio, FixedResolution
        }

        private static void SetSize(int index)
        {
            var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var gvWnd = EditorWindow.GetWindow(gvWndType);
            selectedSizeIndexProp.SetValue(gvWnd, index, null);
        }



        private static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
        {

            var group = GetGroup(sizeGroupType);
            var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
            var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var ctor = gvsType.GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(string) });
            foreach (var c in gvsType.GetConstructors())
            {
                if (ctor == null && c.GetParameters().Length == 4)
                {
                    ctor = c;
                    break;
                }
            }
            var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
            addCustomSize.Invoke(group, new object[] { newSize });
        }


        private static int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
        {
            var group = GetGroup(sizeGroupType);
            var groupType = group.GetType();
            var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
            var getCustomCount = groupType.GetMethod("GetCustomCount");
            int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
            var getGameViewSize = groupType.GetMethod("GetGameViewSize");
            var gvsType = getGameViewSize.ReturnType;
            var widthProp = gvsType.GetProperty("width");
            var heightProp = gvsType.GetProperty("height");
            var indexValue = new object[1];
            for (int i = 0; i < sizesCount; i++)
            {
                indexValue[0] = i;
                var size = getGameViewSize.Invoke(group, indexValue);
                int sizeWidth = (int)widthProp.GetValue(size, null);
                int sizeHeight = (int)heightProp.GetValue(size, null);
                if (sizeWidth == width && sizeHeight == height)
                    return i;
            }
            return -1;
        }

        static object GetGroup(GameViewSizeGroupType type)
        {
            return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
        }


        private static GameViewSizeGroupType GetCurrentGroupType()
        {
            var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
            return (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
        }


        private static void SetSize(int width, int height)
        {
            int index = FindSize(GetCurrentGroupType(), width, height);
            if (index == -1)
            {
                AddCustomSize(GameViewSizeType.FixedResolution, GetCurrentGroupType(), width, height, width+"x"+height);
                index = FindSize(GetCurrentGroupType(), width, height);
            }
            if (index != -1)
            {
                SetSize(index);
            }
            else
            {
                Debug.LogError("设置游戏视窗分辨率出错 " + width.ToString() + "*" + height.ToString());
            }
        }
#endif
#endregion

	}
}
