using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace QTool.FlowGraph
{
	[ScriptedImporter(1, ".qflowgraph")]
	[ExcludeFromPreset]
	public class QFlowGraphImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			string text = File.ReadAllText(ctx.assetPath);
			var graph = text.ParseQData<QFlowGraph>();
			var asset = ObjectFactory.CreateInstance<QFlowGraphAsset>();
			asset.Graph = graph;
			ctx.AddObjectToAsset("main", asset);
			ctx.SetMainObject(asset);
		}
	}
}