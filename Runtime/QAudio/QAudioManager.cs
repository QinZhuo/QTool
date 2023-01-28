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
		public static AudioMixer QAudioSetting { get;private set; }
		protected override void Awake()
		{
			base.Awake();
			QAudioSetting = Resources.Load<AudioMixer>(nameof(QAudioSetting));
			if (QAudioSetting == null)
			{
				Debug.LogWarning(nameof(Resources) + "找不到设置文件" + nameof(QAudioSetting));
			}
		}
		public static void Play(AudioClip clip,QAudioType audioType= QAudioType.SE)
		{
			Play(clip,audioType.ToString());
		}
		public static void Play(AudioClip clip, string audioKey)
		{
			GetAudio(audioKey).Play(clip);
		}
		public static void Play(QMusicSetting music, QAudioType audioType = QAudioType.BGM,float transition=1)
		{
			Play(music, audioType.ToString(), transition);
		}
		public static void Play(QMusicSetting music, string audioKey, float transition = 1)
		{
			if (audioKey == nameof(QAudioType.BGM))
			{
				QSceneMusicSetting.UseSceneMusic = false;
				music.key = nameof(QAudioManager);
			}
			GetAudio(audioKey).Play(music,transition);
		}
		public static void UseSceneMusic(float transition = 1)
		{
			GetAudio(nameof(QAudioType.BGM)).Stop(transition);
			QSceneMusicSetting.UseSceneMusic = true;
			QSceneMusicSetting.ResetSceneMusic();
		}
		public static AudioMixerGroup GetMixer(string audioKey)
		{
			var group = AudioMixerGroups[audioKey];
			if (group == null)
			{
				group = QAudioSetting?.FindMatchingGroups(audioKey).Get(0);
				AudioMixerGroups[audioKey] = group;
			}
			return group;
		}
		public static void ChangeVolume(string audioKey, float value)
		{
			QAudioSetting?.SetFloat(audioKey, value <= 0 ? -80 : Mathf.Lerp(-20, 0, value));
		}
		internal static QAudioSource GetAudio(string audioKey)
		{
			if (AudioSources[audioKey] == null)
			{
				var audioPrefab = QAudioSourcePrefab.Load(audioKey);
				if (audioPrefab == null)
				{
					AudioSources[audioKey] = Instance.transform.GetChild(audioKey, true).GetComponent<QAudioSource>(true);
					if (System.Enum.TryParse<QAudioType>(audioKey, out var type))
					{
						AudioSources[audioKey].SetType(type);
					}
					else
					{
						AudioSources[audioKey].SetType(QAudioType.SE);
					}
				}
				else
				{
					AudioSources[audioKey] = Instantiate(audioPrefab, Instance.transform).GetComponent<QAudioSource>(true);
				}
				if (QAudioSetting != null && AudioSources[audioKey].Audio.outputAudioMixerGroup == null)
				{
					AudioSources[audioKey].Audio.outputAudioMixerGroup = GetMixer(audioKey);
				}
			}
			return AudioSources[audioKey];
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
		public float GetLength()
		{
			float length = 0;
			if (start != null)
			{
				length += start.length;
			}
			if (music != null)
			{
				length += music.length;
			}
			return length;
		}
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
