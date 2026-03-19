#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal sealed class UndoTransactionScope : IDisposable
    {
        [ThreadStatic]
        private static Stack<UndoTransactionScope> _activeScopes;

        private readonly int _group;
        private bool _completed;
        private bool _rolledBack;

        public string GroupName { get; }

        public static bool HasActiveTransaction => _activeScopes != null && _activeScopes.Count > 0;

        public static string CurrentGroupName => HasActiveTransaction ? _activeScopes.Peek().GroupName : null;

        public UndoTransactionScope(string groupName)
        {
            GroupName = groupName;
            Undo.IncrementCurrentGroup();
            _group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(groupName);

            _activeScopes ??= new Stack<UndoTransactionScope>();
            _activeScopes.Push(this);
        }

        public void Complete()
        {
            if (_completed || _rolledBack)
                return;

            Undo.SetCurrentGroupName(GroupName);
            Undo.CollapseUndoOperations(_group);
            _completed = true;
        }

        public void Rollback()
        {
            if (_rolledBack)
                return;

            Undo.RevertAllDownToGroup(_group);
            _rolledBack = true;
        }

        public static void RestoreCurrentGroupName()
        {
            if (!HasActiveTransaction)
                return;

            Undo.SetCurrentGroupName(_activeScopes.Peek().GroupName);
        }

        public void Dispose()
        {
            try
            {
                if (!_completed && !_rolledBack)
                {
                    Rollback();
                }
            }
            finally
            {
                if (_activeScopes != null && _activeScopes.Count > 0 && ReferenceEquals(_activeScopes.Peek(), this))
                {
                    _activeScopes.Pop();
                }
            }
        }
    }
}
#endif
