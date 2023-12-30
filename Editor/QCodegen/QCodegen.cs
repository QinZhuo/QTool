using Mono.Cecil;
using System;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace QTool.Codegen
{
	/// <summary>
	/// 代码生成工具类 
	/// 代码生成期间是多线程运行的 不要使用静态变量
	/// </summary>
	public static class QCodegen
	{
		public static bool HasDefine(this ICompiledAssembly assembly, string define) =>
		   assembly.Defines != null &&
		   assembly.Defines.Contains(define);

		public static TypeReference Import<T>(this AssemblyDefinition Assembly)
		{
			return Assembly.MainModule.ImportReference(typeof(T));
		}
		public static MethodReference ImportMethod(this TypeReference type, string methodName, AssemblyDefinition Assembly)
		{
			foreach (var methodRef in type.Resolve().Methods)
			{
				if (methodRef.Name == methodName)
				{
					return Assembly.MainModule.ImportReference(methodRef);
				}
			};
			return null;
		}
		public static ILProcessor This(this ILProcessor iL)
		{
			iL.Emit(OpCodes.Ldarg, 0);
			return iL;
		}
		public static void NewObject(this ILProcessor iL, MethodReference method)
		{
			iL.Emit(OpCodes.Newobj, method);
		}
		public static void NewArray(this ILProcessor iL, TypeReference type, int count, Action<int> indexAction)
		{
			iL.Int(count);
			iL.Emit(OpCodes.Newarr, type);
			iL.Dup();
			for (int i = 0; i < count; i++)
			{
				iL.Int(i);
				indexAction(i);
			}
			iL.Emit(OpCodes.Stelem_Ref);
		}
		public static void NewArray(this ILProcessor iL, TypeReference type, params Action[] paramAction)
		{
			iL.Int(paramAction.Length);
			iL.Emit(OpCodes.Newarr, type);
			iL.Dup();
			for (int i = 0; i < paramAction.Length; i++)
			{
				iL.Int(i);
				paramAction[i]();
			} 
			iL.Emit(OpCodes.Stelem_Ref);
		}
		public static void Call(this ILProcessor iL, MethodReference method,params Action[] paramActions)
		{
			foreach (var action in paramActions)
			{
				action();
			}
			iL.Emit(OpCodes.Call, method);
		}
		/// <summary>
		/// 储存堆栈到局部变量
		/// </summary>
		public static void Save(this ILProcessor il, int index = 0)
		{
			il.Emit(OpCodes.Stloc, index);
		}
		/// <summary>
		/// 读取局部变量到堆栈
		/// </summary>
		public static void Load(this ILProcessor il, int index = 0)
		{
			il.Emit(OpCodes.Ldloc, index);
		}
		public static void Pop(this ILProcessor iL)
		{
			iL.Emit(OpCodes.Pop);
		}
		public static void Dup(this ILProcessor iL)
		{
			iL.Emit(OpCodes.Dup);
		}
		/// <summary>
		/// 加载参数到计算堆栈上
		/// </summary>
		public static void Param(this ILProcessor iL, int index)
		{
			iL.Emit(OpCodes.Ldarg, index + 1);
		}
		/// <summary>
		/// 加载字符串到堆栈上
		/// </summary>
		public static void String(this ILProcessor iL, string value)
		{
			iL.Emit(OpCodes.Ldstr, value);
		}
		public static void Box(this ILProcessor iL, TypeReference type)
		{
			iL.Emit(OpCodes.Box, type);
		}
		public static void Unbox(this ILProcessor iL, TypeReference type)
		{
			iL.Emit(OpCodes.Unbox, type);
		}
		/// <summary>
		/// 加载整数到堆栈上
		/// </summary>
		public static void Int(this ILProcessor iL, int value)
		{
			iL.Emit(OpCodes.Ldc_I4, value);
		}
		public static void Float(this ILProcessor iL, float value)
		{
			iL.Emit(OpCodes.Ldc_R4, value);
		}
		public static void Double(this ILProcessor iL, double value)
		{
			iL.Emit(OpCodes.Ldc_R8, value);
		}
		public static void Filed(this ILProcessor iL, FieldDefinition value)
		{
			iL.Emit(OpCodes.Ldfld, value);
		}
		public static void SetFiled(this ILProcessor iL, FieldDefinition value)
		{
			iL.Emit(OpCodes.Stfld, value);
		}
		public static void Return(this ILProcessor iL)
		{
			iL.Emit(OpCodes.Ret);
		}
		public static string ToCodeString(this MethodDefinition method)
		{
			if (method == null) return "";
			var str = method.Name + "\n";
			foreach (var data in method.Body.Instructions)
			{
				str += data.OpCode + " " + data.Operand + '\n';
			}
			return str;
		}

		public static bool CheckReplaceMethod(this MethodDefinition method, string moveToMethodName = "QCodeGen_")
		{
			foreach (var m in method.DeclaringType.Methods)
			{
				if (m.Name == moveToMethodName)
				{
					return false;
				}
			}
			var newMethod = new MethodDefinition(moveToMethodName, method.Attributes, method.ReturnType);
			newMethod.IsPublic = false;
			newMethod.IsFamily = true;
			foreach (var pd in method.Parameters)
			{
				newMethod.Parameters.Add(new ParameterDefinition(pd.Name, ParameterAttributes.None, pd.ParameterType));
			}
			(newMethod.Body, method.Body) = (method.Body, newMethod.Body);
			foreach (SequencePoint sequencePoint in method.DebugInformation.SequencePoints)
			{
				newMethod.DebugInformation.SequencePoints.Add(sequencePoint);
			}
			method.DebugInformation.SequencePoints.Clear();

			foreach (CustomDebugInformation customInfo in method.CustomDebugInformations)
			{
				newMethod.CustomDebugInformations.Add(customInfo);
			}
			method.CustomDebugInformations.Clear();
			(method.DebugInformation.Scope, newMethod.DebugInformation.Scope) = (newMethod.DebugInformation.Scope, method.DebugInformation.Scope);
			method.DeclaringType.Methods.Add(newMethod);
			return true;
		}
		public static string GetNameWithParams(this MethodDefinition md)
		{
			var nameStart = md.Name;
			for (int i = 0; i < md.Parameters.Count; ++i)
			{
				nameStart += "_" + md.Parameters[i].ParameterType.Name;
			}
			return nameStart;
		}
		public static bool Is(this TypeReference td, Type type) =>
			  type.IsGenericType
				? td.GetElementType().FullName == type.FullName.Replace('+', '/')
				: td.FullName == type.FullName.Replace('+', '/');
		 
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
	public class QReflectionImporterProvider : IReflectionImporterProvider
	{
		public IReflectionImporter GetReflectionImporter(ModuleDefinition module) =>
			new QReflectionImporter(module);

		public class QReflectionImporter : DefaultReflectionImporter
		{
			const string SystemPrivateCoreLib = "System.Private.CoreLib"; 
			readonly AssemblyNameReference fixedCoreLib;
			public QReflectionImporter(ModuleDefinition module) : base(module)
			{
				fixedCoreLib = module.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib" || a.Name == "netstandard" || a.Name == SystemPrivateCoreLib);
			}
			public override AssemblyNameReference ImportReference(System.Reflection.AssemblyName name)
			{
				if (name.Name == SystemPrivateCoreLib && fixedCoreLib != null)
					return fixedCoreLib;  
				return base.ImportReference(name);
			}
		}
	}
	public class QAssemblyResolver : IAssemblyResolver
	{
		readonly string[] assemblyReferences;

		readonly ConcurrentDictionary<string, AssemblyDefinition> assemblyCache =
			new ConcurrentDictionary<string, AssemblyDefinition>();

		readonly ConcurrentDictionary<string, string> fileNameCache =
			new ConcurrentDictionary<string, string>();

		readonly ICompiledAssembly compiledAssembly;
		public AssemblyDefinition selfAssembly { get; set; }
		public QAssemblyResolver(ICompiledAssembly compiledAssembly)
		{
			this.compiledAssembly = compiledAssembly;
			assemblyReferences = compiledAssembly.References;
		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name) =>
			Resolve(name, new ReaderParameters(ReadingMode.Deferred));
		public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			if (name.Name == compiledAssembly.Name)
				return selfAssembly;

			// cache FindFile.
			// in large projects, this is called thousands(!) of times for String=>mscorlib alone.
			// reduces a single String=>mscorlib resolve from 0.771ms to 0.015ms.
			// => 50x improvement in TypeReference.Resolve() speed!
			// => 22x improvement in Weaver speed!
			if (!fileNameCache.TryGetValue(name.Name, out string fileName))
			{
				fileName = FindFile(name.Name);
				fileNameCache.TryAdd(name.Name, fileName);
			}

			if (fileName == null)
			{
				return null;
			}

			// try to get cached assembly by filename + writetime
			DateTime lastWriteTime = File.GetLastWriteTime(fileName);
			string cacheKey = fileName + lastWriteTime;
			if (assemblyCache.TryGetValue(cacheKey, out AssemblyDefinition result))
				return result;

			// otherwise resolve and cache a new assembly
			parameters.AssemblyResolver = this;
			MemoryStream ms = MemoryStreamFor(fileName);

			string pdb = fileName + ".pdb";
			if (File.Exists(pdb))
				parameters.SymbolStream = MemoryStreamFor(pdb);

			AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
			assemblyCache.TryAdd(cacheKey, assemblyDefinition);
			return assemblyDefinition;
		}
		static MemoryStream MemoryStreamFor(string fileName)
		{
			return Retry(10, TimeSpan.FromSeconds(1), () =>
			{
				byte[] byteArray;
				using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					byteArray = new byte[fs.Length];
					int readLength = fs.Read(byteArray, 0, (int)fs.Length);
					if (readLength != fs.Length)
						throw new InvalidOperationException("File read length is not full length of file.");
				}

				return new MemoryStream(byteArray);
			});
		}
		static MemoryStream Retry(int retryCount, TimeSpan waitTime, Func<MemoryStream> func)
		{
			try
			{
				return func();
			}
			catch (IOException)
			{
				if (retryCount == 0)
					throw;
				Console.WriteLine($"Caught IO Exception, trying {retryCount} more times");
				Thread.Sleep(waitTime);
				return Retry(retryCount - 1, waitTime, func);
			}
		}

		string FindFile(string name)
		{
			foreach (string r in assemblyReferences)
			{
				if (Path.GetFileNameWithoutExtension(r) == name)
					return r;
			}

			string dllName = name + ".dll";

			foreach (string parentDir in assemblyReferences.Select(Path.GetDirectoryName).Distinct())
			{
				string candidate = Path.Combine(parentDir, dllName);
				if (File.Exists(candidate))
					return candidate;
			}
			return null;
		}

	}

}