using System;
using System.Reflection;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Core.Draw
{
    public readonly struct RenderTargetInfo : IEquatable<RenderTargetInfo>, IRichTextTagProvider
    {
        private readonly Component _target;

        public readonly IHierarchyAttribute Attribute;
        public readonly MemberType MemberType;
        public readonly MemberInfo MemberInfo;
        public readonly int SortOrder;

        private readonly string _labelName;
        private readonly string _containerType;
        private readonly string _containerTypeBaseType;

        public RenderTargetInfo(Component target, IHierarchyAttribute attribute, MemberType memberType, MemberInfo memberInfo, int sortOrder)
        {
            _target = target;

            Attribute = attribute;
            MemberType = memberType;
            MemberInfo = memberInfo;
            SortOrder = sortOrder;

            (_containerType, _containerTypeBaseType) = GetType(target.GetType());

            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    _labelName = ObjectNames.NicifyVariableName(fieldInfo.Name);
                    break;
                case PropertyInfo propertyInfo:
                    _labelName = ObjectNames.NicifyVariableName(propertyInfo.Name);
                    break;
                case MethodInfo methodInfo:
                    _labelName = ObjectNames.NicifyVariableName(methodInfo.Name);
                    break;
                default:
                    _labelName = "";
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
            return _labelName;
        }

        public string GetContainerType()
        {
            return _containerType;
        }

        public string GetContainerTypeBaseType()
        {
            return _containerTypeBaseType;
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
                        return fieldInfo.GetValue(_target)?.ToString();
                    }
                    catch (Exception)
                    {
                        return "";
                    }
                case PropertyInfo propertyInfo:
                    try
                    {
                        return propertyInfo.GetValue(_target)?.ToString();
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
