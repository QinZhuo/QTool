using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.CompilationPipeline.Common.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using QTool.Net;

namespace QTool.CodeGen
{
	public class QNetCodeGen : ILPostProcessor
	{
		public const string QNet = nameof(QNet);
		public const string QSyncAction_ = nameof(QSyncAction_);
		public const string IgnoreDefine = "IGNORE_COCDEGEN";
		public const string GenNamespace = "QTool.Net";
		public const string GenClass = nameof(GenClass);
		public static List<DiagnosticMessage> Logs = new List<DiagnosticMessage>(); 
		public static AssemblyDefinition Assembly;

		public static void Log(object obj, DiagnosticType type = DiagnosticType.Warning) 
		{
			Logs.Add(new DiagnosticMessage { DiagnosticType = type, MessageData = nameof(QNetCodeGen) + "  " + obj?.ToString() });
		}
		public override ILPostProcessor GetInstance() => this;
		public override bool WillProcess(ICompiledAssembly compiledAssembly)
		{
			bool relevant = compiledAssembly.Name == QNet ||
							 compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == QNet);
			bool ignore = compiledAssembly.HasDefine(IgnoreDefine);
			return relevant && !ignore;
		}
		public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
		{
			using (var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData))
			using (var pdbStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData))
			using (var asmResolver = new QAssemblyResolver(compiledAssembly))
			using (var assembly = ReadAssembly(peStream, pdbStream, asmResolver))
			{
				if (Weave(asmResolver))
				{
					if (assembly.MainModule.AssemblyReferences.Any(r => r.Name == assembly.Name.Name))
					{
						assembly.MainModule.AssemblyReferences.Remove(assembly.MainModule.AssemblyReferences.First(r => r.Name == assembly.Name.Name));
					}
					var peOut = new MemoryStream();
					var pdbOut = new MemoryStream();
					WriterParameters writerParameters = new WriterParameters
					{
						SymbolWriterProvider = new PortablePdbWriterProvider(),
						SymbolStream = pdbOut,
						WriteSymbols = true
					};
					assembly.Write(peOut, writerParameters);
					var inMemory = new InMemoryAssembly(peOut.ToArray(), pdbOut.ToArray());
					return new ILPostProcessResult(inMemory, Logs);
				}
			}
			return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, Logs);
		}
		public AssemblyDefinition ReadAssembly(MemoryStream peStream, MemoryStream pdbStream, QAssemblyResolver asmResolver)
		{
			var readerParameters = new ReaderParameters
			{
				ReadingMode= ReadingMode.Immediate,
				SymbolStream = pdbStream,
				SymbolReaderProvider = new DefaultSymbolReaderProvider(), 
				AssemblyResolver = asmResolver,
				ReadWrite = true,
				ReadSymbols = true,
				ReflectionImporterProvider = new QReflectionImporterProvider(),
			};
			var assmebly = AssemblyDefinition.ReadAssembly(peStream, readerParameters);
			asmResolver.selfAssembly = assmebly;
			return assmebly;
		}
		public bool Weave(QAssemblyResolver resolver)
		{
			Assembly = resolver.selfAssembly;
			var modified = false;
			try
			{
				//if (Assembly.MainModule.ContainsClass(GenNamespace, GenClass))
				//{
				//	return false;
				//}
				//var GeneratedCodeClass = new TypeDefinition(GenNamespace, GenClass,
				//TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed,
				//Assembly.Import(typeof(object)));
				InitMethod();
				foreach (TypeDefinition type in Assembly.MainModule.GetAllTypes())
				{
					if (type.IsClass && type.BaseType.CanBeResolved())
					{
						modified |= WeaveType(type);
					}  
				}
				if (modified)
				{
					//Log("Add_" + Assembly.Name);
			//		Assembly.MainModule.Types.Add(GeneratedCodeClass);
				}
			}
			catch (Exception e)
			{
				Log($" Exception :{e}", DiagnosticType.Error);
				return false;
			}
			return modified;
		}
		MethodReference LogError = null;
		MethodReference PlayerAction = null;
		MethodDefinition AddParam = null;
		public void InitMethod()
		{
			LogError = Assembly.GetMethodByCount(typeof(UnityEngine.Debug), nameof(UnityEngine.Debug.LogError), 1);
			PlayerAction = Assembly.GetMethodByCount(typeof(QNetBehaviour), nameof(QNetBehaviour._InvokeAction), 1);
			AddParam = Assembly.GetMethodByCount(typeof(QNetBehaviour), nameof(QNetBehaviour._AddActionParam)).Resolve();
		}
		bool WeaveType(TypeDefinition type)
		{
			if (!type.IsClass)
				return false;
			if (!type.IsDerivedFrom<QNetBehaviour>())
			{
				return false;
			}
			var behaviourClasses = new List<TypeDefinition>();
			TypeDefinition parent = type;
			while (parent != null)
			{
				if (parent.Is<QNetBehaviour>())
				{
					break;
				}
				try
				{
					behaviourClasses.Insert(0, parent);
					parent = parent.BaseType.Resolve();
				}
				catch (AssemblyResolutionException)
				{
					break;
				}
			}
			bool modified = false;
			foreach (TypeDefinition behaviour in behaviourClasses)
			{
				modified |= WeaveQNetBehavior(behaviour);
			}
			return modified;
		}
		bool WeaveQNetBehavior(TypeDefinition qNet)
		{
			if (WasProcessed(qNet))
			{
				return false;
			}
			MarkAsProcessed(qNet);
			List<MethodDefinition> methods = new List<MethodDefinition>(qNet.Methods);
			foreach (MethodDefinition md in methods)
			{
				foreach (CustomAttribute ca in md.CustomAttributes)
				{
					if (ca.AttributeType.Is<QSyncActionAttribute>())
					{
						if (md.IsAbstract || md.IsStatic)
						{
							Log("同步Action 必须是非静态实例化函数", DiagnosticType.Error);
							return false;
						}
						WeaveQSyncAction(md);
						break;
					}
				}
			}
			return true;
		}
		void WeaveQSyncAction(MethodDefinition method)
		{
			var action = method.ReplaceMethod(QSyncAction_);
			ILProcessor worker = method.Body.GetILProcessor();
			for (int i = 0; i < method.Parameters.Count; i++)
			{
				worker.This();
				worker.Param(i);
				var add = AddParam.MakeGeneric(Assembly.MainModule, method.Parameters[i].ParameterType);
				worker.Call(add); 
			}
			worker.This();
			worker.String(action.Name);
			worker.Call(PlayerAction);

			worker.String(action.Name);
			worker.Call(LogError);
			worker.Return();
		}
		public const string ProcessedFunctionName = "Weaved";
		public static bool WasProcessed(TypeDefinition td)
		{
			return td.GetMethod(ProcessedFunctionName) != null;
		}
		public static void MarkAsProcessed(TypeDefinition td)
		{
			if (!WasProcessed(td))
			{
				MethodDefinition versionMethod = new MethodDefinition(
					ProcessedFunctionName,
					MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot,
					 Assembly.Import(typeof(bool)));
				ILProcessor worker = versionMethod.Body.GetILProcessor();
				worker.Emit(OpCodes.Ldc_I4_1);
				worker.Emit(OpCodes.Ret); 
				td.Methods.Add(versionMethod);
			}
		}


	}

}
