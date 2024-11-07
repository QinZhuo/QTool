using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTool
{
	//public class QLoactionWindow : EditorWindow
	//{
	//	public SystemLanguage From = SystemLanguage.ChineseSimplified;
	//	public List<SystemLanguage> ToList = new List<SystemLanguage>() { SystemLanguage.English };
	//	[MenuItem("QTool/窗口/翻译")]
	//	public static void OpenWindow()
	//	{
	//		var window = GetWindow<QLoactionWindow>();
	//		window.minSize = new Vector2(400, 300);
	//	}
	//	private void CreateGUI()
	//	{
	//		QPlayerPrefs.Get(nameof(QLoactionWindow) + "_" + nameof(ToList), ToList);
	//		Label toText = null;
	//		rootVisualElement.AddEnum("当前语言", From, value => From = (SystemLanguage)value);
	//		var fromText = rootVisualElement.AddText("", "", null, true);

	//		rootVisualElement.Add("目标语言", ToList, typeof(List<SystemLanguage>), newList =>
	//		{
	//			ToList = (List<SystemLanguage>)newList;
	//			QPlayerPrefs.Set(nameof(QLoactionWindow) + "_" + nameof(ToList), ToList);
	//		});
	//		toText = rootVisualElement.AddLabel("");
	//		rootVisualElement.AddButton("翻译并添加本地化", async () =>
	//		{
	//			toText.text = "";
	//			if (!fromText.text.IsNull())
	//			{
	//				var fromDataList = QLocalizationData.LoadQDataList(From.ToString());
	//				var key = fromText.text;
	//				var fromData = fromDataList[key];
	//				fromData[TitleKey] = fromText.text;
	//				foreach (var To in ToList)
	//				{
	//					if (From == To) continue;
	//					var toDataList = QLocalizationData.LoadQDataList(To.ToString());
	//					await CheckTranslate(fromData, toDataList, To);
	//					toText.text += toDataList[key][TitleKey];
	//					toDataList.Save();
	//				}
	//				fromDataList.Save();
	//				GUIUtility.systemCopyBuffer = toText.text;
	//				AssetDatabase.Refresh();
	//				QLocalizationData.Fresh();
	//			}
	//		});
	//		rootVisualElement.AddButton("翻译全部" + nameof(QLocalization) + "本地化信息", QLocalizationTranslate);
	//	}
	//	public async void QLocalizationTranslate()
	//	{
	//		var fromDataList = QLocalizationData.LoadQDataList(From.ToString());
	//		foreach (var To in ToList)
	//		{
	//			if (From == To) continue;
	//			var toDataList = QLocalizationData.LoadQDataList(To.ToString());
	//			foreach (var data in fromDataList)
	//			{
	//				await CheckTranslate(data, toDataList, To);
	//			}
	//			toDataList.Save();
	//		}
	//		QDebug.Log("翻译完成");
	//		AssetDatabase.Refresh();
	//		QLocalizationData.Fresh();
	//	}
	//	private const string TitleKey = nameof(QLocalizationData.Localization);
	//	private async Task CheckTranslate(QDataRow fromData, QDataList toDataList, SystemLanguage toLanguage)
	//	{
	//		var toDataValue = toDataList[fromData.Key][TitleKey];
	//		if (!fromData[TitleKey].IsNull() && (toDataValue.IsNull() || toDataValue.EndsWith(QLocalizationData.AutoTranslateEndKey)))
	//		{
	//			toDataList[fromData.Key][TitleKey] = await fromData[TitleKey].NetworkTranslateAsync(From, toLanguage) + QLocalizationData.AutoTranslateEndKey;
	//			QDebug.Log("翻译[" + fromData[TitleKey] + "] => " + toLanguage + " => [" + toDataList[fromData.Key][TitleKey] + "]");
	//			await Task.Delay(100);
	//		}
	//	}
	//}
}
