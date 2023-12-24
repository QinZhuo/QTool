using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using QTool;
using System.IO;

public static class QPrefabStage
{
	
	public static PrefabStage CurrrentStage = null;
	public static bool IsPrefabMode => CurrrentStage != null;
	[InitializeOnLoadMethod]
	public static void Init()
	{
		PrefabStage.prefabStageOpened += OnPrefabStageOpened;
		PrefabStage.prefabStageClosing += OnPrefabStageClosing;
		SceneView.duringSceneGui += SceneGUI;
	}
	private static void OnPrefabStageOpened(PrefabStage prefabStage)
	{
		QEditorPath.Insert(prefabStage.assetPath);
		CurrrentStage = prefabStage;
	}
	private static void OnPrefabStageClosing(PrefabStage prefabStage)
	{
		if (CurrrentStage == prefabStage) CurrrentStage = null;
	}

	private static void SceneGUI(SceneView sceneView)
	{
		Handles.BeginGUI();
		GUILayout.FlexibleSpace();
		using (new GUILayout.HorizontalScope())
		{
			if (QEditorPath.PrefabPaths.Count > 0)
			{
				if (IsPrefabMode)
				{
					foreach (var path in QEditorPath.PrefabPaths.ToArray())
					{
						if (path == CurrrentStage.assetPath)
						{
							if (GUILayout.Button("【" + path.FileName() + "】", GUILayout.Height(20)))
							{
								StageUtility.GoBackToPreviousStage();
								PrefabStageUtility.OpenPrefab(path);
							}
						}
						else if (GUILayout.Button(path.FileName(), GUILayout.Height(20)))
						{
							
							PrefabStageUtility.OpenPrefab(path);
						}
					}
				}
				else
				{
					if (GUILayout.Button(""+ QEditorPath.PrefabPaths.QueuePeek().FileName().ToShortString(10), GUILayout.Width(130), GUILayout.Height(20)))
					{
						PrefabStageUtility.OpenPrefab(QEditorPath.PrefabPaths.QueuePeek());
					}
				}
			}
		}
		Handles.EndGUI();
	}

}
