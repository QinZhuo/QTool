using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace QTool
{
	public static class QStringTool
	{
		public static string RemveChars(this string str, params char[] exceptchars)
		{
			if (str.IsNull() || exceptchars == null) return str;
			foreach (var c in exceptchars)
			{
				str = str.Replace(c.ToString(), "");
			}
			return str;
		}
		public static string QName(this object obj)
		{
			if (obj is UnityEngine.Object uObj)
			{
				if (obj is GameObject gameObject)
				{
					return gameObject.name.SplitStartString("(").SplitStartString("_").TrimEnd();
				}
				else if (obj is Component component)
				{
					return component.gameObject.QName();
				}
				else
				{
					return uObj.name;
				}
			}
			else if (obj is Color color)
			{
				return ColorUtility.ToHtmlStringRGB(color);
			}
			else if (obj is Color32 color32)
			{
				return ColorUtility.ToHtmlStringRGB(color32);
			}
			else if (obj is MemberInfo memberInfo)
			{
				return QReflection.QName(memberInfo);
			}
			else if (obj is PropertyInfo property)
			{
				return QReflection.QName(property);
			}
			else if (obj is IKey<string> ikey)
			{
				return ikey.Key;
			}
			else
			{
				return obj?.ToString();
			}
		}
		/// <summary>
		/// 移除不规则符号
		/// </summary>
		public static string ToKeyString(this string str)
		{
			return str.RemveChars('{', '}', '（', '）', '~', '\n', '\t', '\r', '、', '|', '*', '“', '”', '—', '。', '…', '=', '#', ' ', ';', '；', '-', ',', '，', '<', '>', '【', '】', '[', ']', '{', '}', '!', '！', '?', '？', '.', '\'', '‘', '’', '\"', ':', '：');
		}
		
		public static string ToShortString(this object obj, int length = 5000)
		{
			var str = obj?.ToString();
			if (str != null && str.Length > length)
			{
				str = str.Substring(0, length) + "...";
			}
			return str;
		}
		public static string ToSizeString(this float byteLength)
		{
			return ToSizeString((long)byteLength);
		}
		public static string ToColorString(this object obj, string color)
		{
			if (!color.StartsWith("#"))
			{
				color = "#" + color;
			}
			return "<color=" + color + ">" + obj + "</color>";
		}
		public static string ToColorString(this object obj, Color color)
		{
			return obj.ToColorString(ColorUtility.ToHtmlStringRGB(color));
		}
		public static string ToColorString(this object obj)
		{
			var str = obj.ToString();
			return str.ToColorString(str.ToColor());
		}
		public static string ToSizeString(this int byteLength)
		{
			return ToSizeString((long)byteLength);
		}

		public static string ToSizeString(this long longLength)
		{
			string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };
			int i = 0;
			double dblSByte = longLength;
			if (longLength > 1000)
				for (i = 0; (longLength / 1000) > 0; i++, longLength /= 1000)
					dblSByte = longLength / 1000.0;
			if (i == 0)
			{
				return dblSByte.ToString("f0") + "" + Suffix[i];
			}
			else
			{
				return dblSByte.ToString("f1") + "" + Suffix[i];
			}
		}
		public static string ToOneString<T>(this ICollection<T> array, string splitChar = "\n", Func<T, string> toString = null, bool ignoreNull = false)
		{
			if (array == null)
			{
				return "";
			}
			return QTool.BuildString((writer) =>
			{
				int i = 0;
				if (toString == null)
				{
					foreach (var item in array)
					{
						if (writer == null)
						{
							Debug.LogError("null");
						}
						writer.Write(item);
						if (i < array.Count - 1)
						{
							writer.Write(splitChar);
						}
						i++;
					}
				}
				else
				{
					foreach (var item in array)
					{
						var data = toString(item);
						if (ignoreNull && data.IsNull()) { i++; continue; }
						writer.Write(data);
						if (i < array.Count - 1)
						{
							writer.Write(splitChar);
						}
						i++;
					}
				}
			});
		}
		public static string SplitEndString(this string str, string splitStart)
		{
			if (str.Contains(splitStart))
			{

				return str.Substring(str.LastIndexOf(splitStart) + splitStart.Length);
			}
			else
			{
				return str;
			}
		}
		public static string SplitStartString(this string str, string splitStart)
		{
			if (str.Contains(splitStart))
			{

				return str.Substring(0, str.IndexOf(splitStart));
			}
			else
			{
				return str;
			}
		}
		public static string ForeachBlockValue(this string value, char startChar, char endChar, Func<string, string> action,bool deep=false)
		{
			if (string.IsNullOrEmpty(value)) { return value; }
			var start = value.IndexOf(startChar);
			if (start < 0) return value;
			var end = value.IndexOf(endChar, start + 1);
			if (end < 0) return value;
			while (start >= 0 && end >= 0)
			{
				var nextStart = value.IndexOf(startChar, start + 1);
				while (nextStart > 0 && nextStart < end)
				{
					end = value.IndexOf(endChar, end + 1);
					nextStart = value.IndexOf(startChar, nextStart + 1);
				}
				var key = "";
				if (end > start)
				{
					key = value.Substring(start + 1, end - start - 1);
				}
				if (deep && value.Contains(key))
				{
					key.ForeachBlockValue(startChar, endChar, action, true);
				}
				var result = action(key);
				value = value.Substring(0, start) + result + value.Substring(end + 1);
				end += result.Length - key.Length - 2;
				start = value.IndexOf(startChar, end + 1);
				if (start < 0) break;
				end = value.IndexOf(endChar, start);
			}
			return value;
		}
		public static string GetBlockValue(this string value, char startChar, char endChar)
		{
			var start = value.IndexOf(startChar) + 1;
			var end = value.IndexOf(endChar, start);
			if (end >= 0)
			{
				return value.Substring(start, end - start);
			}
			else
			{
				return value.Substring(start);
			}
		}
		public static string GetBlockValue(this string value, string startStr, string endStr)
		{
			var index = value.IndexOf(startStr);
			if (index < 0)
			{
				return "";
			}
			var start = index + startStr.Length;

			var end = value.IndexOf(endStr, start);

			if (end >= 0)
			{
				return value.Substring(start, end - start);
			}
			else
			{
				return value.Substring(start);
			}
		}
		public static bool SplitTowString(this string str, string splitStart, out string start, out string end)
		{

			if (str.Contains(splitStart))
			{
				var startIndex = str.IndexOf(splitStart);
				start = str.Substring(0, startIndex);
				end = str.Substring(startIndex + splitStart.Length);
				return true;
			}
			else
			{
				start = str;
				end = "";
				return false;
			}
		}
		public static string ToSizeString(this string array)
		{
			return array.Length.ToSizeString();
		}
		public static string ToSizeString(this IList array)
		{
			return array.Count.ToSizeString();
		}
	}
}
