using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface
    {
        internal IEnumerable<IDrawableContainer> GroupMembers(IDrawableContainer element)
        {
            var id = (element as DrawableContainer)?.GroupId ?? Guid.Empty;
            return id == Guid.Empty ? new[] { element } : _elements.Where(e => (e as DrawableContainer)?.GroupId == id).ToArray();
        }

        private DrawableContainerList CompleteGroups(IEnumerable<IDrawableContainer> elements)
        {
            var requested = new HashSet<IDrawableContainer>(elements.SelectMany(GroupMembers));
            return new DrawableContainerList(_elements.Where(requested.Contains));
        }

        internal bool CanGroup => selectedElements.Count > 1 && selectedElements.All(e => e is DrawableContainer && !e.IsConfirmable && e.IsUndoable)
            && (selectedElements.OfType<DrawableContainer>().Any(e => e.GroupId == Guid.Empty)
                || selectedElements.OfType<DrawableContainer>().Select(e => e.GroupId).Distinct().Count() > 1);
        internal bool CanUngroup => selectedElements.OfType<DrawableContainer>().Any(e => e.GroupId != Guid.Empty);

        public bool GroupSelectedElements()
        {
            if (!CanGroup) return false;
            CancelPolygonInteraction();
            var members = CompleteGroups(selectedElements).OfType<DrawableContainer>().ToArray();
            MakeUndoable(new GroupMembershipMemento(this, members), false);
            var id = Guid.NewGuid();
            foreach (var member in members) { member.GroupId = id; member.GroupReferenceAngle = 0; }
            RefreshGroupSelection(members); Modified = true; return true;
        }

        public bool UngroupSelectedElements()
        {
            if (!CanUngroup) return false;
            var members = CompleteGroups(selectedElements).OfType<DrawableContainer>().ToArray();
            MakeUndoable(new GroupMembershipMemento(this, members), false);
            foreach (var member in members) { member.GroupId = Guid.Empty; member.GroupReferenceAngle = 0; }
            RefreshGroupSelection(members); Modified = true; return true;
        }

        internal void RefreshGroupSelection(IEnumerable<DrawableContainer> members)
        {
            DeselectAllElements(); SelectElements(new DrawableContainerList(members.Where(m => _elements.Contains(m)))); Invalidate();
        }

        public void SelectElement(IDrawableContainer container, bool invalidate = true, bool generateEvents = true)
        {
            if (!(container is DrawableContainer drawable) || drawable.GroupId == Guid.Empty)
            { SelectSingleElement(container, invalidate, generateEvents); return; }
            var members = GroupMembers(container).ToArray();
            if (members.All(selectedElements.Contains)) return;
            foreach (var member in members) SelectSingleElement(member, invalidate, false);
            if (generateEvents) _movingElementChanged?.Invoke(this, new SurfaceElementEventArgs { Elements = selectedElements });
            if (invalidate) Invalidate();
        }

        public void DeselectElement(IDrawableContainer container, bool generateEvents = true)
        {
            if (!(container is DrawableContainer drawable) || drawable.GroupId == Guid.Empty)
            { DeselectSingleElement(container, generateEvents); return; }
            foreach (var member in GroupMembers(container)) DeselectSingleElement(member, false);
            if (generateEvents) _movingElementChanged?.Invoke(this, new SurfaceElementEventArgs { Elements = selectedElements });
            Invalidate();
        }

        internal static void RenewGroupIds(IEnumerable<IDrawableContainer> elements)
        {
            var ids = new Dictionary<Guid, Guid>();
            foreach (var element in elements.OfType<DrawableContainer>())
            {
                if (element.GroupId == Guid.Empty) continue;
                if (!ids.TryGetValue(element.GroupId, out var id)) { id = Guid.NewGuid(); ids.Add(element.GroupId, id); }
                element.GroupId = id;
            }
        }

        private bool IsGroupedMember(IDrawableContainer element) => element is DrawableContainer member && member.GroupId != Guid.Empty && GroupMembers(element).Skip(1).Any();

        private void PaintGroupSelection(Graphics graphics)
        {
            foreach (var group in selectedElements.OfType<DrawableContainer>().Where(e => e.GroupId != Guid.Empty).GroupBy(e => e.GroupId))
            {
                if (group.Count() < 2) continue;
                var bounds = group.Select(e => (Rectangle)e.Bounds).Aggregate(Rectangle.Union);
                var saved = graphics.Save(); graphics.ScaleTransform(_zoomFactor, _zoomFactor);
                using var pen = new Pen(Color.DeepSkyBlue, (float)(1 / (double)_zoomFactor)) { DashStyle = DashStyle.Dash };
                graphics.DrawRectangle(pen, bounds); graphics.Restore(saved);
            }
        }
    }
}
