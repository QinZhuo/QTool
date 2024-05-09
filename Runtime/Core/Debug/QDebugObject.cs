using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QDebugObject : MonoBehaviour
{
	private void Awake()
	{
#if !(DEVELOPMENT_BUILD || UNITY_EDITOR)
		gameObject.SetActive(false);
#endif
	}
}
