using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace QTool.TileMap {
	[ExecuteInEditMode]
	public class QQualityLevel : MonoBehaviour
	{
		#region 质量检测
		static int curLevel = -1;
		[RuntimeInitializeOnLoadMethod]
		static void Init()
		{
			QToolManager.Instance.OnUpdateEvent += CheckUpdate;
		}
		static void CheckUpdate()
		{
			if (curLevel != QualitySettings.GetQualityLevel())
			{
				curLevel = QualitySettings.GetQualityLevel();
				QDebug.Log(nameof(QQualityLevel)+" 画质级别 " + curLevel);
				OnFresh?.Invoke();
			}
		}
		public static System.Action OnFresh;
		#endregion
		public List<BoolEvent> OnLevelChange = new List<BoolEvent>();
		private void Awake()
		{
			if (!Application.isPlaying) return;
			Fresh();
			OnFresh += Fresh;
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
				if (i != curLevel)
				{
					OnLevelChange[i]?.Invoke(false);
				}
			}
			OnLevelChange.Get(curLevel)?.Invoke(true);
		}
	}

}
