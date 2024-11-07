#if UNITY_EDITOR
using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;
namespace QTool.Graph {
	[ScriptedImporter(1, QGraph.ext)]
	[ExcludeFromPreset]
	public class QGraphImporter : ScriptedImporter {
		public override void OnImportAsset(AssetImportContext ctx) {
			var asset = new TextAsset(File.ReadAllText(ctx.assetPath));
			ctx.AddObjectToAsset(nameof(TextAsset), asset);
			ctx.SetMainObject(asset);
		}
	}
}
#endif