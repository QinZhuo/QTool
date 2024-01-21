using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace QTool
{
	public class QLocalization : MonoBehaviour
	{
		#region 基础数据
		[QName, QPopup(nameof(QLocalizationData) + "." + nameof(QLocalizationData.List)), SerializeField]
		private string key;
		[QName("本地化"), SerializeField, QReadonly]
		private string localization = "";
		public string Key
		{
			get
			{
				return key;
			}
			set
			{
				if (key != value)
				{
					key = value;
					OnKeyChange?.Invoke(key);
				}
				FreshLocalization();
			}
		}
		#endregion
		public StringEvent OnKeyChange = new StringEvent();
		public StringEvent OnLocalizationChange = new StringEvent();
		private void Awake()
		{
			QLocalizationData.OnLanguageChange += FreshFont;
			QLocalizationData.OnLanguageChange += FreshLocalization;
		}
		private void Reset()
		{
			var text = GetComponentInChildren<Text>();
			if (text != null)
			{
				OnLocalizationChange.AddPersistentListener(text.GetAction<string>("set_text"));
			}
		}
		private void Start()
		{
			FreshFont();
			FreshLocalization();
		}
		private void OnDestroy()
		{
			QLocalizationData.OnLanguageChange -= FreshFont;
			QLocalizationData.OnLanguageChange -= FreshLocalization;
		}
		protected virtual void FreshFont()
		{
			if (!QLocalizationData.FontInfo.IsNull())
			{
				var count = OnLocalizationChange.GetPersistentEventCount();
				for (int i = 0; i < count; i++)
				{
					if (OnLocalizationChange.GetPersistentTarget(i) is Text text)
					{
						text.font = QLocalizationData.FontInfo.font;
					}
				}
			}
		}
		private void FreshLocalization()
		{
			if (key.IsNull()) return;
			localization = QLocalizationData.GetLozalization(key);
			OnLocalizationChange?.Invoke(localization);
		}
#if UNITY_EDITOR
		private void OnValidate()
		{
			FreshLocalization();
		}
#endif
	}
	public class QLocalizationData : QDataList<QLocalizationData>
	{
		public static string GetLozalization(string key)
		{
			if (key.IsNull()) return key;
			List<object> Params = null;
			if (key.Contains(FormatStart))
			{
				Params = new List<object>();
				key = key.ForeachBlockValue(FormatStart, FormatEnd, key =>
				{
					Params.Add(key);
					return "{" + (Params.Count - 1) + "}";
				});
			}
			if (ContainsKey(key))
			{
				var text = Get(key).Localization;
				if (text.EndsWith(AutoTranslateEndKey))
				{
					text = text.Substring(0, text.Length - AutoTranslateEndKey.Length);
				}
				if (Params != null)
				{
					text = string.Format(text, Params.ToArray());
				}
				text = text.ForeachBlockValue('{', '}', subKey =>
				{
					if (ContainsKey(subKey))
					{
						return GetLozalization(subKey);
					}
					else
					{
						return "{" + subKey + "}";
					}
				});
				return text;
			}
			else
			{
				QDebug.LogWarning("缺少翻译[" + key + "]" + Language);
			}
			return key;
		}
		public const string AutoTranslateEndKey = " [Auto]";
		public const char FormatStart = '[';
		public const char FormatEnd = ']';
		[QName]
		public string Localization { get; private set; }
		static QLocalizationData()
		{
			Fresh();
			QEventManager.Register<string>(nameof(QEventKey.设置更新), Fresh);
		}
		public static void Fresh(string key = nameof(Language))
		{
			if (key == nameof(Language))
			{
				Language = QPlayerPrefs.Get(nameof(Language), Application.systemLanguage);
			}
		}
		private static SystemLanguage _Language = SystemLanguage.Unknown;
		public static QLocalizationFont FontInfo { get; private set; } = default;
		public static SystemLanguage Language
		{
			get => _Language;
			set
			{
				if (Language != value)
				{
					_Language = value;
					QPlayerPrefs.Set(nameof(Language), value);
					Load(value.ToString());
					if (List.Count == 0 && Language != SystemLanguage.English)
					{
						QDebug.LogError("不存在语言[" + value + "]转为英语");
						Language = SystemLanguage.English;
						return;
					}
					FontInfo = QToolSetting.Instance.qLocalizationFontList.Get(Language);
					QDebug.Log(nameof(QLocalization) + " : " + value);
					OnLanguageChange?.Invoke();
				}
			}
		}
		public static event System.Action OnLanguageChange = null;
	}
	#region Tool
	public static class QLocalizationTool
	{
		public static string ToLozalizationKey(this string key, params object[] Params)
		{
			for (int i = 0; i < Params.Length; i++)
			{
				Params[i] = QLocalizationData.FormatStart + Params[i]?.ToString() + QLocalizationData.FormatEnd;
			}
			return string.Format(key, Params);
		}
		public static string ToLozalizationColorKey(this object key)
		{
			return ("{" + key + "}").ToColorString(key.ToColor());
		}
		const string NetworkTranslateURL = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={2}&tl={1}&dt=t&q={0}";

		static List<List<List<string>>> translateData = new List<List<List<string>>>();
		public static async Task<string> NetworkTranslateAsync(this string text, QLocalizationCode fromLanguage, QLocalizationCode toLanguage)
		{
			if (text.IsNull()) return text;
			try
			{
				var jsonStr = await QTool.RunURLAsync(string.Format(NetworkTranslateURL, text, toLanguage, fromLanguage));
				jsonStr.ParseQData(translateData);
				text = translateData[0][0][0];
			}
			catch (System.Exception e)
			{
				Debug.LogError("翻译出错[" + text + "] \n" + e);
			}
			return text;
		}
		public static async Task<string> NetworkTranslateAsync(this string chineseText, SystemLanguage from = SystemLanguage.English, SystemLanguage to = SystemLanguage.ChineseSimplified)
		{
			return await chineseText.NetworkTranslateAsync(from.ToCode(), to.ToCode());
		}
		public static QLocalizationCode ToCode(this SystemLanguage language)
		{
			switch (language)
			{
				case SystemLanguage.Chinese:
					return QLocalizationCode.zh_CN;
				case SystemLanguage.English:
					return QLocalizationCode.en;
				case SystemLanguage.French:
					return QLocalizationCode.fr;
				case SystemLanguage.Japanese:
					return QLocalizationCode.ja_JP;
				case SystemLanguage.Korean:
					return QLocalizationCode.ko_KR;
				case SystemLanguage.ChineseSimplified:
					return QLocalizationCode.zh_CN;
				case SystemLanguage.ChineseTraditional:
					return QLocalizationCode.zh_HK;
				default:
					break;
			}
			return QLocalizationCode.en;
		}
		public static string ToCodeString(this QLocalizationCode code)
		{
			return code.ToString().Replace('_', '-').TrimStart('-');
		}

	}
	[System.Serializable]
	public struct QLocalizationFont : IKey<SystemLanguage>
	{
		public SystemLanguage Key { get => language; set => language = value; }
		public SystemLanguage language;
		public Font font;
	}
	public enum QLocalizationCode
	{
		af,//南非语
		af_ZA,// 南非语
		ar,//阿拉伯语
		ar_AE,// 阿拉伯语(阿联酋)
		ar_BH,// 阿拉伯语(巴林)
		ar_DZ,// 阿拉伯语(阿尔及利亚)
		ar_EG,// 阿拉伯语(埃及)
		ar_IQ,// 阿拉伯语(伊拉克)
		ar_JO,// 阿拉伯语(约旦)
		ar_KW,// 阿拉伯语(科威特)
		ar_LB,// 阿拉伯语(黎巴嫩)
		ar_LY,// 阿拉伯语(利比亚)
		ar_MA,// 阿拉伯语(摩洛哥)
		ar_OM,// 阿拉伯语(阿曼)
		ar_QA,// 阿拉伯语(卡塔尔)
		ar_SA,// 阿拉伯语(沙特阿拉伯)
		ar_SY,// 阿拉伯语(叙利亚)
		ar_TN,// 阿拉伯语(突尼斯)
		ar_YE,// 阿拉伯语(也门)
		az,//阿塞拜疆语
		az_AZ,// 阿塞拜疆语(拉丁文)
		be,//比利时语
		be_BY,// 比利时语
		bg,//保加利亚语
		bg_BG,// 保加利亚语
		bs_BA,// 波斯尼亚语(拉丁文，波斯尼亚和黑塞哥维那)
		ca,//加泰隆语
		ca_ES,// 加泰隆语
		cs,//捷克语
		cs_CZ,// 捷克语
		cy,//威尔士语
		cy_GB,// 威尔士语
		da,//丹麦语
		da_DK,// 丹麦语
		de,//德语
		de_AT,// 德语(奥地利)
		de_CH,// 德语(瑞士)
		de_DE,// 德语(德国)
		de_LI,// 德语(列支敦士登)
		de_LU,// 德语(卢森堡)
		dv,//第维埃语
		dv_MV,// 第维埃语
		el,//希腊语
		el_GR,// 希腊语
		en,//英语
		en_AU,// 英语(澳大利亚)
		en_BZ,// 英语(伯利兹)
		en_CA,// 英语(加拿大)
		en_CB,// 英语(加勒比海)
		en_GB,// 英语(英国)
		en_IE,// 英语(爱尔兰)
		en_JM,// 英语(牙买加)
		en_NZ,// 英语(新西兰)
		en_PH,// 英语(菲律宾)
		en_TT,// 英语(特立尼达)
		en_US,// 英语(美国)
		en_ZA,// 英语(南非)
		en_ZW,// 英语(津巴布韦)
		eo,//世界语
		es,//西班牙语
		es_AR,// 西班牙语(阿根廷)
		es_BO,// 西班牙语(玻利维亚)
		es_CL,// 西班牙语(智利)
		es_CO,// 西班牙语(哥伦比亚)
		es_CR,// 西班牙语(哥斯达黎加)
		es_DO,// 西班牙语(多米尼加共和国)
		es_EC,// 西班牙语(厄瓜多尔)
		es_ES,// 西班牙语(传统)
		es_GT,// 西班牙语(危地马拉)
		es_HN,// 西班牙语(洪都拉斯)
		es_MX,// 西班牙语(墨西哥)
		es_NI,// 西班牙语(尼加拉瓜)
		es_PA,// 西班牙语(巴拿马)
		es_PE,// 西班牙语(秘鲁)
		es_PR,// 西班牙语(波多黎各(美))
		es_PY,// 西班牙语(巴拉圭)
		es_SV,// 西班牙语(萨尔瓦多)
		es_UY,// 西班牙语(乌拉圭)
		es_VE,// 西班牙语(委内瑞拉)
		et,//爱沙尼亚语
		et_EE,// 爱沙尼亚语
		eu,//巴士克语
		eu_ES,// 巴士克语
		fa,//法斯语
		fa_IR,// 法斯语
		fi,//芬兰语
		fi_FI,// 芬兰语
		fo,//法罗语
		fo_FO,// 法罗语
		fr,//法语
		fr_BE,// 法语(比利时)
		fr_CA,// 法语(加拿大)
		fr_CH,// 法语(瑞士)
		fr_FR,// 法语(法国)
		fr_LU,// 法语(卢森堡)
		fr_MC,// 法语(摩纳哥)
		gl,//加里西亚语
		gl_ES,// 加里西亚语
		gu,//古吉拉特语
		gu_IN,// 古吉拉特语
		he,//希伯来语
		he_IL,// 希伯来语
		hi,//印地语
		hi_IN,// 印地语
		hr,//克罗地亚语
		hr_BA,// 克罗地亚语(波斯尼亚和黑塞哥维那)
		hr_HR,// 克罗地亚语
		hu,//匈牙利语
		hu_HU,// 匈牙利语
		hy,//亚美尼亚语
		hy_AM,// 亚美尼亚语
		id,//印度尼西亚语
		id_ID,// 印度尼西亚语
		_is,//冰岛语
		is_IS,// 冰岛语
		it,//意大利语
		it_CH,// 意大利语(瑞士)
		it_IT,// 意大利语(意大利)
		ja,//日语
		ja_JP,// 日语
		ka,//格鲁吉亚语
		ka_GE,// 格鲁吉亚语
		kk,//哈萨克语
		kk_KZ,// 哈萨克语
		kn,//卡纳拉语
		kn_IN,// 卡纳拉语
		ko,//朝鲜语
		ko_KR,// 朝鲜语
		kok,// 孔卡尼语
		kok_IN,//孔卡尼语
		ky,//吉尔吉斯语
		ky_KG,// 吉尔吉斯语(西里尔文)
		lt,//立陶宛语
		lt_LT,// 立陶宛语
		lv,//拉脱维亚语
		lv_LV,// 拉脱维亚语
		mi,//毛利语
		mi_NZ,// 毛利语
		mk,//马其顿语
		mk_MK,// 马其顿语(FYROM)
		mn,//蒙古语
		mn_MN,// 蒙古语(西里尔文)
		mr,//马拉地语
		mr_IN,// 马拉地语
		ms,//马来语
		ms_BN,// 马来语(文莱达鲁萨兰)
		ms_MY,// 马来语(马来西亚)
		mt,//马耳他语
		mt_MT,// 马耳他语
		nb,//挪威语(伯克梅尔)
		nb_NO,// 挪威语(伯克梅尔)(挪威)
		nl,//荷兰语
		nl_BE,// 荷兰语(比利时)
		nl_NL,// 荷兰语(荷兰)
		nn_NO,// 挪威语(尼诺斯克)(挪威)
		ns,//北梭托语
		ns_ZA,// 北梭托语
		pa,//旁遮普语
		pa_IN,// 旁遮普语
		pl,//波兰语
		pl_PL,// 波兰语
		pt,//葡萄牙语
		pt_BR,// 葡萄牙语(巴西)
		pt_PT,// 葡萄牙语(葡萄牙)
		qu,//克丘亚语
		qu_BO,// 克丘亚语(玻利维亚)
		qu_EC,// 克丘亚语(厄瓜多尔)
		qu_PE,// 克丘亚语(秘鲁)
		ro,//罗马尼亚语
		ro_RO,// 罗马尼亚语
		ru,//俄语
		ru_RU,// 俄语
		sa,//梵文
		sa_IN,// 梵文
		se,//北萨摩斯语
		se_FI,// 北萨摩斯语(芬兰)
		se_NO,// 北萨摩斯语(挪威)
		se_SE,// 北萨摩斯语(瑞典)
		sk,//斯洛伐克语
		sk_SK,// 斯洛伐克语
		sl,//斯洛文尼亚语
		sl_SI,// 斯洛文尼亚语
		sq,//阿尔巴尼亚语
		sq_AL,// 阿尔巴尼亚语
		sr_BA,// 塞尔维亚语(拉丁文，波斯尼亚和黑塞哥维那)
		sr_SP,// 塞尔维亚(拉丁)
		sv,//瑞典语
		sv_FI,// 瑞典语(芬兰)
		sv_SE,// 瑞典语
		sw,//斯瓦希里语
		sw_KE,// 斯瓦希里语
		syr,// 叙利亚语
		syr_SY,//叙利亚语
		ta,//泰米尔语
		ta_IN,// 泰米尔语
		te,//泰卢固语
		te_IN,// 泰卢固语
		th,//泰语
		th_TH,// 泰语
		tl,//塔加路语
		tl_PH,// 塔加路语(菲律宾)
		tn,//茨瓦纳语
		tn_ZA,// 茨瓦纳语
		tr,//土耳其语
		tr_TR,// 土耳其语
		ts,//宗加语
		tt,//鞑靼语
		tt_RU,// 鞑靼语
		uk,//乌克兰语
		uk_UA,// 乌克兰语
		ur,//乌都语
		ur_PK,// 乌都语
		uz,//乌兹别克语
		uz_UZ,// 乌兹别克语(拉丁文)
		vi,//越南语
		vi_VN,// 越南语
		xh,//班图语
		xh_ZA,// 班图语
		zh,//中文
		zh_CN,// 中文(简体)
		zh_HK,// 中文(香港)
		zh_MO,// 中文(澳门)
		zh_SG,// 中文(新加坡)
		zh_TW,// 中文(繁体)
		zu,//祖鲁语
		zu_ZA,// 祖鲁语
	}
	#endregion
}
