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
		public Dropdown dropdown;
#if TMPro
		public TMP_Dropdown TMP_Dropdown;
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
				else
#endif
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

			}
		}
		public void Set(int index)
		{
			var value = dropdown.options[index].text;
			QPlayerPrefs.Set(name, value);
			QDebug.Log(name + ":[" + value + "]");
			QEventManager.InvokeEvent(nameof(QEventKey.设置更新), name);
		}
#if UNITY_EDITOR
		private void Reset()
		{
			dropdown = GetComponentInChildren<Dropdown>();
			dropdown.onValueChanged.AddPersistentListener(Set);
#if TMPro
			TMP_Dropdown = GetComponentInChildren<TMP_Dropdown>();
			TMP_Dropdown.onValueChanged.AddPersistentListener(Set);
#endif
		}
#endif
	}
}

