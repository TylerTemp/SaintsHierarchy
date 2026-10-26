using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SaintsHierarchy.Editor.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor.Legacy
{
    // Owns legacy IMGUI interception; menu rendering and customization stay in Core.
    internal sealed class HierarchyContextMenuAdapter
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        private static readonly FieldInfo CurrentEvent = typeof(Event).GetField("s_Current", Flags);
        private readonly EditorWindow _window;
        private readonly HierarchyContextMenu _menu;

        public HierarchyContextMenuAdapter(EditorWindow window)
        {
            _window = window;
            _menu = new HierarchyContextMenu(window);
        }

        public void Dispose() => _menu.Dispose();

        // Legacy adapter: install only for this draw, after Unity's selection handling and before its popup callback.
        public void Draw(Action original)
        {
            Event evt = Event.current;
            if (!HierarchyContextMenu.Enabled || evt == null || evt.type != EventType.ContextClick || _window.rootVisualElement.panel == null)
            {
                original();
                return;
            }
            List<Action> restore = new List<Action>();
            try
            {
                object hierarchy = Get(_window, "m_SceneHierarchy");
                object tree = hierarchy == null ? null : Get(hierarchy, "m_TreeView");
                if (tree != null)
                {
                    PropertyInfo callback = tree.GetType().GetProperty("contextClickItemCallback", Flags);
                    if (callback?.GetValue(tree) is Delegate previous && previous.GetInvocationList().Length == 1
                        && previous.Method.DeclaringType == hierarchy.GetType())
                    {
                        Type idType = previous.GetType().GetGenericArguments()[0];
                        MethodInfo factory = GetType().GetMethod(nameof(ItemCallback), Flags)!.MakeGenericMethod(idType);
                        Delegate installed = (Delegate)factory.Invoke(this, new object[] { previous, hierarchy, tree });
                        callback.SetValue(tree, installed);
                        restore.Add(() =>
                        {
                            if (Equals(callback.GetValue(tree), installed)) callback.SetValue(tree, previous);
                        });
                    }
                    PropertyInfo outside = tree.GetType().GetProperty("contextClickOutsideItemsCallback", Flags);
                    if (outside?.GetValue(tree) is Action oldOutside && oldOutside.GetInvocationList().Length == 1
                        && oldOutside.Method.DeclaringType == hierarchy.GetType())
                    {
                        Action installed = () => OutsideItems(hierarchy, oldOutside);
                        outside.SetValue(tree, installed);
                        restore.Add(() =>
                        {
                            if (Equals(outside.GetValue(tree), installed)) outside.SetValue(tree, oldOutside);
                        });
                    }
                }
            }
            catch (Exception exception)
            {
                _menu.ReportFailure(exception);
                for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
                restore.Clear();
            }
            try
            {
                original();
            }
            finally
            {
                for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
            }
        }

        private Action<T> ItemCallback<T>(Delegate previous, object hierarchy, object tree) => id =>
        {
            bool handled = false;
            try
            {
                object item = Call(tree, "FindItem", id);
                GameObject go = item == null ? null : Get(item, "objectPPTR") as GameObject;
                // Scene headers and SubScenes keep Unity's original menu.
                if (go != null && !IsSubScene(go))
                {
                    handled = ShowLegacy(hierarchy,
#if UNITY_6000_4_OR_NEWER
                        go.GetEntityId()
#else
                        go.GetInstanceID()
#endif
                    );
                }
            }
            catch (Exception exception)
            {
                _menu.ReportFailure(exception);
            }
            if (!handled) previous.DynamicInvoke(id);
        };

        private void OutsideItems(object hierarchy, Action native)
        {
            Object[] selection = Selection.objects;
            Object active = Selection.activeObject;
            bool handled = false;
            try
            {
                // Match Unity: creations from blank space must not become children of the old selection.
                Selection.activeObject = null;
                handled = ShowLegacy(hierarchy, null);
            }
            catch (Exception exception)
            {
                _menu.ReportFailure(exception);
            }
            if (!handled)
            {
                Selection.objects = selection;
                Selection.activeObject = active;
                native();
            }
        }

        private static bool IsSubScene(GameObject go)
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SubSceneGUI");
            MethodInfo method = type?.GetMethod("GetSubSceneInfo", Flags, null, new[] { typeof(GameObject) }, null);
            object info = method?.Invoke(null, new object[] { go });
            return info != null && Get(info, "isValid") is true;
        }

        private bool ShowLegacy(object hierarchy, object id)
        {
            GenericMenu menu = new GenericMenu();
            MethodInfo build = hierarchy.GetType().GetMethod("CreateGameObjectContextClick", Flags)
                ?? throw new MissingMethodException(hierarchy.GetType().FullName, "CreateGameObjectContextClick");
            Type idType = build.GetParameters()[1].ParameterType;
            if (id == null)
            {
                id = Activator.CreateInstance(idType);
            }
            else if (!idType.IsInstanceOfType(id))
            {
                MethodInfo convert = Array.Find(id.GetType().GetMethods(Flags), method =>
                    method.Name == "op_Implicit" && method.ReturnType == idType
                    && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == id.GetType());
                id = convert?.Invoke(null, new[] { id }) ?? throw new InvalidOperationException("Unsupported Hierarchy identifier.");
            }

            if (CurrentEvent == null)
            {
                throw new MissingFieldException(typeof(Event).FullName, "s_Current");
            }
            // prevent the default menu pop in legacy
            object current = CurrentEvent.GetValue(null);
            try
            {
                CurrentEvent.SetValue(null, null);
                build.Invoke(hierarchy, new[] { menu, id });
            }
            finally
            {
                CurrentEvent.SetValue(null, current);
            }
            bool shown = _menu.Show(Read(menu), GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
            if (shown)
            {
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }
            return shown;
        }

        // Copy values and callbacks out of Unity's invocation-owned menu. Never retain a pooled menu or event.
        private static List<HierarchyContextUtil.MenuItem> Read(GenericMenu menu)
        {
            IEnumerable items = Get(menu, "menuItems") as IEnumerable
                ?? Get(menu, "m_MenuItems") as IEnumerable
                ?? throw new MissingMemberException(typeof(GenericMenu).FullName, "menuItems");
            List<HierarchyContextUtil.MenuItem> source = new List<HierarchyContextUtil.MenuItem>();
            foreach (object item in items)
            {
                GUIContent content = (GUIContent)Get(item, "content");
                bool separator = (bool)Get(item, "separator");
                GenericMenu.MenuFunction function = Get(item, "func") as GenericMenu.MenuFunction;
                GenericMenu.MenuFunction2 function2 = Get(item, "func2") as GenericMenu.MenuFunction2;
                object data = Get(item, "userData");
                Action execute = function2 != null ? () => function2(data)
                    : function != null ? () => function() : (Action)null;
                source.Add(HierarchyContextUtil.Item(content.text, separator, execute != null, (bool)Get(item, "on"), execute));
            }
            return source;
        }

        private static object Get(object target, string name)
        {
            Type type = target.GetType();
            return type.GetField(name, Flags)?.GetValue(target)
                ?? type.GetProperty(name, Flags)?.GetValue(target);
        }

        private static object Call(object target, string name, params object[] arguments)
        {
            MethodInfo method = Array.Find(target.GetType().GetMethods(Flags),
                candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, name);
            }
            return method.Invoke(target, arguments);
        }
    }
}
