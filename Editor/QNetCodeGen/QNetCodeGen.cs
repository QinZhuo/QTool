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

namespace QTool.Net
{
	 class ILPostProcessorReflectionImporterProvider : IReflectionImporterProvider
	{
		public IReflectionImporter GetReflectionImporter(ModuleDefinition module) =>
			new DefaultReflectionImporter(module);
	}
	public class QNetCodeGen : ILPostProcessor
	{
		public const string QNet = nameof(QNet);
		public const string GeneratedCodeNamespace = "Mirror";
		public const string GeneratedCodeClassName = "GeneratedNetworkCode"; 
		public static TypeDefinition GeneratedCodeClass;
		public static List<DiagnosticMessage> Logs = new List<DiagnosticMessage>();
		public static AssemblyDefinition Assembly;
		public static TypeReference Import(Type type)
		{
			return Assembly.MainModule.ImportReference(type);
		}
		public static void Log(object obj, DiagnosticType type = DiagnosticType.Warning)
		{
			Logs.Add(new DiagnosticMessage { DiagnosticType = type, MessageData = obj?.ToString() });
		}
		public override ILPostProcessor GetInstance() => this;
		public override bool WillProcess(ICompiledAssembly compiledAssembly) 
		{
			bool relevant = compiledAssembly.Name == QNet ||
							 compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == QNet);
			return relevant;
		}
		public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
		{
			byte[] peData = compiledAssembly.InMemoryAssembly.PeData;
			using (MemoryStream stream = new MemoryStream(peData))
			//using (ILPostProcessorAssemblyResolver asmResolver = new ILPostProcessorAssemblyResolver(compiledAssembly, Log))
			{
				using (MemoryStream symbols = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData))
				{
					ReaderParameters readerParameters = new ReaderParameters
					{
						SymbolStream = symbols,
						ReadWrite = true,
						ReadSymbols = true,
						AssemblyResolver = new DefaultAssemblyResolver(),
						ReflectionImporterProvider = new ILPostProcessorReflectionImporterProvider(),
					};
					using (Assembly = AssemblyDefinition.ReadAssembly(stream, readerParameters))
					{
						if (Weave(readerParameters.AssemblyResolver))
						{
							if (Assembly.MainModule.AssemblyReferences.Any(r => r.Name == Assembly.Name.Name))
							{
								Assembly.MainModule.AssemblyReferences.Remove(Assembly.MainModule.AssemblyReferences.First(r => r.Name == Assembly.Name.Name));
							
							}

							MemoryStream peOut = new MemoryStream();
							MemoryStream pdbOut = new MemoryStream();
							WriterParameters writerParameters = new WriterParameters
							{
								SymbolWriterProvider = new PortablePdbWriterProvider(),
								SymbolStream = pdbOut,
								WriteSymbols = true
							};

							Assembly.Write(peOut, writerParameters);

							InMemoryAssembly inMemory = new InMemoryAssembly(peOut.ToArray(), pdbOut.ToArray());
							return new ILPostProcessResult(inMemory, Logs);
						}
					}
				}
			}

			return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, Logs);
		}
		public bool Weave(IAssemblyResolver resolver)
		{
			var modified = false;
			try
			{
				if (Assembly.MainModule.ContainsClass(GeneratedCodeNamespace, GeneratedCodeClassName))
				{
					return false;
				}
				GeneratedCodeClass = new TypeDefinition(GeneratedCodeNamespace, GeneratedCodeClassName,
				TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed,
				Import(typeof(object)));

				foreach (TypeDefinition td in Assembly.MainModule.GetAllTypes())
				{
					if (td.IsClass && td.BaseType.CanBeResolved())
					{
						Log(td?.Name);
						modified |= WeaveNetworkBehavior(td);
					}
				}

				if (modified)
				{
					Assembly.MainModule.Types.Add(GeneratedCodeClass);

					Log("WriteOver");

					return true;
				}

				//if (Assembly.Name.Name == QNet)
				//{
				//	ToggleWeaverFuse();
				//}

			}
			catch (Exception e)
			{
				Log($"Exception :{e}", DiagnosticType.Error);
				return false;
			}
			return modified;
		}
		bool WeaveNetworkBehavior(TypeDefinition td)
		{
			if (!td.IsClass)
				return false;

			if (!td.IsDerivedFrom<QNetBehaviour>())
			{
				return false;
			}
			List<TypeDefinition> behaviourClasses = new List<TypeDefinition>();

			TypeDefinition parent = td;
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

				modified |= QNetBehaviourProcessor(behaviour);
			}
			return modified;
		}
		public bool QNetBehaviourProcessor(TypeDefinition subClass)
		{
			if (WasProcessed(subClass))
			{
				return false;
			}
			else
			{
				MarkAsProcessed(subClass);
			}

			List<MethodDefinition> methods = new List<MethodDefinition>(subClass.Methods);
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

						Log("函数 [" + md.Name + "]");
						break;
					}
				}
			}
			return true;
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
					Import(typeof(bool)));
				ILProcessor worker = versionMethod.Body.GetILProcessor();
				worker.Emit(OpCodes.Ldc_I4_1);
				worker.Emit(OpCodes.Ret);
				td.Methods.Add(versionMethod);
			}
		}
		
	}
	public static class Extends
	{
		public static bool Is(this TypeReference td, Type type) =>
			  type.IsGenericType
				? td.GetElementType().FullName == type.FullName
				: td.FullName == type.FullName;

		public static bool Is<T>(this TypeReference td) => Is(td, typeof(T));
		public static bool IsDerivedFrom<T>(this TypeReference tr) => IsDerivedFrom(tr, typeof(T));

		public static bool IsDerivedFrom(this TypeReference tr, Type baseClass)
		{
			TypeDefinition td = tr.Resolve();
			if (!td.IsClass)
				return false;

			TypeReference parent = td.BaseType;

			if (parent == null)
				return false;

			if (parent.Is(baseClass))
				return true;

			if (parent.CanBeResolved())
				return IsDerivedFrom(parent.Resolve(), baseClass);

			return false;
		}
		public static MethodDefinition GetMethod(this TypeDefinition td, string methodName)
		{
			return td.Methods.FirstOrDefault(method => method.Name == methodName);
		}
		public static bool ContainsClass(this ModuleDefinition module, string nameSpace, string className) =>
		  module.GetTypes().Any(td => td.Namespace == nameSpace &&
								td.Name == className);
		public static bool CanBeResolved(this TypeReference parent)
		{
			while (parent != null) 
			{
				if (parent.Scope.Name == "Windows")
				{
					return false;
				}

				if (parent.Scope.Name == "mscorlib")
				{
					TypeDefinition resolved = parent.Resolve();
					return resolved != null;
				}

				try
				{
					parent = parent.Resolve().BaseType;
				}
				catch
				{
					return false;
				}
			}
			return true;
		}
	}
}
