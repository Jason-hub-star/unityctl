#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// IDisposable Undo group management.
    /// Creates an undo group on construction, closes it on disposal.
    /// </summary>
    public sealed class UndoScope : IDisposable
    {
        private readonly int _group;

        public string GroupName { get; }

        public UndoScope(string groupName)
        {
            GroupName = groupName;
            if (UndoTransactionScope.HasActiveTransaction)
            {
                _group = -1;
                return;
            }

            Undo.IncrementCurrentGroup();
            _group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(groupName);
        }

        public void Dispose()
        {
            if (_group < 0)
            {
                UndoTransactionScope.RestoreCurrentGroupName();
                return;
            }

            Undo.CollapseUndoOperations(_group);
        }
    }
}
#endif
