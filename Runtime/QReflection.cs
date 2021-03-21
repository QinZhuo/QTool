using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace QTool.Reflection
{
    public static class QReflection
    {
        public static void ForeachMemeber(this Type type, Action<FieldInfo> fieldInfo, Action<PropertyInfo> propertyInfo = null, BindingFlags bindingFlags= BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo[] fields = type.GetFields(bindingFlags);
            foreach (var item in fields)
            {
                fieldInfo?.Invoke(item);
            }
            var infos = type.GetProperties(bindingFlags);
            foreach (var item in infos)
            {
                propertyInfo?.Invoke(item);
            }
        }
        public static void ForeachFunction(this Type type, Action<MethodInfo> methodeInfo, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            var methods= type.GetMethods(bindingFlags);
            foreach (var method in methods)
            {
                methodeInfo?.Invoke(method);
            }
        }
    }
}
