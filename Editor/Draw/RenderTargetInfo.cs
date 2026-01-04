using System;
using System.Reflection;
using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Draw
{
    public readonly struct RenderTargetInfo : IEquatable<RenderTargetInfo>, IRichTextTagProvider
    {
        private readonly Component Target;

        public readonly IHierarchyAttribute Attribute;
        public readonly MemberType MemberType;
        public readonly MemberInfo MemberInfo;
        public readonly int SortOrder;

        private readonly string LabelName;
        private readonly string ContainerType;
        private readonly string ContainerTypeBaseType;

        public RenderTargetInfo(Component target, IHierarchyAttribute attribute, MemberType memberType, MemberInfo memberInfo, int sortOrder)
        {
            Target = target;

            Attribute = attribute;
            MemberType = memberType;
            MemberInfo = memberInfo;
            SortOrder = sortOrder;

            (ContainerType, ContainerTypeBaseType) = GetType(target.GetType());

            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    LabelName = ObjectNames.NicifyVariableName(fieldInfo.Name);
                    break;
                case PropertyInfo propertyInfo:
                    LabelName = ObjectNames.NicifyVariableName(propertyInfo.Name);
                    break;
                case MethodInfo methodInfo:
                    LabelName = ObjectNames.NicifyVariableName(methodInfo.Name);
                    break;
                default:
                    LabelName = "";
                    break;
            }
        }

        private static (string, string) GetType(Type type)
        {
            if (type == null)
            {
                return ("", "");
            }
            return (type.Name, type.BaseType?.Name ?? "");
        }

        public override string ToString() => $"<RenderTargetInfo {MemberInfo.Name} {Attribute.GetType()} {Attribute.IsLeft} {Attribute.GroupBy} />";

        public bool Equals(RenderTargetInfo other)
        {
            return Equals(Attribute, other.Attribute) && MemberType == other.MemberType && Equals(MemberInfo, other.MemberInfo);
        }

        public override bool Equals(object obj)
        {
            return obj is RenderTargetInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Attribute, (int)MemberType, MemberInfo);
        }

        public string GetLabel()
        {
            return LabelName;
        }

        public string GetContainerType()
        {
            return ContainerType;
        }

        public string GetContainerTypeBaseType()
        {
            return ContainerTypeBaseType;
        }

        public string GetIndex(string formatter)
        {
            return "";
        }

        public string GetField(string rawContent, string tagName, string tagValue)
        {
            switch (MemberInfo)
            {
                case FieldInfo fieldInfo:
                    try
                    {
                        return fieldInfo.GetValue(Target)?.ToString();
                    }
                    catch (Exception)
                    {
                        return "";
                    }
                case PropertyInfo propertyInfo:
                    try
                    {
                        return propertyInfo.GetValue(Target)?.ToString();
                    }
                    catch (Exception)
                    {
                        return "";
                    }
                default:
                    return "";
            }
        }
    }
}
