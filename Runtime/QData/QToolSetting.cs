using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using QTool.Reflection;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	[QName("基础设置")]
	public class QToolSetting : InstanceScriptable<QToolSetting>
	{
#if Steamworks
		public uint SteamId = 480;
#endif
		[QName(nameof(QKeyColor) + "颜色")]
		public List<QKeyColorValue> qKeyColorList = new List<QKeyColorValue>();
		[QName("强制分辨率比例"), Tooltip("只有挂载 " + nameof(QScreenAspect) + " 脚本的相机和UI会生效")]
		public float targetAspect = 16f / 9f;
		[QName("Mod文件夹")]
		public List<string> modeList = new List<string> { nameof(QLocalizationData) };
		[QName("游戏数据邮箱")]
		public QMailAccount QAnalysisMail;
		[QName("QNet网络同步速度")]
		[Range(5, 50)]
		public int qNetFrameSpeed = 50;
#if UNITY_EDITOR
		[QName("音频强制单声道")]
		public bool forceToMono = true;
		[QName("音频导入设置(时长顺序为 [1s,3s,长音频])")]
		public AudioImporterSampleSettings[] audioImporterSettings = new AudioImporterSampleSettings[]
		{
			new AudioImporterSampleSettings
			{
				loadType= AudioClipLoadType.DecompressOnLoad,
				compressionFormat= AudioCompressionFormat.ADPCM,
				quality=0.8f,
			},
			new AudioImporterSampleSettings
			{
				loadType= AudioClipLoadType.CompressedInMemory,
				compressionFormat= AudioCompressionFormat.Vorbis,
				quality=0.8f,
			}
			,
			new AudioImporterSampleSettings
			{
				loadType= AudioClipLoadType.Streaming,
				compressionFormat= AudioCompressionFormat.Vorbis,
				quality=0.8f,
			}
		};
		[QName("图片压缩质量")]
		[Range(0, 100)]
		public int compressionQuality = 50;
#endif
		private void OnValidate()
		{
			QAnalysisMail?.Init();
		}
	}
	[System.Serializable]
	public class QKeyColorList : QList<string, QKeyColorValue>
	{

	}
	[System.Serializable]
	public struct QKeyColorValue : IKey<string>
	{
		public string Key { get => key; set => value = key; }
		public string key;
		public Color color;
	}
}
