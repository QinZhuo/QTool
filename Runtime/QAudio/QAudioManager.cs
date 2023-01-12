using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace QTool
{

	public class QAudioManager : QToolManagerBase<QAudioManager>
	{
		static QDictionary<string, AudioSource> AudioSources = new QDictionary<string, AudioSource>();
		AudioSource BGM = null;
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
		public static void Play(AudioClip clip,string soundKey=nameof(QAudioType.SE))
		{
			var audio= GetAudioSource(soundKey);
			audio.PlayOneShot(clip);
		}
		public static AudioSource GetAudioSource(string soundKey = nameof(QAudioType.SE))
		{
			if (AudioSources[soundKey] == null)
			{
				var audioPrefab = Resources.Load<GameObject>(nameof(QAudioType) + "/" + soundKey);
				if (audioPrefab == null)
				{
					AudioSources[soundKey] = Instance.transform.GetChild(soundKey, true).GetComponent<AudioSource>(true);
				}
				else
				{
					AudioSources[soundKey] = Instantiate(audioPrefab, Instance.transform).GetComponent<AudioSource>(true);
				}
				if (QAudioSetting != null && AudioSources[soundKey].outputAudioMixerGroup == null)
				{
					AudioSources[soundKey].outputAudioMixerGroup = QAudioSetting.audioMixer.FindMatchingGroups(soundKey).Get(0);
				}
			}
			return AudioSources[soundKey];
		}
	}
	public enum QAudioType
	{
		/// <summary>
		/// 背景音乐
		/// </summary>
		BGM,
		/// <summary>
		/// 环境音
		/// </summary>
		BGS,
		/// <summary>
		/// 音效
		/// </summary>
		SE,
		/// <summary>
		/// 音乐效果
		/// </summary>
		ME,
	}
}
