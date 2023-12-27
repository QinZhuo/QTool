using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{

	[RequireComponent(typeof(Dropdown)), ExecuteInEditMode]
	public class QDropdownSetting : MonoBehaviour
	{
		public Dropdown Dropdown { get; private set; }
		protected void Awake()
		{
			Dropdown = GetComponent<Dropdown>();

		}
		private void Reset()
		{
#if UNITY_EDITOR
			Dropdown.onValueChanged.RemoveAllListeners();
			UnityEditor.Events.UnityEventTools.AddPersistentListener(Dropdown.onValueChanged, Set);
#endif
		}
		public void Set(int index)
		{
			QPlayerPrefs.Set(name, Dropdown.options[index]);
			QEventManager.InvokeEvent(QEventKey.设置更新);
		}
	}
}

