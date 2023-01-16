using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace QTool
{

	public class QAudioManager : QToolManagerBase<QAudioManager>
	{
		static QDictionary<string, QAudioSource> AudioSources = new QDictionary<string, QAudioSource>();
		static QDictionary<string, AudioMixerGroup> AudioMixerGroups = new QDictionary<string, AudioMixerGroup>();
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
		public static void Play(AudioClip clip,QAudioType audioType= QAudioType.SE)
		{
			Play(clip,audioType.ToString());
		}
		public static void Play(AudioClip clip, string soundKey)
		{
			GetAudio(soundKey).Play(clip);
		}
		public static void Play(QMusicSetting music, QAudioType audioType = QAudioType.SE)
		{
			Play(music, audioType.ToString());
		}
		public static void Play(QMusicSetting music, string soundKey)
		{
			if (soundKey == nameof(QAudioType.BGM))
			{
				music.key = nameof(QAudioManager);
			}
			GetAudio(soundKey).Play(music);
		}
		public static AudioMixerGroup GetMixer(string soundKey)
		{
			var group = AudioMixerGroups[soundKey];
			if (group == null)
			{
				group = QAudioSetting.audioMixer.FindMatchingGroups(soundKey).Get(0);
				AudioMixerGroups[soundKey] = group;
			}
			return group;
		}
		internal static QAudioSource GetAudio(string soundKey)
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
					AudioSources[soundKey].Audio.outputAudioMixerGroup = GetMixer(soundKey);
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
	public struct QMusicSetting
	{
		public string key;
		[QName("音乐")]
		public AudioClip music;
		[QName("前奏")]
		public AudioClip start;
		internal void Play()
		{
			QAudioManager.GetAudio(nameof(QAudioType.BGM)).Play(this);
		}
		public override string ToString()
		{
			return key + "[" + music + "][" + start + "]";
		}
	}
}
