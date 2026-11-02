using System;
using System.Linq;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Memento
{
    internal sealed class GroupMembershipMemento : IMemento
    {
        private readonly Surface _surface;
        private readonly DrawableContainer[] _members;
        private readonly Guid[] _ids;
        internal GroupMembershipMemento(Surface surface, DrawableContainer[] members)
        {
            _surface = surface; _members = members; _ids = members.Select(m => m.GroupId).ToArray();
        }
        public IMemento Restore()
        {
            var inverse = new GroupMembershipMemento(_surface, _members);
            for (int i = 0; i < _members.Length; i++) _members[i].GroupId = _ids[i];
            _surface.RefreshGroupSelection(_members); _surface.Modified = true; return inverse;
        }
        public bool Merge(IMemento other) => false;
        public void Dispose() { }
    }
}
