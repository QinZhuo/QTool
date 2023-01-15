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
		[UnityEngine.Serialization.FormerlySerializedAs("BGM")]
		[QName("音乐")]
		public AudioClip music;
		[UnityEngine.Serialization.FormerlySerializedAs("BGMIntro")]
		[QName("前奏")]
		public AudioClip start;
		[QName("事件音乐")]
		public List<EventMusic> eventMusic = new List<EventMusic>();

		protected override void Awake()
		{
			base.Awake();
			foreach (var eventMusic in eventMusic)
			{
				if (eventMusic.IsNull())
				{

				}
		}
		}
		public float BGMFadeInTime = 1f;
		public float BGMDelayTime = 0f;
		public AudioClip FightIntroMusic1;
		public AudioClip FightIntroMusic2;
		public AudioClip FightBGM_1X;
		public AudioClip FightBGM_2X;
		private static bool isSpeed2X = false;
		public static int list = 0;
		float time = 0;
		public static bool isStart = false;
		public bool isIntro = false;
		public float volume = 1f;
		public static bool Continue = true;
		public static bool newBGM = false;

		[System.Serializable]
		public struct EventMusic
		{
			public string key;
			[QName("音乐")]
			public AudioClip Music;
			[QName("前奏")]
			public AudioClip IntroMusic;
		}
	}
}
