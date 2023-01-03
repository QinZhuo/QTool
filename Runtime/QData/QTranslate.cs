using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QTool.Inspector;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace QTool
{
	public class QTranslateKey:IKey<string>
	{
		public string Key { get; set; }
		public string Name { get; set; }
		public string WebAPI { get; set; }
	}
	public class QTranslate : MonoBehaviour
	{
		public static QList<string, QTranslateKey> TranslateKeys = new QList<string, QTranslateKey>
		{
			new QTranslateKey
			{
				Key="schinese",
				Name="简体中文",
				WebAPI="zh-CN",
			},
			new QTranslateKey
			{
				Key="tchinese",
				Name="繁體中文",
				WebAPI="zh-TW",
			},
			new QTranslateKey
			{
				Key="english",
				Name="English",
				WebAPI="en",
			},
			new QTranslateKey
			{
				Key="japanese",
				Name="日本語",
				WebAPI="ja",
			},
			//new QTranslateKey
			//{
			//	Key="koreana",
			//	Name="한국어",
			//	WebAPI="ko",
			//},
		};
		static QDataList _languageData;
		
		public static QDataList QTranslateData =>_languageData??=GetQDataList();
		public static QDataList GetQDataList(string name=null)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = nameof(QTranslateData);
			}
			else
			{
				name = nameof(QTranslateData) + "/" + name;
			}
			return QDataList.GetResourcesData(name, () => {
				var data = new QDataList();
				List<string> titleList = new List<string>();
				titleList.Add("Key");
				foreach (var translateKey in TranslateKeys)  
				{
					titleList.Add(translateKey.Key);
				}
				data.SetTitles(titleList.ToArray());
				if (name == nameof(QTranslateData))
				{
					data["测试文本"].SetValue(GlobalLanguage, @"测试文
本123");
					data["测试文本"].SetValue("english", "test123");
				}
				return data;
			});
		}
		#region 基础数据

		[HideInInspector]
        public string curValue;
        [QName("文本")]
        [SerializeField]
        private string value;
        [SerializeField]
        [QName("固定翻译语言")]
        private string language = "";
        [SerializeField]
        [QName("翻译结果")]
        [QReadonly]
        private string translateResult = "";
		[QName("强制刷新翻译")]
		public bool forceUpdate;
        public string Language
        {
            get
            {
                return string.IsNullOrWhiteSpace(language) ? GlobalLanguage : language;
            }
        }
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                if (this.value != value||forceUpdate)
                {
                    this.value = value;
                    CheckFresh();
                }

            }
        }
        #endregion
        #region 全局翻译
		const string DefaultLanguage = "schinese";
		public static string GlobalLanguage { get; private set; } = DefaultLanguage;
		public static QTranslateKey GetTranslateKey(string key)
		{
			var data= TranslateKeys.Find((obj) =>
			{
				if (obj.Name == key)
				{
					return true;
				}
				else if(obj.Key==key)
				{
					return true;
				}
				else if(obj.WebAPI==key)
				{
					return true;
				}
				else if(key.SplitStartString("-")==obj.WebAPI)
				{
					return true;
				}
				return false;
			});
			if (data == null)
			{
				Debug.LogError("不支持语言[" + key + "] 默认 english");
				data = TranslateKeys["english"];
			}
			return data;
		}

		public static void ChangeGlobalLanguage(string value)
        {
			value = GetTranslateKey(value).Key;
			if (GlobalLanguage == value)
			{
				QEventManager.Trigger(nameof(QTranslate) + "_语言",GlobalLanguage);
            }
            else
            {
                GlobalLanguage = value;
				QEventManager.Trigger(nameof(QTranslate) + "_语言", GlobalLanguage);
            }
            QDebug.Log("文本语言：" + value);
        }
      //  static event System.Action OnLanguageChange;
        #endregion
        public StringEvent OnValueChange;
        public StringEvent OnTranslateChange;
        private void Awake()
        {
			QEventManager.Register<string>(nameof(QTranslate) + "_语言", FreshFont);
			QEventManager.Register<string>(nameof(QTranslate) + "_语言", CheckFresh);
		}
        private void Start()
		{
			FreshFont();
			CheckFresh();
        }
        private void OnDestroy()
        {
			QEventManager.UnRegister<string>(nameof(QTranslate) + "_语言", FreshFont);
			QEventManager.UnRegister<string>(nameof(QTranslate) + "_语言", CheckFresh);
        }

		public static QDictionary<string, Font> LanguageFont = new QDictionary<string, Font>();
		public static string Translate(string value,string language="")
        {
			if (string.IsNullOrEmpty(value)) { return value; }
			if (language.IsNullOrEmpty()) language = GlobalLanguage;
			value = value.Trim();
			var oldValue = value;
			value = TranslateKey(value, language);
			value= value.ForeachBlockValue('{', '}',(key)=> TranslateKey(key,language));
			if (oldValue == value && !QTranslateData.ContainsKey(oldValue))
			{
				Debug.LogWarning("缺少翻译[" + value + "][" + language + "]");
			}
            return value; 
        }
        public static QDictionary<string, string> KeyReplace = new QDictionary<string, string>();
        static string TranslateKey(string value, string language)
        {
			if (KeyReplace.ContainsKey(value))
			{
				return TranslateKey(KeyReplace[value], language);
			}
			else if (QTranslateData.ContainsKey(value)&& QTranslateData[value].HasValue(language))
            {
                var translate = QTranslateData[value].GetValue<string>(language);
				if (translate.IsNullOrEmpty()&&!QTranslateData[value].GetValue<string>(DefaultLanguage).IsNullOrEmpty())
				{
					Debug.LogWarning("缺少翻译[" + value + "]["+language+"]");
					return value;
				}
				else
				{
					return translate;
				}
			}
			return value;
        }
		private void OnEnable()
		{
			CheckFresh();
		}
		private void FreshFont(string key=default)
		{
			try
			{
				if (LanguageFont[key] != null)
				{
					foreach (var text in GetComponentsInChildren<Text>())
					{
						text.font = LanguageFont[key];
					}
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("字体更改出错"+e.ToShortString());
			}
		}
        private void CheckFresh(string key=null)
        {
            if (curValue != value)
            {
                curValue = value;
                OnValueChange?.Invoke(value);
            }
            try
            {
                translateResult = Translate(value,Language);
                OnTranslateChange?.Invoke(translateResult);
               
            }
            catch (System.Exception e)
            {
                Debug.LogError("翻译[" + value + "]出错" + e);
            }
        }
       
      

    }
}
