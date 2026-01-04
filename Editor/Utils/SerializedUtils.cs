using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public static class SerializedUtils
    {
        public readonly struct FieldOrProp
        {
            public readonly bool IsField;
            public readonly FieldInfo FieldInfo;
            public readonly PropertyInfo PropertyInfo;

            public FieldOrProp(FieldInfo fieldInfo)
            {
                IsField = true;
                FieldInfo = fieldInfo;
                PropertyInfo = null;
            }

            public FieldOrProp(PropertyInfo propertyInfo)
            {
                IsField = false;
                FieldInfo = null;
                PropertyInfo = propertyInfo;
            }

            public override string ToString()
            {
                return IsField ? FieldInfo.ToString() : PropertyInfo.ToString();
            }
        }

        public static int PropertyPathIndex(string propertyPath)
        {
            string[] propPaths = propertyPath.Split('.');
            // ReSharper disable once UseIndexFromEndExpression
            string lastPropPath = propPaths[propPaths.Length - 1];
            if (lastPropPath.StartsWith("data[") && lastPropPath.EndsWith("]"))
            {
                return int.Parse(lastPropPath.Substring(5, lastPropPath.Length - 6));
            }

            return -1;
        }

        public static (bool trimed, string[] propPathSegs) TrimEndArray(string[] propPathSegments)
        {

            int usePathLength = propPathSegments.Length;

            if (usePathLength <= 2)
            {
                return (false, propPathSegments);
            }

            string lastPart = propPathSegments[usePathLength - 1];
            string secLastPart = propPathSegments[usePathLength - 2];
            bool isArray = secLastPart == "Array" && lastPart.StartsWith("data[") && lastPart.EndsWith("]");
            if (!isArray)
            {
                return (false, propPathSegments);
            }

            // old Unity does not have SkipLast
            string[] propPaths = new string[usePathLength - 2];
            Array.Copy(propPathSegments, 0, propPaths, 0, usePathLength - 2);
            return (true, propPaths);
        }

        public static IReadOnlyList<(FieldOrProp fieldOrProp, object parent)> GetFieldInfoAndParentListByPathSegments(
            object sourceObj, IEnumerable<string> pathSegments)
        {
            List<(FieldOrProp fieldOrProp, object parent)> results =
                new List<(FieldOrProp fieldOrProp, object parent)>();
            // object sourceObj = property.serializedObject.targetObject;
            FieldOrProp fieldOrProp = default;

            bool preNameIsArray = false;
            foreach (string propSegName in pathSegments)
            {
                // Debug.Log($"check key {propSegName}");
                if(propSegName == "Array")
                {
                    preNameIsArray = true;
                    continue;
                }
                if (propSegName.StartsWith("data[") && propSegName.EndsWith("]"))
                {
                    Debug.Assert(preNameIsArray);
                    // Debug.Log(propSegName);
                    // Debug.Assert(targetProp != null);
                    preNameIsArray = false;

                    int elemIndex = Convert.ToInt32(propSegName.Substring(5, propSegName.Length - 6));

                    object useObject;

                    if(fieldOrProp.FieldInfo is null && fieldOrProp.PropertyInfo is null)
                    {
                        useObject = sourceObj;
                    }
                    else
                    {
                        useObject = fieldOrProp.IsField
                            // ReSharper disable once PossibleNullReferenceException
                            ? fieldOrProp.FieldInfo.GetValue(sourceObj)
                            : fieldOrProp.PropertyInfo.GetValue(sourceObj);
                    }

                    // Debug.Log($"Get index from obj {useObject}[{elemIndex}]");
                    sourceObj = Util.GetValueAtIndexFromCollection(useObject, elemIndex).Item2;
                    // Debug.Log($"Get index from obj `{useObject}` returns {sourceObj}");
                    fieldOrProp = default;
                    // Debug.Log($"[index={elemIndex}]={targetObj}");
                    continue;
                }

                preNameIsArray = false;

                // if (propSegName.StartsWith("<") && propSegName.EndsWith(">k__BackingField"))
                // {
                //     propSegName = propSegName.Substring(1, propSegName.Length - 17);
                // }

                // Debug.Log($"get obj {sourceObj}.{propSegName}")
                //
                if (sourceObj == null)  // TODO: better error handling
                {
                    break;
                    // return (default, null);
                }
                // ;
                // ReSharper disable once UseNegatedPatternInIsExpression
                if (!(fieldOrProp.FieldInfo is null)
                    // ReSharper disable once UseNegatedPatternInIsExpression
                    || !(fieldOrProp.PropertyInfo is null))
                {
                    sourceObj = fieldOrProp.IsField
                        // ReSharper disable once PossibleNullReferenceException
                        ? fieldOrProp.FieldInfo.GetValue(sourceObj)
                        : fieldOrProp.PropertyInfo.GetValue(sourceObj);
                    // Debug.Log($"get key {propSegName} sourceObj = {sourceObj}");
                }

                fieldOrProp = GetFileOrProp(sourceObj, propSegName);
                results.Add((fieldOrProp, sourceObj));
            }

            results.Reverse();
            return results;
            // return (fieldOrProp, sourceObj);
        }

        private static FieldOrProp GetFileOrProp(object source, string name)
        {
            Type type = source.GetType();
            // Debug.Log($"get type {type}");

            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    // Debug.Log($"return field {field.Name} by {name}");
                    return new FieldOrProp(field);
                }

                PropertyInfo property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    // return property.GetValue(source, null);
                    // Debug.Log($"return prop {property.Name} by {name}");
                    return new FieldOrProp(property);
                }

                type = type.BaseType;
            }


            throw new Exception($"Unable to get {name} from {source}");
        }
    }
}
