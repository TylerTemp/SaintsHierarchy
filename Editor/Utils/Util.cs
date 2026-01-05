using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Draw;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor.Utils
{
    public static class Util
    {
        private static readonly string[] ResourceSearchFolder = {
            "Assets/Editor Default Resources/SaintsHierarchy",
            // this is readonly, put it to last so user can easily override it
            "Packages/today.comes.saintshierarchy/Editor/Editor Default Resources/SaintsHierarchy", // Unity UPM
#if SAINTSHIERARCHY_SAINTSFIELD
            "Assets/Editor Default Resources/SaintsField",
            "Assets/SaintsField/Editor/Editor Default Resources/SaintsField",  // unitypackage
            // this is readonly, put it to last so user  can easily override it
            "Packages/today.comes.saintsfield/Editor/Editor Default Resources/SaintsField", // Unity UPM
#endif
        };

        public static T LoadResource<T>(string resourcePath) where T: Object
        {
            foreach (T each in ResourceSearchFolder
                         .Select(resourceFolder => AssetDatabase.LoadAssetAtPath<T>($"{resourceFolder}/{resourcePath}")))
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
                if(each != null)
                {
                    return each;
                }
            }

            T result = EditorGUIUtility.Load(resourcePath) as T;

            if (typeof(T) == typeof(Texture2D))
            {
                if (!result)
                {
                    Texture2D r = EditorGUIUtility.IconContent(resourcePath).image as Texture2D;
                    if (r)
                    {
                        result = r as T;
                    }
                }
            }

            if (result == null)
            {
#if SAINTSHIERARCHY_DEBUG
                Debug.LogWarning($"{resourcePath} not found in {string.Join(", ", ResourceSearchFolder)}");
#endif
                return null;
            }
            // Debug.Assert(result, $"{resourcePath} not found in {string.Join(", ", ResourceSearchFolder)}");
            return result;
        }

        private static SaintsHierarchyConfig _config;

        public static SaintsHierarchyConfig EnsureConfig()
        {
            // ReSharper disable once InvertIf
            if (_config == null)
            {
                if (!Directory.Exists("Assets/Editor Default Resources"))
                {
                    Debug.Log("Create folder: Assets/Editor Default Resources");
                    AssetDatabase.CreateFolder("Assets", "Editor Default Resources");
                }
                if (!Directory.Exists("Assets/Editor Default Resources/SaintsHierarchy"))
                {
                    Debug.Log("Create folder: Assets/Editor Default Resources/SaintsHierarchy");
                    AssetDatabase.CreateFolder("Assets/Editor Default Resources", "SaintsHierarchy");
                }

                const string assetPath =
                    "Assets/Editor Default Resources/SaintsHierarchy/SaintsHierarchyConfig.asset";
                _config = AssetDatabase.LoadAssetAtPath<SaintsHierarchyConfig>(assetPath);
                // ReSharper disable once InvertIf
                if (_config == null && !File.Exists(assetPath))
                {
                    _config = ScriptableObject.CreateInstance<SaintsHierarchyConfig>();
                    Debug.Log("Create SaintsHierarchyConfig");
                    AssetDatabase.CreateAsset(_config,
                        assetPath);
                }
            }

            return _config;
        }

        public static void PopupConfig(Rect worldBound, GameObject go, SaintsHierarchyConfig.GameObjectConfig goConfig)
        {
            PopupWindow.Show(worldBound, new GameObjectConfigPopup(go, goConfig));
        }

        // public static GlobalObjectId ScenePrefabGidToUnpackedGid(GlobalObjectId id, string prefabId)
        // {
        //     string[] sourceSplit = id.ToString().Split('-');
        //     // string[] prefabSplit = prefabId.ToString().Split('-');
        //     // string prefabFileId = prefabSplit[prefabSplit.Length - 2];
        //     // sourceSplit[1] = "1";
        //     sourceSplit[2] = prefabId;
        //     sourceSplit[4] = "0";
        //     ulong fileId = (id.targetObjectId ^ id.targetPrefabId) & 0x7fffffffffffffff;
        //     // sourceSplit[3] = fileId.ToString();
        //
        //     var join = string.Join("-", sourceSplit);
        //
        //     // ReSharper disable once ConvertIfStatementToReturnStatement
        //     if (GlobalObjectId.TryParse(
        //             join,
        //             out GlobalObjectId unpackedGid))
        //     {
        //         return unpackedGid;
        //     }
        //
        //     return new GlobalObjectId();
        //     // // ulong fileId = (id.targetObjectId ^ id.targetPrefabId) & 0x7fffffffffffffff;
        //     // ulong fileId = (id.targetObjectId ^ prefabId) & 0x7fffffffffffffff;
        //     //
        //     // // ReSharper disable once ConvertIfStatementToReturnStatement
        //     // if (GlobalObjectId.TryParse(
        //     //         $"GlobalObjectId_V1-{id.identifierType}-{id.assetGUID}-{fileId}-0",
        //     //         out GlobalObjectId unpackedGid))
        //     // {
        //     //     return unpackedGid;
        //     // }
        //     //
        //     // return new GlobalObjectId();
        // }

        public static string GlobalObjectIdNormString(GlobalObjectId goId)
        {
            string goIdStringRaw = goId.ToString();
            string[] goIdSplit = goIdStringRaw.Split('-');
            goIdSplit[1] = "1";
            // goIdSplit[4] = "0";
            return string.Join('-', goIdSplit);
        }

        // public static string GlobalObjectIdNormStringNoPrefabLink(GlobalObjectId goId)
        // {
        //     string goIdStringRaw = goId.ToString();
        //     string[] goIdSplit = goIdStringRaw.Split('-');
        //     goIdSplit[1] = "1";
        //     goIdSplit[4] = "0";
        //     return string.Join('-', goIdSplit);
        // }

        private static readonly Dictionary<Type, IReadOnlyList<RenderTargetInfo>> CacheRenderTargetInfos =
            new Dictionary<Type, IReadOnlyList<RenderTargetInfo>>();

        public static IReadOnlyList<RenderTargetInfo> GetRenderTargetInfos(Component component)
        {
            if (component == null)
            {
                return Array.Empty<RenderTargetInfo>();
            }

            Type type = component.GetType();
            if (CacheRenderTargetInfos.TryGetValue(type, out IReadOnlyList<RenderTargetInfo> cached))
            {
                return cached;
            }

            List<Type> types = ReflectUtils.GetSelfAndBaseTypesFromType(type);
            types.Reverse();

            List<RenderTargetInfo> results = new List<RenderTargetInfo>();

            for (int inherentDepth = 0; inherentDepth < types.Count; inherentDepth++)
            {
                Type systemType = types[inherentDepth];
                MemberInfo[] members = systemType
                    .GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic |
                                BindingFlags.Public | BindingFlags.DeclaredOnly);
                IOrderedEnumerable<MemberInfo> memberLis =
                    members.OrderBy(memberInfo => memberInfo.MetadataToken);

                foreach (MemberInfo memberInfo in memberLis)
                {
                    Attribute[] allAttributes = memberInfo.GetCustomAttributes().ToArray();
                    foreach (Attribute attribute in allAttributes)
                    {
                        if (attribute is IHierarchyAttribute hierarchyAttribute)
                        {
                            MemberType memberType;
                            switch (memberInfo)
                            {
                                case FieldInfo _:
                                    memberType = MemberType.Field;
                                    break;
                                case PropertyInfo _:
                                    memberType = MemberType.Property;
                                    break;
                                case MethodInfo _:
                                    memberType = MemberType.Method;
                                    break;
                                default:
                                    continue;
                            }

                            RenderTargetInfo renderTargetInfo = new RenderTargetInfo(component, hierarchyAttribute, memberType,
                                memberInfo, inherentDepth);
                            results.Add(renderTargetInfo);
                        }
                    }
                }
            }
            return CacheRenderTargetInfos[type] = results;
        }

        public static (string error, T result) GetOf<T>(string by, T defaultValue, SerializedProperty property, MemberInfo memberInfo, object target, IReadOnlyList<object> overrideParams)
        {
            if (by.StartsWith(":"))
            {
                // ReSharper disable once ReplaceSubstringWithRangeIndexer
                return GetOfStatic(by.Substring(1), defaultValue, property, memberInfo, target, overrideParams);
            }

            if (target == null)
            {
                return ("Target is null", defaultValue);
            }

            return by.Contains(".")
                ? AccGetOf(by, defaultValue, property, target, overrideParams)
                : FlatGetOf(by, defaultValue, property, memberInfo, target, overrideParams);
        }

        private static (string error, T result) GetOfStatic<T>(string nameSpaceAndName, T defaultValue, SerializedProperty property, MemberInfo memberInfo, object target, IReadOnlyList<object> overrideParams)
        {
            List<string> split = new List<string>(nameSpaceAndName.Split('.'));
            int totalLength = split.Count;
            if (totalLength == 0)
            {
                return ($"Static/Const callback must be in form of `Namespace.ClassName.FieldNameOrMethodName` or `ClassName.FieldNameOrMethodName`, get {nameSpaceAndName}", defaultValue);
            }

            bool fullSearch = totalLength > 1;
            // Debug.Log(totalLength);
            if (totalLength == 1)
            {
                split.Insert(0, target.GetType().Name);
                totalLength = 2;
            }

            string fieldOrMethod = split[totalLength - 1];
            split.RemoveAt(totalLength - 1);
            Type type = null;
            if(target != null)
            {
                Assembly assembly = target.GetType().Assembly;
                type = FindTypeInAssembly(assembly, split);
            }
            if (type == null && fullSearch)
            {
                foreach (Assembly searchAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = FindTypeInAssembly(searchAssembly, split);
                    if (type != null)
                    {
                        break;
                    }
                }
            }
            if (type == null)
            {
                return ($"type name `{string.Join(".", split)}` not found", defaultValue);
            }

            const BindingFlags bindAttr = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public |
                                          BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy;

            FieldInfo fieldInfo = type.GetField(fieldOrMethod, bindAttr);
            if (fieldInfo != null)
            {
                object genResult;
                try
                {
                    genResult = fieldInfo.GetValue(null);
                }
                catch (Exception e)
                {
                    // _error = e.Message;
#if SAINTSFIELD_DEBUG
                    Debug.LogException(e);
#endif
                    return (e.Message, defaultValue);
                }

                return ConvertTo(genResult, defaultValue);
            }

            PropertyInfo propertyInfo = type.GetProperty(fieldOrMethod, bindAttr);
            if (propertyInfo != null)
            {
                object genResult;
                try
                {
                    genResult = propertyInfo.GetValue(null);
                }
                catch (Exception e)
                {
#if SAINTSFIELD_DEBUG
                    Debug.LogException(e);
#endif
                    return (e.Message, defaultValue);
                }

                return ConvertTo(genResult, defaultValue);
            }

            MethodInfo[] methodInfos = type.GetMethods(bindAttr);
            if (methodInfos.Length == 0)
            {
#if SAINTSFIELD_DEBUG
                Debug.LogWarning($"No field, property or method found for {nameSpaceAndName}");
#endif
                return ($"No field, property or method found for {nameSpaceAndName}", defaultValue);
            }

            List<string> errors = new List<string>();
            foreach (MethodInfo methodInfo in methodInfos)
            {
                if(methodInfo.Name == fieldOrMethod)
                {
                    (string error, object returnValue) =
                        InvokeMethodInfo(methodInfo, defaultValue, property, memberInfo, target, overrideParams);
                    if (error == "")
                    {
                        return ConvertTo(returnValue, defaultValue);
                    }

                    errors.Add(error);
                }
            }

            if (errors.Count == 0)
            {
                return ($"No method/field/property {fieldOrMethod} found", defaultValue);
            }

            string finalError = string.Join("\n", errors);

#if SAINTSFIELD_DEBUG
            Debug.LogWarning(finalError);
#endif

            return (finalError, defaultValue);
        }

        private static Type FindTypeInAssembly(Assembly assembly, IReadOnlyList<string> split)
        {
            return split.Count > 1
                ? assembly.GetType(string.Join(".", split), false)
                : assembly.GetTypes().FirstOrDefault(t => t.Name == split[0]);
        }

        private static (string error, T result) AccGetOf<T>(string by, T defaultValue, SerializedProperty property,
            object parent, IReadOnlyList<object> overrideParams)
        {
            string accBy = by;
            // SerializedProperty accProperty = property;
            object accParent = parent;
            if (by.StartsWith("../"))
            {
                string error;
                (error, accBy, accParent) = UpwardWalk(by, property);
                if (error != "")
                {
                    return (error, defaultValue);
                }
            }

            // Don't use this: the memberInfo need to change
            // if (!accBy.Contains("."))
            // {
            //     return FlatGetOf(accBy, defaultValue, property, memberInfo, accParent);
            // }

            // Debug.Log($"looking for {accBy} in {accParent}");

            // MemberInfo accMemberInfo = memberInfo;
            (string error, T result) thisResult = ("No Attributes", defaultValue);

            foreach (string attrName in accBy.Split('.'))
            {
                MemberInfo accMemberInfo = null;
                foreach (Type type in ReflectUtils.GetSelfAndBaseTypesFromInstance(accParent))
                {
                    MemberInfo[] members = type.GetMember(attrName,
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.FlattenHierarchy);
                    if (members.Length <= 0)
                    {
                        continue;
                    }
                    accMemberInfo = members[0];
                    break;
                }

                thisResult = FlatGetOf(attrName, defaultValue, property, accMemberInfo, accParent, overrideParams);
                // Debug.Log($"{attrName} = {thisResult.result}({thisResult.error})");
                if (thisResult.error != "")
                {
                    return thisResult;
                }
                accParent = thisResult.result;
            }
            return thisResult;

        }

        private static (string error, T result) FlatGetOf<T>(string by, T defaultValue, SerializedProperty property, MemberInfo memberInfo, object target, IReadOnlyList<object> overrideParams)
        {
            if (by.StartsWith(":"))
            {
                // ReSharper disable once ReplaceSubstringWithRangeIndexer
                return GetOfStatic(by.Substring(1), defaultValue, property, memberInfo, target, overrideParams);
            }

            if (target == null)
            {
                return ("Target is null", defaultValue);
            }

            foreach (Type type in ReflectUtils.GetSelfAndBaseTypesFromInstance(target))
            {
                ReflectUtils.GetPropType getPropType;
                object fieldOrMethodInfo;
                try
                {
                    (getPropType, fieldOrMethodInfo) = ReflectUtils.GetProp(type, by);
                }
                catch (AmbiguousMatchException)
                {
                    List<MethodInfo> methodInfos = new List<MethodInfo>();
                    foreach (MethodInfo methodInfo in type.GetMethods(ReflectUtils.FindTargetBindAttr))
                    {
                        if (methodInfo.Name == by)
                        {
                            methodInfos.Add(methodInfo);
                            (string methodError, object methodReturnValue) = InvokeMethodInfo(methodInfo, defaultValue, property, memberInfo, target, overrideParams);
                            if (methodError == "")
                            {
                                return ConvertTo(methodReturnValue, defaultValue);
                            }
                        }
                    }
                    return ($"All method failed to match the signature: {string.Join("; ", methodInfos.Select(eachMethod => $"({string.Join(", ", eachMethod.GetParameters().Select(each => $"{each.ParameterType} {each.Name}{(each.HasDefaultValue? $"={each.DefaultValue}": "")}"))}) => {eachMethod.ReturnParameter}"))}", defaultValue);
                }

                object genResult;
                switch (getPropType)
                {
                    case ReflectUtils.GetPropType.NotFound:
                        continue;

                    case ReflectUtils.GetPropType.Property:
                        genResult = ((PropertyInfo)fieldOrMethodInfo).GetValue(target);
                        break;
                    case ReflectUtils.GetPropType.Field:
                    {
                        FieldInfo fInfo = (FieldInfo)fieldOrMethodInfo;
                        genResult = fInfo.GetValue(target);
                        // Debug.Log($"{fInfo}/{fInfo.Name}, target={target} genResult={genResult}");
                    }
                        break;
                    case ReflectUtils.GetPropType.Method:
                    {
                        MethodInfo methodInfo = (MethodInfo)fieldOrMethodInfo;

                        (string methodError, object methodReturnValue) = InvokeMethodInfo(methodInfo, defaultValue, property, memberInfo, target, overrideParams);
                        if (methodError != "")
                        {
                            return (methodError, defaultValue);
                        }

                        genResult = methodReturnValue;

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(getPropType), getPropType, null);
                }

                // Debug.Log($"GetOf {genResult}/{genResult?.GetType()}/{genResult==null}");
                return ConvertTo(genResult, defaultValue);
            }

            return ($"No field or method named `{by}` found on `{target}`", defaultValue);
        }

        private static (string error, T result) ConvertTo<T>(object genResult, T defaultValue)
        {
            T finalResult;
            try
            {
                // finalResult = (T)genResult;
                finalResult = (T)Convert.ChangeType(genResult, typeof(T));
            }
            catch (InvalidCastException)
            {
                // Debug.Log($"{genResult}/{genResult.GetType()} -> {typeof(T)}");
                // Debug.LogException(e);
                // return (e.Message, defaultValue);
                if (typeof(T) == typeof(string))
                {
                    finalResult = (T)Convert.ChangeType(genResult == null? "": genResult.ToString(), typeof(T));
                }
                else
                {
                    try
                    {
                        finalResult = (T)genResult;
                    }
                    catch (InvalidCastException e)
                    {
                        Debug.LogException(e);
                        Debug.LogError($"{genResult} -> {typeof(T)}");
                        return (e.Message, defaultValue);
                    }
                }
            }
            catch (FormatException)
            {
                try
                {
                    finalResult = (T)genResult;
                }
                catch (InvalidCastException e)
                {
                    Debug.LogError($"Failed to convert {genResult} to type {typeof(T)}");
                    Debug.LogException(e);
                    return (e.Message, defaultValue);
                }
            }

            return ("", finalResult);
        }

        private static (string error, object returnValue) InvokeMethodInfo(MethodInfo methodInfo, object defaultValue, SerializedProperty property, MemberInfo memberInfo, object target, IReadOnlyList<object> overrideParams)
        {
            object[] passParams;
            if (property == null || memberInfo == null || target == null)
            {
                passParams = Array.Empty<object>();
            }
            else
            {
                (string error, int arrayIndex, object curValue) = GetValue(property, memberInfo, target);
                if (error != "")
                {
                    return (error, defaultValue);
                }

                IReadOnlyList<object> baseParams;
                if (overrideParams != null)
                {
                    baseParams = overrideParams;
                }
                else
                {
                    baseParams = arrayIndex == -1
                        ? new[]
                        {
                            curValue,
                        }
                        : new[]
                        {
                            curValue,
                            arrayIndex,
                        };
                }

                string paramError;
                (paramError, passParams) = ReflectUtils.MethodParamsFill(methodInfo.GetParameters(), baseParams);
                if (paramError != "")
                {
                    return (paramError, null);
                }

#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_UTIL_GET_OF
                   Debug.Log($"#Util# arrayIndex={arrayIndex}, rawValue={rawValue}, curValue={curValue}, fill={string.Join(",", passParams)}");
#endif
            }

            object genResult;
            try
            {
                genResult = methodInfo.Invoke(target, passParams);
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e);
                Debug.Assert(e.InnerException != null);
                return (e.InnerException.Message, defaultValue);
            }
            catch (Exception e)
            {
                // _error = e.Message;
                Debug.LogException(e);
                return (e.Message, defaultValue);
            }

            return ("", genResult);
        }

        private static (string error, int index, object value) GetValue(SerializedProperty property, MemberInfo fieldInfo, object parent)
        {
            int arrayIndex;
            if (property == null)
            {
                arrayIndex = -1;
            }
            else
            {
                try
                {
                    arrayIndex = SerializedUtils.PropertyPathIndex(property.propertyPath);
                }
                catch (NullReferenceException e)
                {
                    return (e.Message, -1, null);
                }
                catch (ObjectDisposedException e)
                {
                    return (e.Message, -1, null);
                }
            }

            return GetValueAtIndex(arrayIndex, fieldInfo, parent);
        }

        private static (string error, int index, object value) GetValueAtIndex(int arrayIndex, MemberInfo fieldInfo, object parent)
        {
            if (fieldInfo == null)
            {
#if SAINTSFIELD_DEBUG
                Debug.LogError($"MemberInfo is null");
#endif
                return ("No MemberInfo given", arrayIndex, null);
            }

            if (parent == null)
            {
                return ("No parent given", arrayIndex, null);
            }

            object rawValue;
            if (fieldInfo.MemberType == MemberTypes.Field)
            {
                rawValue = ((FieldInfo)fieldInfo).GetValue(parent);
            }
            else if (fieldInfo.MemberType == MemberTypes.Property)
            {
                rawValue = ((PropertyInfo)fieldInfo).GetValue(parent);
            }
            else
            {
                return ($"Unable to get value from {fieldInfo} ({fieldInfo.MemberType})", -1, null);
            }

            if (arrayIndex == -1)
            {
                return ("", -1, rawValue);
            }

            // Debug.Log($"get value at {arrayIndex} from rawValue {((IEnumerable)rawValue).Cast<object>().Count()}");
            (string indexError, object indexResult) = GetValueAtIndexFromCollection(rawValue, arrayIndex);
            if (indexError != "")
            {
                return (indexError, -1, null);
            }

            return ("", arrayIndex, indexResult);
        }

        private static (string error, string by, object parent) UpwardWalk(string by, SerializedProperty property)
        {
            Debug.Assert(by.StartsWith("../"));
            string[] split = by.Split("../");

            int splitCount = split.Length;

            int upWalkCount = splitCount - 1;
            string leftBy = split[splitCount - 1];

            string originPath = property.propertyPath;
            string[] propPaths = originPath.Split('.');
            (bool arrayTrim, string[] propPathSegments) = SerializedUtils.TrimEndArray(propPaths);
            if (arrayTrim)
            {
                propPaths = propPathSegments;
            }

            IReadOnlyList<(SerializedUtils.FieldOrProp fieldOrProp, object parent)> walkable = SerializedUtils.GetFieldInfoAndParentListByPathSegments(property.serializedObject.targetObject, propPaths);
            if (walkable.Count < upWalkCount - 1)  // skip self
            {
                return (
                    $"Can not walk upward for {upWalkCount} steps when only {walkable.Count} parents found: {string.Join(", ", walkable.Select(each => each.parent))}",
                    null, null);
            }

            (SerializedUtils.FieldOrProp _, object walkParent) = walkable[upWalkCount];

            // Debug.Log($"upWalkCount-1={upWalkCount-1}");
            // foreach ((SerializedUtils.FieldOrProp fieldOrProp, object fieldParent)  in walkable)
            // {
            //     Debug.Log($"{fieldOrProp}, {fieldParent}");
            // }

            // return (leftBy parent);

            return ("", leftBy, walkParent);
        }

        public static (string error, object result) GetValueAtIndexFromCollection(object source, int index)
        {
            // ReSharper disable once UseNegatedPatternInIsExpression
            if (!(source is IEnumerable enumerable))
            {
                throw new Exception($"Not a enumerable {source}");
            }

            if (source is Array arr)
            {
                object result;
                try
                {
                    result = arr.GetValue(index);
                }
                catch (IndexOutOfRangeException e)
                {
                    return (e.Message, null);
                }

                return ("", result);
            }
            if (source is IList list)
            {
                object result;
                try
                {
                    result = list[index];
                }
                catch (ArgumentOutOfRangeException e)
                {
                    return (e.Message, null);
                }

                return ("", result);
            }

            // Debug.Log($"start check index in {source}");
            int searchIndex = 0;
            foreach (object result in enumerable.Cast<object>())
            {
                // Debug.Log($"check index {searchIndex} in {source}");
                if(searchIndex == index)
                {
                    return ("", result);
                }

                searchIndex++;
            }

            return ($"Not found index {index} in {source}", null);
        }

        public static string FormatBinary(string formatControl, object value)
        {
            if (value is IFormattable iFormattable)
            {
                try
                {
                    return iFormattable.ToString(formatControl, null);
                }
                catch (Exception)
                {
                    // do nothing
                }
            }

            int bitLength = 0;
            if (formatControl.Length >= 2)
            {
                // ReSharper disable once ReplaceSubstringWithRangeIndexer
                string lengthPart = formatControl.Substring(1);
                // Debug.Log(lengthPart);
                if (!int.TryParse(lengthPart, out bitLength))
                {
                    return "";
                }
            }

            // Debug.Log($"ConvertToBinary {value}/{bitLength}");
            return ConvertToBinary(value, bitLength);
        }

        private static string ConvertToBinary(object value, int bitLength)
        {
            if (value == null)
            {
                return "";
                // throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            long numericValue;

            if (value is int intValue)
            {
                numericValue = intValue;
            }
            else if (value is long longValue)
            {
                numericValue = longValue;
            }
            else if (value is short shortValue)
            {
                numericValue = shortValue;
            }
            else if (value is byte byteValue)
            {
                numericValue = byteValue;
            }
            else if (value is uint uintValue)
            {
                numericValue = uintValue;
            }
            else if (value is ulong ulongValue)
            {
                numericValue = (long)ulongValue;
            }
            else if (value is sbyte sbyteValue)
            {
                numericValue = sbyteValue;
            }
            else if (value is Enum)
            {
                numericValue = Convert.ToInt64(value);
            }
            else
            {
#if SAINTSFIELD_DEBUG
                Debug.LogError($"Unsupported type for binary conversion: {value.GetType()}");
#endif
                return "";
            }

            string binaryString = Convert.ToString(numericValue, 2);

            return bitLength <= 0
                ? binaryString
                : binaryString.PadLeft(bitLength, '0');
        }
    }
}
