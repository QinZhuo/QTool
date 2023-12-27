using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{

	[ExecuteInEditMode]
	public class QDropdownSetting : MonoBehaviour
	{
		public Dropdown dropdown;
		protected void Awake()
		{
			dropdown = GetComponentInChildren<Dropdown>();
			var select = QPlayerPrefs.Get<string>(name);
			for (int i = 0; i < dropdown.options.Count; i++)
			{
				if (dropdown.options[i].text == select)
				{
					dropdown.value = i;
					break;
				}
			}
		}


		public void Set(int index)
		{
			QPlayerPrefs.Set(name, dropdown.options[index].text);
			QEventManager.InvokeEvent(nameof(QEventKey.设置更新), name);
		}

#if UNITY_EDITOR
		private void Reset()
		{
			dropdown.onValueChanged.RemoveAllListeners();
			UnityEditor.Events.UnityEventTools.AddPersistentListener(dropdown.onValueChanged, Set);
		}
#endif
	}
}

