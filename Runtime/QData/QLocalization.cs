using UnityEngine;
namespace QTool
{
	public class QLocalization : MonoBehaviour
	{
		#region 基础数据
		[QName,QPopup(nameof(QLocalizationData) +"."+nameof(QLocalizationData.List)), SerializeField]
		private string m_key;
		[QName("本地化"), SerializeField, QReadonly]
		private string m_localization = "";
		public string Key
		{
			get
			{
				return m_key;
			}
			set
			{
				if (m_key != value)
				{
					this.m_key = value;
					OnKeyChange?.Invoke(m_key);
				}
				Fresh();
			}
		}
		#endregion
		public StringEvent OnKeyChange;
		public StringEvent OnLocalizationChange;
		private void Awake()
		{
			OnLanguageChange += Fresh;
		}
		private void Start()
		{
			Fresh();
		}
		private void OnDestroy()
		{
			OnLanguageChange -= Fresh;
		}
		private void Fresh()
		{
			try
			{
				m_localization = Get(m_key);
				OnLocalizationChange?.Invoke(m_localization);
			}
			catch (System.Exception e)
			{
				Debug.LogError("翻译[" + m_key + "]出错" + e);
			}
		}
		#region 静态方法
		public static SystemLanguage Language
		{
			get => QPlayerPrefs.Get(nameof(Language), SystemLanguage.ChineseSimplified);
			set
			{
				if (value != Language)
				{
					QPlayerPrefs.Set(nameof(Language), value);
					QLocalizationData.FreshList(value.ToString());
					OnLanguageChange?.Invoke();
				}
			}
		}
		public static event System.Action OnLanguageChange = null;
		public static string Get(string value)
		{
			if (value.IsNull()) { return value; }
			value = value.Trim();
			var oldValue = value;
			value = TranslateKey(value);
			value = value.ForeachBlockValue('{', '}', (key) => TranslateKey(key));
			if (oldValue == value && !QLocalizationData.ContainsKey(oldValue))
			{
				QDebug.LogWarning("缺少翻译[" + value + "][" + Language + "]");
			}
			return value;
		}
		public static QDictionary<string, string> KeyReplace = new QDictionary<string, string>();
		static string TranslateKey(string value)
		{
			if (KeyReplace.ContainsKey(value))
			{
				return TranslateKey(KeyReplace[value]);
			}
			else if (QLocalizationData.ContainsKey(value))
			{
				return QLocalizationData.Get(value).Localization;
			}
			return value;
		}
		#endregion
	}
	public class QLocalizationData : QDataList<QLocalizationData>
	{
		[QName]
		public string Localization { get; private set; }
		static QLocalizationData()
		{
			FreshList(nameof(SystemLanguage.ChineseSimplified));
		}
	}
}
