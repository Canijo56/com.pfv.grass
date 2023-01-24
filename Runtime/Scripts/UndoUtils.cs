using System;

using UnityEngine;

using UObject = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace PFV.Grass
{
    //************************************************
    //Author: pfernandez
    //************************************************
    /// <summary>
    ///  summay
    /// </summary>
    public class UndoGroupScope : IDisposable
    {
        public int undoGroup { get; private set; }
        public string message;
        public UndoGroupScope(string message)
        {
#if UNITY_EDITOR
            this.undoGroup = UndoUtils.BeginGroup();
            this.message = message;
#endif
        }
        public void Dispose()
        {
#if UNITY_EDITOR
            UndoUtils.EndGroup(message, undoGroup);
#endif
        }
    }
    public struct UndoScope : IDisposable
    {
        public UnityEngine.Object target { get; private set; }
        public UndoScope(UnityEngine.Object target, string message)
        {
#if UNITY_EDITOR
            this.target = target;
            UndoUtils.BeginRecord(string.IsNullOrEmpty(message) ? "Modification to object " + target.name : message, target);
#else
            this.target = null;
#endif
        }
        public void Dispose()
        {
#if UNITY_EDITOR
            UndoUtils.EndRecord(target);
#endif
        }
    }
    public struct UndoMultiScope : IDisposable
    {
        public UnityEngine.Object[] targets { get; private set; }
        public UndoMultiScope(UnityEngine.Object[] targets, string message = null)
        {
#if UNITY_EDITOR
            this.targets = targets;
            UndoUtils.BeginRecord(string.IsNullOrEmpty(message) ? $"Modification to objects ({targets.Length})" : message, targets);
#else
            this.targets = null;
#endif
        }
        public void Dispose()
        {
#if UNITY_EDITOR
            UndoUtils.EndRecord(targets);
#endif
        }
    }
    public static class UndoUtils
    {
        public static void ModifyAndRecord<T>(Action<T> modification, T obj)
            where T : UObject
        {
#if UNITY_EDITOR
            ModifyAndRecord("Modification of " + obj.name, modification, obj);
#else
            modification(obj);
#endif
        }

        public static void ModifyAndRecord<T>(Action<T[]> modification, params T[] objs)
            where T : UObject
        {
#if UNITY_EDITOR
            ModifyAndRecord("Modification of objects", modification, objs);
#else
            modification(objs);
#endif
        }

        public static void ModifyAndRecord<T>(string undoMessage, Action<T[]> modification, params T[] objs)
            where T : UObject
        {
#if UNITY_EDITOR
            BeginRecord(undoMessage, objs);
            modification(objs);
            EndRecord(objs);
#else
            modification(objs);
#endif
        }

        public static void ModifyAndRecord<T>(string undoMessage, Action<T> modification, params T[] objs)
            where T : UObject
        {
#if UNITY_EDITOR
            for (int i = 0; i < objs.Length; i++)
                ModifyAndRecord(undoMessage, modification, objs[i]);
#else
            for (int i = 0; i < objs.Length; i++)
                modification(objs[i]);
#endif
        }

        public static void ModifyAndRecord<T>(string undoMessage, Action<T> modification, T obj)
            where T : UObject
        {
#if UNITY_EDITOR
            BeginRecord(undoMessage, obj);
            modification(obj);
            EndRecord(obj);
#else
            modification(obj);
#endif
        }

        public static UndoScope RecordScope(UnityEngine.Object target, string message = null)
        {
            return new UndoScope(target, message);
        }

        public static UndoMultiScope RecordScope(params UnityEngine.Object[] targets)
        {
            return new UndoMultiScope(targets);
        }

        public static UndoMultiScope RecordScope(string message, params UnityEngine.Object[] targets)
        {
            return new UndoMultiScope(targets, message);
        }

        public static UndoGroupScope GroupScope(string message = null)
        {
            return new UndoGroupScope(message);
        }

        public static int BeginGroup()
        {
#if UNITY_EDITOR
            int group = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            return group;
#endif
            return 0;
        }

        public static void EndGroup(string message, int group)
        {
#if UNITY_EDITOR
            Undo.SetCurrentGroupName(message ?? "Modifications to group");
            Undo.CollapseUndoOperations(group);
#endif
        }

        public static void BeginRecord(UObject obj)
        {
#if UNITY_EDITOR
            BeginRecord("Modification to object " + obj.name, obj);
#endif
        }

        public static void BeginRecord(params UObject[] objs)
        {
#if UNITY_EDITOR
            BeginRecord("Modification to objects", objs);
#endif
        }

        public static void BeginRecord(string undoMessage, params UObject[] objs)
        {
#if UNITY_EDITOR
            for (int i = 0; i < objs.Length; i++)
                BeginRecord(undoMessage, objs[i]);
#endif
        }

        public static void BeginRecord(string undoMessage, UObject obj)
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(obj, undoMessage);
#endif
        }

        public static void EndRecord(params UObject[] objs)
        {
#if UNITY_EDITOR
            for (int i = 0; i < objs.Length; i++)
                EndRecord(objs[i]);
#endif
        }

        public static void EndRecord(UObject obj)
        {
#if UNITY_EDITOR
            if (PrefabUtility.GetPrefabInstanceStatus(obj).HasFlag(PrefabInstanceStatus.Connected))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                if (!PrefabUtility.IsPartOfNonAssetPrefabInstance(obj))
                {
                    if (obj is GameObject go)
                        PrefabUtility.SavePrefabAsset(go.transform.root.gameObject);
                    else if (obj is Component component)
                        PrefabUtility.SavePrefabAsset(component.transform.root.gameObject);
                }
            }
            EditorUtility.SetDirty(obj);

            if (!Application.isPlaying)
            {
                switch (obj)
                {
                    case GameObject go: EditorSceneManager.MarkSceneDirty(go.scene); break;
                    case Component comp: EditorSceneManager.MarkSceneDirty(comp.gameObject.scene); break;
                    case ScriptableObject so: break;
                    default: EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); break;
                }
            }
#endif
        }
    }
}