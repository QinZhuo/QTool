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

namespace QTool.CodeGen
{
	public class QNetCodeGen : QILPostProcessor
	{
		public const string QNet = nameof(QNet);
		public override bool WillProcess(ICompiledAssembly compiledAssembly) 
			=> compiledAssembly.Name == QNet || compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == QNet);
		public override bool Weave()
		{
			var modified = false;
			try
			{
				if (!Check()) return modified;
				InitMethod();
				foreach (TypeDefinition type in Assembly.MainModule.GetAllTypes())
				{
					if (type.IsClass && type.BaseType.CanBeResolved())
					{
						modified |= WeaveType(type);
					}
				}
			}
			catch (Exception e)
			{
				Log($" Exception :{e}", DiagnosticType.Error);
				return false;
			}
			return modified;
		}
		private MethodReference LogError = null;
		private MethodReference PlayerAction = null;
		public void InitMethod()
		{
			LogError = Assembly.GetMethod(typeof(UnityEngine.Debug), nameof(UnityEngine.Debug.LogError), 1);
			PlayerAction = Assembly.GetMethod(typeof(Net.QNetBehaviour), nameof(Net.QNetBehaviour._PlayerAction));
		}
		bool WeaveType(TypeDefinition type)
		{
			if (!type.IsClass || !type.IsDerivedFrom<Net.QNetBehaviour>())
			{
				return false;
			}
			if (!Check(type)) return false;
			var behaviourClasses = new List<TypeDefinition>();
			TypeDefinition parent = type;
			while (parent != null)
			{
				if (parent.Is<Net.QNetBehaviour>())
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
			foreach (MethodDefinition md in qNet.Methods.ToArray())
			{
				foreach (CustomAttribute ca in md.CustomAttributes)
				{
					if (ca.AttributeType.Is<Net.QNetBehaviour.QSyncActionAttribute>())
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
			var methodName = method.GetNameWithParams() + nameof(Net.QNetBehaviour._PlayerAction); 
			if (method.CheckReplaceMethod(methodName))
			{
				var worker = method.Body.GetILProcessor();
				worker.This().Call(PlayerAction, () => { worker.String(methodName); }, () =>
				{
					worker.NewArray(Assembly.Import(typeof(object)), method.Parameters.Count, index =>
					{
						worker.Param(index);
					});
				});
				worker.Return();
			}
		}
		public bool Check()
		{
			if (Assembly.MainModule.ContainsClass(nameof(QNet), nameof(QNetCodeGen)))
			{
				return false;
			}
			var GeneratedCodeClass = new TypeDefinition(nameof(QNet), nameof(QNetCodeGen),
			TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed,
			Assembly.Import(typeof(object)));
			Assembly.MainModule.Types.Add(GeneratedCodeClass);
			return true;
		}
		public static bool Check(TypeDefinition td)
		{
			if (td.GetMethod(nameof(QNetCodeGen)) != null)
			{
				return false;
			}
			var versionMethod = new MethodDefinition(
					nameof(QNetCodeGen),
					MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot,
					 Assembly.Import(typeof(bool)));
			var worker = versionMethod.Body.GetILProcessor();
			worker.Emit(OpCodes.Ldc_I4_1);
			worker.Emit(OpCodes.Ret);
			td.Methods.Add(versionMethod);
			return true;
		}
	}
}
