using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if TMPro
using TMPro;
#endif
namespace QTool
{

	[ExecuteInEditMode]
	public class QDropdownSettingUI : MonoBehaviour
	{
#if TMPro
		public TMP_Dropdown TMP_Dropdown;
#else
		public Dropdown dropdown;
#endif
		protected void Awake()
		{

			if (Application.isPlaying)
			{
				var select = QPlayerPrefs.Get<string>(name)?.Trim('"');
#if TMPro
				if (TMP_Dropdown != null)
				{
					for (int i = 0; i < TMP_Dropdown.options.Count; i++)
					{
						if (TMP_Dropdown.options[i].text == select)
						{
							TMP_Dropdown.value = i;
							break;
						}
					}
				}
#else
				if (dropdown != null)
				{
					for (int i = 0; i < dropdown.options.Count; i++)
					{
						if (dropdown.options[i].text == select)
						{
							dropdown.value = i;
							break;
						}
					}
				}
#endif
			}
		}
		public void Set(int index)
		{
#if TMPro
			var value = TMP_Dropdown.options[index].text;
#else
			var value = dropdown.options[index].text;
#endif
			QPlayerPrefs.Set(name, value);
			QDebug.Log(name + ":[" + value + "]");
			QEventManager.InvokeEvent(nameof(QEventKey.设置更新), name);
		}
		private void Reset()
		{
#if TMPro
			TMP_Dropdown = GetComponentInChildren<TMP_Dropdown>();
			TMP_Dropdown.onValueChanged.AddPersistentListener(Set);
#else
			dropdown = GetComponentInChildren<Dropdown>();
			dropdown.onValueChanged.AddPersistentListener(Set);
#endif
		}
	}
}

