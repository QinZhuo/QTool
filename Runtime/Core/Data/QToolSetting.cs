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
	public class QToolSetting : QSingletonScriptable<QToolSetting>
	{
#if Steamworks
		public uint SteamId = 480;
#endif
		[QName(nameof(QKeyColor) + "颜色")]
		public List<QKeyColorValue> qKeyColorList = new List<QKeyColorValue>();
		[QName(nameof(QLocalization) + "字体")]
		public List<QLocalizationFont> qLocalizationFontList = new List<QLocalizationFont>();
		[QName("默认饱和度")]
		public float DefualtColorSaturation = 0.6f;
		[QName("默认亮度")]
		public float DefualtColorValue = 1;
		
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

		public static List<QKeyColorValue> ColorList => Instance.qKeyColorList;
	}
	

	[System.Serializable]
	public struct QKeyColorValue : IKey<string>
	{
		public string Key { get => key; set => key = value; }
		public string key;
		public Color color;
	}

}
