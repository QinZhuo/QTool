using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Unity.CompilationPipeline.Common.ILPostProcessing;
namespace QTool.Net
{
	public class QNetCodeGen : ILPostProcessor
	{
		public static void Log(object obj)
		{
			Console.WriteLine(obj);
		}
		public override ILPostProcessor GetInstance() => this;

		public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
		{
			throw new Exception("131dad");
		}

		public override bool WillProcess(ICompiledAssembly compiledAssembly)
		{
			bool relevant = compiledAssembly.Name == nameof(QTool) ||
							 compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == nameof(QTool));
			return false;
		}
	}
}
