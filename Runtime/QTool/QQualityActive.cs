using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace QTool {
	[ExecuteInEditMode]
	public class QQualityActive : MonoBehaviour
	{
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
				OnQualityChange?.Invoke();
			}
		}
		public static event System.Action OnQualityChange;
		[QName("范围"), Range(0, 5)]
		public Vector2Int levelRange = Vector2Int.zero;
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
			gameObject.SetActive(CurLevel >= levelRange.x && CurLevel <= levelRange.y);
		}
	}
}
