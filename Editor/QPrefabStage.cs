using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using QTool;
using System.IO;
public static class QPrefabStage
{
	public static List<string> PrefabPath = new List<string>();
	public static PrefabStage CurrrentStage = null;
	public static bool IsPrefabMode => CurrrentStage != null;
	[InitializeOnLoadMethod]
	public static void Init()
	{
		PrefabStage.prefabStageOpened += OnPrefabStageOpened;
		PrefabStage.prefabStageClosing += OnPrefabStageClosing;
		SceneView.duringSceneGui += SceneGUI;
		QPlayerPrefs.Get(nameof(PrefabPath), PrefabPath);
	}
	private static void OnPrefabStageOpened(PrefabStage prefabStage)
	{
		PrefabPath.Remove(prefabStage.assetPath);
		PrefabPath.Insert(0, prefabStage.assetPath);
		PrefabPath.RemoveAll(path => PrefabPath.IndexOf(path) > 8);
		QPlayerPrefs.Set(nameof(PrefabPath), PrefabPath);
		CurrrentStage = prefabStage;
	}
	private static void OnPrefabStageClosing(PrefabStage prefabStage)
	{
		if (CurrrentStage == prefabStage) CurrrentStage = null;
	}

	private static void SceneGUI(SceneView sceneView)
	{
		Handles.BeginGUI();
		using (new GUILayout.HorizontalScope())
		{
			if (PrefabPath.Count > 0)
			{
				if (IsPrefabMode)
				{
					foreach (var path in PrefabPath.ToArray())
					{
						if (path == CurrrentStage.assetPath) continue;
						if(GUILayout.Button(path.FileName(true)))
						{
							PrefabStageUtility.OpenPrefab(path);
						}
					}
				}
				else
				{
					if(GUILayout.Button(PrefabPath.QueuePeek().FileName(true),GUILayout.Width(100)))
					{
						PrefabStageUtility.OpenPrefab(PrefabPath.QueuePeek());
					}
				}
			}
		}
		Handles.EndGUI();
	}

}
