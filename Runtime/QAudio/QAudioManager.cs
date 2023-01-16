using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace QTool
{

	public class QAudioManager : QToolManagerBase<QAudioManager>
	{
		static QDictionary<string, QAudioSource> AudioSources = new QDictionary<string, QAudioSource>();
		public static AudioMixerGroup QAudioSetting { get;private set; }
		protected override void Awake()
		{
			base.Awake();
			QAudioSetting = Resources.Load<AudioMixerGroup>(nameof(QAudioSetting));
			if (QAudioSetting == null)
			{
				Debug.LogWarning(nameof(Resources) + "找不到设置文件" + nameof(QAudioSetting));
			}
		}
		public static QAudioSource GetAudio(QAudioType audioType= QAudioType.SE)
		{
			return GetAudio(audioType.ToString());
		}
		public static QAudioSource GetAudio(string soundKey)
		{
			if (AudioSources[soundKey] == null)
			{
				var audioPrefab = QAudioSourcePrefab.Load(soundKey);
				if (audioPrefab == null)
				{
					AudioSources[soundKey] = Instance.transform.GetChild(soundKey, true).GetComponent<QAudioSource>(true);
				}
				else
				{
					AudioSources[soundKey] = Instantiate(audioPrefab, Instance.transform).GetComponent<QAudioSource>(true);
				}
				if (QAudioSetting != null && AudioSources[soundKey].Audio.outputAudioMixerGroup == null)
				{
					AudioSources[soundKey].Audio.outputAudioMixerGroup = QAudioSetting.audioMixer.FindMatchingGroups(soundKey).Get(0);
				}
				if(System.Enum.TryParse<QAudioType>(soundKey,out var type)){
					AudioSources[soundKey].SetType(type);
				}
				else
				{
					AudioSources[soundKey].SetType(QAudioType.SE);
				}
			}
			return AudioSources[soundKey];
		}
		private class QAudioSourcePrefab:Asset.QAssetLoader<QAudioSourcePrefab,GameObject>
		{
			
		}
	}
	public enum QAudioType
	{
		/// <summary>
		/// 背景音乐 全场唯一 循环播放
		/// </summary>
		BGM,
		/// <summary>
		/// 环境音 可拥有多个 循环播放
		/// </summary>
		BGS,
		/// <summary>
		/// 音效 可拥有多个 播放一次
		/// </summary>
		SE,
		/// <summary>
		/// 音乐效果 全场唯一 播放一次
		/// </summary>
		ME,
	}

	[System.Serializable]
	public struct QBackgroundMusic
	{
		public string key;
		[QName("音乐")]
		public AudioClip music;
		[QName("前奏")]
		public AudioClip start;
		public void Play()
		{
			QAudioManager.GetAudio(QAudioType.BGM).Play(this);
		}
	}
}
