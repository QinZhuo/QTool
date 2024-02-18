using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using QTool;
using System.IO;

public static class QPrefabStage
{
	public static QHistoryList HistoryList = null; 
	public static PrefabStage CurrrentStage = null;
	public static bool IsPrefabMode => CurrrentStage != null;
	[InitializeOnLoadMethod]
	public static void Init()
	{ 
		HistoryList = new QHistoryList(nameof(QPrefabStage));
		PrefabStage.prefabStageOpened += OnPrefabStageOpened;
		PrefabStage.prefabStageClosing += OnPrefabStageClosing;
		SceneView.duringSceneGui += SceneGUI;
	}
	private static void OnPrefabStageOpened(PrefabStage prefabStage)
	{
		HistoryList.Add(prefabStage.assetPath); 
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
			if (HistoryList.List.Count > 0)
			{
				if (IsPrefabMode)
				{
					for (int i = HistoryList.List.Count - 1; i >= 0; i--)
					{
						var path = HistoryList.List[i];
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
					if (GUILayout.Button("" + HistoryList.List.StackPeek().FileName().ToShortString(10), GUILayout.Width(130), GUILayout.Height(20)))
					{
						PrefabStageUtility.OpenPrefab(HistoryList.List.StackPeek());
					}
				}
			}
		}
		Handles.EndGUI();
	}

}
