using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace QTool {
	[ExecuteInEditMode]
	public class QQualityActive : MonoBehaviour
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
				QDebug.Log(nameof(QQualityActive)+" 画质级别 " + CurLevel);
				OnQualityChange?.Invoke();
			}
		}
		public static System.Action OnQualityChange;
		#endregion
		[QName("画质级别")]
		public int level = 0;
		private void Awake()
		{
			if (!Application.isPlaying) return;
			OnQualityChange += FreshActive;
		}
		private void Start()
		{
			if (!Application.isPlaying) return;
			FreshActive();
		}
		private void OnDestroy()
		{
			if (!Application.isPlaying) return;
			OnQualityChange -= FreshActive;
		}
		void FreshActive()
		{
			gameObject.SetActive(level == CurLevel);
		}
	}

}
