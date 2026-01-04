using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor.Draw
{
    public static class HierarchyDrawDrawer
    {
        public static (bool used, HierarchyUsed headerUsed) Draw(Object target, HierarchyArea hierarchyArea, RenderTargetInfo renderTargetInfo)
        {
            MethodInfo method = (MethodInfo)renderTargetInfo.MemberInfo;

            ParameterInfo[] methodParams = method.GetParameters();
            object[] methodPass = new object[methodParams.Length];
            methodPass[0] = hierarchyArea;
#if SAINTSHIERARCHY_SAINTSFIELD
            {
                ParameterInfo checkFirstParam = methodParams[0];
                if (checkFirstParam.ParameterType == typeof(SaintsField.ComponentHeader.HeaderArea))
                {
                    methodPass[0] = new SaintsField.ComponentHeader.HeaderArea(
                        hierarchyArea.Y,
                        hierarchyArea.Height,
                        hierarchyArea.TitleStartX,
                        hierarchyArea.TitleEndX,
                        hierarchyArea.SpaceStartX,
                        hierarchyArea.SpaceEndX,
                        hierarchyArea.GroupStartX,
                        hierarchyArea.GroupUsedRect
                    );
                }
            }
#endif
            for (int index = 1; index < methodParams.Length; index++)
            {
                ParameterInfo param = methodParams[index];
                object defaultValue = param.DefaultValue;
                methodPass[index] = defaultValue;
            }

            HierarchyUsed methodReturn;
            object methodReturnRaw;
            try
            {
                methodReturnRaw = method.Invoke(target, methodPass);
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException ?? e);
                return (false, default);
            }

            switch (methodReturnRaw)
            {
                case HierarchyUsed methodReturnHierarchy:
                    methodReturn = methodReturnHierarchy;
                    break;
#if SAINTSHIERARCHY_SAINTSFIELD
                case SaintsField.ComponentHeader.HeaderUsed headerUsed:
                    methodReturn = new HierarchyUsed(headerUsed.UsedRect);
                    break;
#endif
                default:
                    Debug.LogWarning($"return {methodReturnRaw} is not supported");
                    return (false, default);
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!methodReturn.Used)
            {
                return (false, default);
            }

            return (true, methodReturn);
        }
    }
}
