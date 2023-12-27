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

		}
		private void Reset()
		{
#if UNITY_EDITOR
			dropdown.onValueChanged.RemoveAllListeners();
			UnityEditor.Events.UnityEventTools.AddPersistentListener(dropdown.onValueChanged, Set);
#endif
		}
		public void Set(int index)
		{
			QPlayerPrefs.Set(name, dropdown.options[index].text);
			QEventManager.InvokeEvent(QEventKey.设置更新);
		}
	}
}

