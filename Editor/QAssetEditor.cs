using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Data;
using System;
using System.Threading.Tasks;
using UnityEngine.U2D;
using UnityEditor.U2D;
using UnityEngine.Networking;
#if Addressable
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif
namespace QTool.Asset {
	public static class AddressableToolEditor
	{

		#region 资源Id
		[MenuItem("QTool/资源管理/场景资源/查找当前场景所有Mesh丢失")]
		static void FindAllMeshNull()
		{
			var meshs = GameObject.FindObjectsOfType<MeshFilter>();
			foreach (var mesh in meshs)
			{
				if (Application.isPlaying ? mesh.mesh == null : mesh.sharedMesh == null)
				{
					Debug.LogError(mesh.transform.GetPath() + " Mesh为null");
				}
			}
		}
		[MenuItem("QTool/资源管理/资源Id/复制Id")]
		static void CopyID()
		{
			if (Selection.assetGUIDs.Length == 1)
			{
				if (Selection.activeObject != null)
				{
					GUIUtility.systemCopyBuffer = Selection.assetGUIDs[0];
					Debug.LogError("复制 " + Selection.activeObject.name + " Id[" + GUIUtility.systemCopyBuffer + "]");
				}
			}
			else
			{
				Debug.LogError("选中过多");
			}
		}
		[MenuItem("QTool/资源管理/资源Id/使用粘贴板Id替换当前Id")]
		static void RepleaceID()
		{
			if (Selection.assetGUIDs.Length == 1)
			{
				var target = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(GUIUtility.systemCopyBuffer), typeof(UnityEngine.Object));

				if (Selection.activeObject != null && target != null && Selection.activeObject != target)
				{
					if (Selection.activeObject.GetType() != target.GetType())
					{
						Debug.LogError(Selection.activeObject.name + " : " + target.name + " 类型不匹配");
						return;
					}
					var oldPath = AssetDatabase.GetAssetPath(Selection.activeObject);
					if (EditorUtility.DisplayDialog("资源替换", "确定将" + oldPath + "替换为" + AssetDatabase.GetAssetPath(target), "确定", "取消"))
					{
						Debug.LogError("将" + oldPath + "替换为" + AssetDatabase.GetAssetPath(target));
						var oldId = Selection.assetGUIDs[0];
						var newId = GUIUtility.systemCopyBuffer;
						foreach (var path in AssetDatabase.GetAllAssetPaths())
						{
							if (!path.StartsWith("Assets/") || path == oldPath) continue;

							var end = Path.GetExtension(path);
							switch (end)
							{
								case ".prefab":
								case ".asset":
								case ".unity":
								case ".mat":
								case ".playable":
									{
										var text = QFileManager.Load(path,"",true);
										if (text.Contains(oldId))
										{
											Debug.LogError("更改[" + path + "]引用资源");
											QFileManager.Save(path, text.Replace(oldId, newId));
										}
									}
									break;
								default:
									break;
							}

						}
					}

				}
			}
			else
			{
				Debug.LogError("选中过多");
			}
		}
	
