using System;
using UnityEditor.IMGUI.Controls;

namespace SAGA.Editor
{
    public class TreeView<TItem, TData>: TreeView, IDisposable
    {
        public TreeView(TreeViewState state) : base(state)
        {
        }

        public TreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
        }
        public void Dispose()
        {
            
        }

        protected override TreeViewItem BuildRoot()
        {
            throw new NotImplementedException();
        }
    }
}