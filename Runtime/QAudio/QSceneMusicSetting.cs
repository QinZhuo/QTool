using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
namespace QTool
{

	public class QSceneMusicSetting : InstanceBehaviour<QSceneMusicSetting>
	{
		public static bool UseSceneMusic { get; internal set; }
		[QName("背景乐"), SerializeField]
		private QMusicSetting defaultMusic = new QMusicSetting() { key = "默认" };
		[QName("事件音乐"),SerializeField]
		private List<QMusicSetting> eventMusic = new List<QMusicSetting>();

		protected override void Awake()
		{
			base.Awake();
			foreach (var eventMusic in eventMusic)
			{
				if (!eventMusic.IsNull())
				{
					QEventManager.Register(eventMusic.key, eventMusic.Play);
				}
			}
			if(QAudioManager.GetAudio(nameof(QAudioType.BGM)).CurBGM?.key != nameof(QAudioManager))
			{
				if (UseSceneMusic)
				{
					defaultMusic.Play();
				}
			}
		}
		private void OnDestroy()
		{
			foreach (var eventMusic in eventMusic)
			{
				if (!eventMusic.IsNull())
				{
					QEventManager.UnRegister(eventMusic.key, eventMusic.Play);
				}
			}
		}
	}
}
