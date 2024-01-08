using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{

	[ExecuteInEditMode]
	public class QDropdownSettingUI : MonoBehaviour
	{
		public Dropdown dropdown;
		protected void Awake()
		{
			dropdown = GetComponentInChildren<Dropdown>();
			if (Application.isPlaying)
			{
				var select = QPlayerPrefs.Get<string>(name)?.Trim('"');
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
			dropdown.onValueChanged.AddPersistentListener(Set);
		}
#endif
	}
}

