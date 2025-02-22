#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
#endif
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
namespace QTool {
	public static class LazyTypeLocator {
		public static void LocateTypeFile(this Type type, bool open = true) {
#if UNITY_EDITOR
			LocateTypeInProject(type, open);
#endif
		}
#if UNITY_EDITOR
		private static Dictionary<string, string> _typeRegistry = new Dictionary<string, string>();
		private static bool _isInitialized;

		[InitializeOnLoadMethod]
		static void InitRegistry() {
			BuildTypeRegistry();
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// 全量构建类型注册表（耗时操作）
		/// </summary>
		static void BuildTypeRegistry() {
			_typeRegistry.Clear();
			string[] allScripts = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
				.Where(p => !p.EndsWith(".meta")).ToArray();

			foreach (var scriptPath in allScripts) {
				try {
					var content = File.ReadAllText(scriptPath);
					var cleanCode = StripComments(content);

					// 解析命名空间
					var namespaceMatch = Regex.Match(cleanCode,
						@"namespace\s+([^\s;{]+)", RegexOptions.Singleline);
					string ns = namespaceMatch.Success ?
						namespaceMatch.Groups[1].Value.Trim() : "";

					// 解析类型定义
					var typeMatches = Regex.Matches(cleanCode,
						@"(?:class|struct)\s+([\w<>]+)");

					foreach (Match match in typeMatches) {
						string typeName = match.Groups[1].Value;
						string fullName = string.IsNullOrEmpty(ns) ?
							typeName : $"{ns}.{typeName}";

						string unityPath = ConvertToAssetPath(scriptPath);
						_typeRegistry[fullName] = unityPath;
					}
				}
				catch {/* 异常处理 */}
			}
			_isInitialized = true;
		}

		/// <summary>
		/// 精准定位类型文件
		/// </summary>
		public static void LocateTypeInProject(Type targetType, bool openFile = false) {
			if (!_isInitialized)
				BuildTypeRegistry();

			string fullName = GetComparableName(targetType);

			if (_typeRegistry.TryGetValue(fullName, out var path)) {
				UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				if (openFile) {
					EditorGUIUtility.PingObject(obj);
					AssetDatabase.OpenAsset(obj);
				}
				else {
					Selection.activeObject = obj;
				}
			}
		}

		static string ConvertToAssetPath(string systemPath) {
			return systemPath.Substring(Application.dataPath.Length - 6)
				.Replace('\\', '/');
		}

		static string GetComparableName(Type type) {
			// 处理嵌套类型和泛型
			string name = type.FullName ?? type.Name;
			if (type.DeclaringType != null) {
				name = $"{GetComparableName(type.DeclaringType)}.{type.Name}";
			}
			return name.Replace('+', '.')
				.Split('`')[0]; // 移除泛型参数标识
		}

		static string StripComments(string code) {
			// 移除所有注释（性能关键点）
			code = Regex.Replace(code,
				@"/\*.*?\*/", "", RegexOptions.Singleline);      // 块注释
			code = Regex.Replace(code,
				@"(//.*)|(^\s*#.*)", "", RegexOptions.Multiline); // 行注释与预处理指令
			code = Regex.Replace(code,
				@"\s+", " "); // 简化空格处理
			return code;
		}
#endif
	}
}
