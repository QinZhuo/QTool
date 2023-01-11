using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	public class QToolSetting : InstanceScriptable<QToolSetting>
	{
		/// <summary>
		/// 获取设置文件
		/// </summary>
		public static T GetSetting<T>(string name)where T: ScriptableObject
		{
			foreach (var setting in Instance.Settings)
			{
				if (setting == null) continue;
				if (setting.name == name)
				{
					return setting as T;
				}
			}
			return null;
		}
		public List<ScriptableObject> Settings = new List<ScriptableObject>();
		public QMailAccount QAnalysisMail;
		public string QAnalysisProject;
		[QEnum(nameof(GetModList))]
		public List<string> modeList = new List<string> { nameof(QTranslate.QTranslateData) };
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
		
		private void OnValidate()
		{
			QAnalysisMail?.Init();
		}
	}
}
