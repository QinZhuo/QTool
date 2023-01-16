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
		[QName("背景乐"), SerializeField]
		private QBackgroundMusic defaultMusic = new QBackgroundMusic() { key = "默认" };
		[QName("事件音乐"),SerializeField]
		private List<QBackgroundMusic> eventMusic = new List<QBackgroundMusic>();

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
			defaultMusic.Play();
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
