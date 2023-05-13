using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using QTool.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	[QName("基础设置")]
	public class QToolSetting : InstanceScriptable<QToolSetting>
	{
#if Steamworks
		public uint SteamId=480;
#endif

		[QName("强制渲染比例"),Tooltip("只有挂载 "+nameof(QScreenAspect)+" 脚本的相机和UI会生效")]
		public float targetAspect = 16f/9f;
		[QName("支持Mod文件夹"),QEnum(nameof(GetModList))]
		public List<string> modeList = new List<string> { nameof(QTranslate.QTranslateData) };
		[QName("游戏数据邮箱")]
		public QMailAccount QAnalysisMail;
		public static List<string> GetModList()
		{
			List<string> pathList = new List<string>();
			(QDataList.ModPath+"/").CheckDirectoryPath();
			QDataList.ModPath.ForeachDirectory((path) =>
			{
				pathList.Add(path.SplitEndString(QDataList.ModPath+"/"));
			});
			return pathList;
		}
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
		[Range(0,100)]
		public int compressionQuality = 50;
#endif
		[QName("QNet网络同步速度")]
		[Range(5, 50)]
		public int qNetFrameSpeed=50;
		
		private void OnValidate()
		{
			QAnalysisMail?.Init();
		}
	}
}
