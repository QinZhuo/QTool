using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace QTool.FlowGraph
{
    [ScriptedImporter(1, ".qfg")]
    public class QFGImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var qfg= ScriptableObject.CreateInstance<QFlowGraphAsset>();
			qfg.Init(File.ReadAllText(ctx.assetPath));
			ctx.AddObjectToAsset(nameof(qfg), qfg); 
            ctx.SetMainObject(qfg);
			qfg.Graph.Name = Path.GetFileName(ctx.assetPath);
		}
    }
}
