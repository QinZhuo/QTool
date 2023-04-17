using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace QTool {
	[ExecuteInEditMode]
	public class QQualityLevel : MonoBehaviour
	{
		#region 质量检测
		public static int CurLevel { get; private set; } = -1;
		[RuntimeInitializeOnLoadMethod]
		static void Init()
		{
			QToolManager.Instance.OnUpdateEvent += CheckUpdate;
		}
		static void CheckUpdate()
		{
			if (CurLevel != QualitySettings.GetQualityLevel())
			{
				CurLevel = QualitySettings.GetQualityLevel();
				QDebug.Log(nameof(QQualityLevel)+" 画质级别 " + CurLevel);
				OnFresh?.Invoke();
			}
		}
		public static System.Action OnFresh;
		#endregion
		public List<BoolEvent> OnLevelChange = new List<BoolEvent>();
		private void Awake()
		{
			if (!Application.isPlaying) return;
			OnFresh += Fresh;
		}
		private void Start()
		{
			if (!Application.isPlaying) return;
			Fresh();
		}
		private void OnDestroy()
		{
			if (!Application.isPlaying) return;
			OnFresh -= Fresh;
		}
		void Fresh()
		{
			if(OnLevelChange==null)return;
			for (int i = 0; i < OnLevelChange.Count; i++)
			{
				if (i != CurLevel)
				{
					OnLevelChange[i]?.Invoke(false);
				}
			}
			OnLevelChange.Get(CurLevel)?.Invoke(true);
		}
	}

}