		[MenuItem("QTool/资源管理/资源Id/查找资源引用 %#&f")]
		static void FindreAssetFerencesMenu()
		{
			if (Selection.assetGUIDs.Length == 0)
			{
				Debug.LogError("请先选择任意一个资源 再查找资源引用");
				return;
			}
			Debug.LogError("开始查找引用[" + Selection.objects.ToOneString(" ", (obj) => obj.name) + "]的资源");
			var assetGUIDs = Selection.assetGUIDs;
			var assetPaths = new string[assetGUIDs.Length];
			for (int i = 0; i < assetGUIDs.Length; i++)
			{
				assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
			}
			var allAssetPaths = AssetDatabase.GetAllAssetPaths();
			Task.Run(async () =>
			{
				List<Task> tasks = new List<Task>();

				for (int i = 0; i < allAssetPaths.Length; i++)
				{
					var path = allAssetPaths[i];
					if (!path.StartsWith("Assets/")) continue;
					tasks.Add(Task.Run(() =>
					{
						var end = Path.GetExtension(path);
						switch (end)
						{
							case ".prefab":
							case ".asset":
							case ".unity":
							case ".mat":
							case ".playable":
							case ".shadergraph":
							case ".shadersubgraph":
								{
									string content = File.ReadAllText(path);
									if (content == null)
									{
										return;
									}

									for (int j = 0; j < assetGUIDs.Length; j++)
									{
										if (content.IndexOf(assetGUIDs[j]) > 0)
										{
											Debug.LogError(path + " 引用 " + assetPaths[j]);
										}
									}
								}
								break;
							default:
								break;
						}
					}));
				}
				foreach (var task in tasks)
				{
					await task;
				}
				Debug.LogError("查找完成");
			});
		}
		[MenuItem("QTool/资源管理/资源Id/查找引用的资源")]
		static void FindDependencies()
		{
			if (Selection.assetGUIDs.Length == 0)
			{
				Debug.LogError("请先选择任意一个资源 再查找引用的资源");
				return;
			}
			Debug.LogError("开始查找资源[" + Selection.objects.ToOneString(" ", (obj) => obj.name) + "]的引用");
			var assetGUIDs = Selection.assetGUIDs;
			var assetPaths = new string[assetGUIDs.Length];
			for (int i = 0; i < assetGUIDs.Length; i++)
			{
				assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
			}
			for (int i = 0; i < assetPaths.Length; i++)
			{
				if (string.IsNullOrEmpty(assetPaths[i])) continue;
				foreach (var path in AssetDatabase.GetDependencies(assetPaths))
				{
					Debug.LogError(path + " 被 " + assetPaths[i] + "引用");
				}
			}
			Debug.LogError("查找完成");
		}
		[MenuItem("QTool/资源管理/资源Id/通过粘贴版Id查找资源")]
		public static void FindAsset()
		{
			try
			{
				var obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(GUIUtility.systemCopyBuffer), typeof(UnityEngine.Object));

				Debug.LogError("找到 " + obj);
				Selection.activeObject = obj;
			}
			catch (System.Exception e)
			{
				Debug.LogError("查找出错：" + e);
				throw;
			}
		}

		[MenuItem("QTool/资源管理/资源名/解析URL编码资源名")]
		public static void ParseURLName()
		{
			foreach (var asset in Selection.objects)
			{
				var path= AssetDatabase.GetAssetPath(asset);
				var newName = UnityWebRequest.UnEscapeURL(asset.name);
				if (asset.name != newName)
				{
					QDebug.Log("更改资源名[" + asset.name + "]=>[" + newName + "]");
					var error= AssetDatabase.RenameAsset(path, newName);
					if (!error.IsNull())
					{
						Debug.LogError("更改资源名出错 "+error);
						return;
					}
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		#endregion
		#region 批量处理

		[MenuItem("QTool/资源管理/批量处理/转换所有动画事件")]
		static void FreshAnimationEvent()
		{

			foreach (var path in AssetDatabase.GetAllAssetPaths())
			{
				if (!(path.StartsWith("Assets/") && (path.EndsWith(".fbx") || path.EndsWith(".FBX")))) continue;
				var importer = AssetImporter.GetAtPath(path) as ModelImporter;
				FreshEvents(importer);
			}
			AssetDatabase.Refresh();
		}
		static void FreshEvents(ModelImporter importer)
		{
			if (importer && importer.clipAnimations.Length > 0)
			{
				bool changed = false;
				ModelImporterClipAnimation[] animations = importer.clipAnimations;
				foreach (var animation in animations)
				{
					List<AnimationEvent> events = new List<AnimationEvent>();
					foreach (var eventData in animation.events)
					{
						if (eventData.functionName != nameof(QEventTrigger))
						{
							changed = true;
							events.Add(new AnimationEvent
							{
								time = eventData.time,
								stringParameter = eventData.functionName,
								functionName = nameof(QEventTrigger)
							});
						}
						else
						{
							events.Add(new AnimationEvent
							{
								time = eventData.time,
								stringParameter = eventData.stringParameter,
								functionName = eventData.functionName
							});
						}
					}
					if (changed)
					{
						animation.events = events.ToArray();
					}
				}
				if (changed)
				{
					importer.clipAnimations = animations;
					importer.SaveAndReimport();
				}
			}
		}
		[MenuItem("QTool/资源管理/批量处理/显示所有资源格式")]
		static void FindAllAssetExtension()
		{
			QDictionary<string, string> list = new QDictionary<string, string>();
			foreach (var path in AssetDatabase.GetAllAssetPaths())
			{
				if (!path.StartsWith("Assets/")) continue;
				list[Path.GetExtension(path)] = path;
			}
			Debug.LogError(list.ToOneString());
		}
		[MenuItem("QTool/资源管理/批量处理/优化所有资源")]
		public static void FreshAllImporter()
		{
			var paths = AssetDatabase.GetAllAssetPaths();
			foreach (var path in paths)
			{
				if (!path.StartsWith("Assets/")) continue;
				if (path.Contains("/Plugins/")) continue;
				AssetImporter assetImporter = AssetImporter.GetAtPath(path);
				if (assetImporter is AudioImporter audioImporter)
				{
					ReImportAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(path), audioImporter);
				}
				else if (assetImporter is TextureImporter textureImporter)
				{
					if (AssetDatabase.LoadAssetAtPath<Texture>(path) is Texture2D tex2D)
					{
						ReImportTexture(tex2D, textureImporter);
					}
				}
			};
			AssetDatabase.SaveAssets();
		}
		public static void ReImportAudio(AudioClip audio, AudioImporter audioImporter)
		{
			if (audio == null) return;
			var setting = QToolSetting.Instance;
			AudioImporterSampleSettings audioSetting = default;
			if (audio.length < 1f)
			{
				audioSetting = setting.audioImporterSettings.Get(0);
				audioImporter.preloadAudioData = true;
			}
			else if (audio.length < 3f)
			{
				audioSetting = setting.audioImporterSettings.Get(1);
				audioImporter.preloadAudioData = false;
			}
			else
			{
				audioSetting = setting.audioImporterSettings.Get(2);
				audioImporter.preloadAudioData = false;
			}
			if (!audioImporter.defaultSampleSettings.Equals(audioSetting))
			{
				if (setting.forceToMono)
				{
					audioImporter.forceToMono = true;
				}
				QDebug.Log("重新导入音频[" + audioImporter.assetPath + "]");
				audioImporter.defaultSampleSettings = audioSetting;
				//audioImporter.SetOverrideSampleSettings("Standalone", audioSetting);
				//audioImporter.SetOverrideSampleSettings("iPhone", audioSetting);
				//audioImporter.SetOverrideSampleSettings("Android", audioSetting);
				audioImporter.SaveAndReimport();
			}
		}
		public readonly static List<int> TextureSize = new List<int> { 1, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };
		public const float MaxCompressionSize = 512 * 512;
		public static void ReImportTexture(Texture2D texture, TextureImporter textureImporter)
		{
			if (texture == null || textureImporter.textureType == TextureImporterType.Cursor) return;
			var setting = QToolSetting.Instance;
			if (textureImporter.assetPath.EndsWith(".png") && textureImporter.textureType == TextureImporterType.Sprite && textureImporter.spriteImportMode == SpriteImportMode.Single && (texture.width % 4 != 0 || texture.height % 4 != 0))
			{
				var last = textureImporter.isReadable;
				if (!last)
				{
					textureImporter.isReadable = true;
					textureImporter.SaveAndReimport();
				}
				var widthOffset = texture.width % 4;
				var heightOffset = texture.height % 4;
				var newText = new Texture2D(texture.width + (widthOffset == 0 ? 0 : 4 - widthOffset), texture.height + (heightOffset == 0 ? 0 : 4 - heightOffset));
				textureImporter.spriteBorder += new Vector4(0, 0, newText.width - texture.width, newText.height - texture.height);
				for (int x = 0; x < newText.width; x++)
				{
					for (int y = 0; y < newText.height; y++)
					{
						if (x < texture.width && y < texture.height)
						{
							newText.SetPixel(x, y, texture.GetPixel(x, y));
						}
						else
						{
							newText.SetPixel(x, y, Color.clear);
						}
					}
				}
				QFileManager.SavePNG(newText, textureImporter.assetPath);
				Debug.LogError("拓展图片用于压缩 " + textureImporter.assetPath);
				if (textureImporter.isReadable != last)
				{
					textureImporter.isReadable = last;
					textureImporter.SaveAndReimport();
				}
			}

			if (!textureImporter.crunchedCompression&&(texture.width*texture.height< MaxCompressionSize))
			{
				QDebug.Log("重新导入图片[" + textureImporter.assetPath + "]");

				if (textureImporter.maxTextureSize > texture.width && textureImporter.maxTextureSize > texture.height)
				{
					for (int i = 0; i < TextureSize.Count - 1 && textureImporter.maxTextureSize > TextureSize[i]; i++)
					{
						var minSize = TextureSize[i];
						var maxSize = TextureSize[i + 1];
						if (texture.width >= minSize || texture.height >= minSize)
						{
							if (texture.width <= maxSize && texture.height <= maxSize)
							{
								textureImporter.maxTextureSize = maxSize;
								Debug.LogError(texture + "  " + nameof(textureImporter.maxTextureSize) + " : " + maxSize);
								break;
							}
						}
					}
				}
				if (textureImporter.textureType != TextureImporterType.Sprite)
				{
					textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
				}
				if (textureImporter.textureType == TextureImporterType.Default)
				{
					if (textureImporter.textureShape == TextureImporterShape.Texture2D)
					{
						textureImporter.isReadable = false;
						textureImporter.mipmapEnabled = false;
					}
				}
				textureImporter.crunchedCompression = true;
				textureImporter.textureCompression = TextureImporterCompression.Compressed;
				textureImporter.compressionQuality = setting.compressionQuality;
//				var defualtSetting = textureImporter.GetDefaultPlatformTextureSettings();
//#if UNITY_SWITCH_API
//				var switchSetting = defualtSetting.QDataCopy();
//				switchSetting.name = nameof(RuntimePlatform.Switch);
//				switchSetting.textureCompression = TextureImporterCompression.CompressedLQ;
//				textureImporter.SetPlatformTextureSettings(switchSetting);
//#endif
				textureImporter.SaveAndReimport();

			}

		}

		#endregion

	}
}
