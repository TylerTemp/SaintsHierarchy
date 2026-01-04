using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public static class ReflectUtils
    {
        public enum GetPropType
        {
            NotFound,
            Property,
            Field,
            Method,
        }

        public static List<Type> GetSelfAndBaseTypesFromInstance(object target)
        {
            return GetSelfAndBaseTypesFromType(target.GetType());
        }

        public static List<Type> GetSelfAndBaseTypesFromType(Type thisType)
        {
            List<Type> types = new List<Type>(1)
            {
                thisType,
            };

            // ReSharper disable once UseIndexFromEndExpression
            while (types[types.Count - 1].BaseType != null)
            {
                // ReSharper disable once UseIndexFromEndExpression
                types.Add(types[types.Count - 1].BaseType);
            }

            // types.Reverse();

            return types;
        }

        public const BindingFlags FindTargetBindAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic |
                                                       BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy;

        public static (GetPropType getPropType, object fieldOrMethodInfo) GetProp(Type targetType, string fieldName)
        {
            FieldInfo fieldInfo = targetType.GetField(fieldName, FindTargetBindAttr);
            // Debug.Log($"init get fieldInfo {fieldInfo}");
            if (fieldInfo == null)
            {
                fieldInfo = targetType.GetField($"<{fieldName}>k__BackingField", FindTargetBindAttr);
            }
            if (fieldInfo != null)
            {
                return (GetPropType.Field, fieldInfo);
            }

            PropertyInfo propertyInfo = targetType.GetProperty(fieldName, FindTargetBindAttr);
            if (propertyInfo != null)
            {
                return (GetPropType.Property, propertyInfo);
            }

            MethodInfo methodInfo = targetType.GetMethod(fieldName, FindTargetBindAttr);
            // Debug.Log($"methodInfo={methodInfo}, fieldName={fieldName}, targetType={targetType}/FlattenHierarchy={bindAttr.HasFlagFast(BindingFlags.FlattenHierarchy)}");
            return methodInfo == null ? (GetPropType.NotFound, null) : (GetPropType.Method, methodInfo);

        }

        private class MethodParamFiller
        {
            public string Name;
            public bool IsOptional;
            public object DefaultValue;

            public bool Signed;
            public object Value;
        }

        public static (string error, object[] filled) MethodParamsFill(IReadOnlyList<ParameterInfo> methodParams, IEnumerable<object> toFillValues)
        {
            // first we just sign default value and null value
            MethodParamFiller[] filledValues = methodParams
                .Select(param => param.IsOptional
                    ? new MethodParamFiller
                    {
                        Name = param.Name,
                        IsOptional = true,
                        DefaultValue = param.DefaultValue,
                    }
                    : new MethodParamFiller
                    {
                        Name = param.Name,
                    })
                .ToArray();
            // then we check for each params:
            // 1.  If there are required params, fill the value
            // 2.  Then, if there are left value to fill and can match the optional type, then fill it
            // 3.  Ensure all required params are filled
            // 4.  Return.

            Queue<object> toFillQueue = new Queue<object>(toFillValues);
            Queue<object> leftOverQueue = new Queue<object>();
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_CALLBACK
            Debug.Log($"toFillQueue.Count={toFillQueue.Count}");
#endif
            // required:
            for (int methodParamIndex = 0; methodParamIndex < methodParams.Count; methodParamIndex++)
            {
                // ReSharper disable once InvertIf
                if(!methodParams[methodParamIndex].IsOptional)
                {
                    // Debug.Log($"checking {index}={methodParams[index].Name}");
                    // Debug.Assert(toFillQueue.Count > 0, $"Nothing to fill required parameter {methodParams[index].Name}");
                    if (toFillQueue.Count == 0)
                    {
                        string message = $"Nothing to fill required parameter {methodParams[methodParamIndex].Name}";
#if SAINTSFIELD_DEBUG
                        Debug.LogWarning(message);
#endif
                        return (message, null);
                    }
                    while(toFillQueue.Count > 0)
                    {
                        object value = toFillQueue.Dequeue();
                        Type paramType = methodParams[methodParamIndex].ParameterType;
                        // Debug.Log($"{value} -> {paramType}");
                        if (value == null || paramType.IsInstanceOfType(value) || CheckSignEnum(value, paramType))
                        {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_CALLBACK
                            Debug.Log($"Push value {value} for {methodParams[index].Name}");
#endif
                            filledValues[methodParamIndex].Value = value;
                            filledValues[methodParamIndex].Signed = true;
                            break;
                        }
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_CALLBACK
                        Debug.Log($"No push value {value}({value.GetType()}) for {methodParams[index].Name}({paramType})");
#endif

                        // Debug.Log($"Skip value {value} for {methodParams[index].Name}");
                        leftOverQueue.Enqueue(value);
                        // Debug.Assert(valueType == paramType || valueType.IsSubclassOf(paramType),
                        //     $"The value type `{valueType}` is not match the param type `{paramType}`");
                        // Debug.Log($"Add {value} at {index}");

                    }
                }
            }

            foreach (object leftOver in toFillQueue)
            {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_CALLBACK
                Debug.Log($"leftOver: {leftOver}");
#endif
                leftOverQueue.Enqueue(leftOver);
            }

            // optional:
            if(leftOverQueue.Count > 0)
            {
                for (int index = 0; index < methodParams.Count; index++)
                {
                    if (leftOverQueue.Count == 0)
                    {
                        break;
                    }

                    if (methodParams[index].IsOptional)
                    {
                        object value = leftOverQueue.Peek();
                        Type paramType = methodParams[index].ParameterType;
                        if(value == null || paramType.IsInstanceOfType(value))
                        {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_CALLBACK
                            Debug.Log($"add optional: {value} -> {methodParams[index].Name}({paramType})");
#endif
                            leftOverQueue.Dequeue();
                            filledValues[index].Value = value;
                            filledValues[index].Signed = true;
                        }
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_CALLBACK
                        else
                        {
                            Debug.Log($"not fit optional: {value}({value.GetType()}) -> {paramType}");
                        }
#endif
                    }
                }
            }

            object[] results = new object[filledValues.Length];
            int filledValuesIndex = 0;
            foreach (MethodParamFiller each in filledValues)
            {
                if (each.Signed)
                {
                    results[filledValuesIndex] = each.Value;
                    // return each.Value;
                }
                else if (each.IsOptional)
                {
                    results[filledValuesIndex] = each.DefaultValue;
                }
                else
                {
                    string message = $"No value for required parameter `{each.Name}` in method.";
#if SAINTSFIELD_DEBUG
                    Debug.LogWarning(message);
#endif
                    return (message, null);
                }

                filledValuesIndex++;
            }

            return ("", results.ToArray());
            // return filledValues.Select(each =>
            // {
            //     if (each.Signed)
            //     {
            //         return each.Value;
            //     }
            //     Debug.Assert(each.IsOptional, $"No value for required parameter `{each.Name}` in method.");
            //     return each.DefaultValue;
            // }).ToArray();
        }

        private static bool CheckSignEnum(object value, Type paramType)
        {
            return value is int && paramType.IsSubclassOf(typeof(Enum));
        }
    }
}
