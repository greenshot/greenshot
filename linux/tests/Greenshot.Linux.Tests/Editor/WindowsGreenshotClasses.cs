/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

#nullable disable
#pragma warning disable CS0169, CS0414, CS0649, IDE0044, IDE0051, IDE0052

// The classes of the Windows editor which are in a .greenshot file, with their names and fields, so the tests can write
// files the way the Windows editor does (BinaryFormatter). Only for the tests of WindowsElementReader.

using System.Drawing;
using System.Runtime.Serialization;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Base.Interfaces.Drawing
{
    public interface IField;

    public interface IFieldType;

    public interface IFieldHolder;

    public interface IDrawableContainer;

    public enum EditStatus
    {
        UNDRAWN,
        DRAWING,
        MOVING,
        RESIZING,
        IDLE,
    }
}

namespace Greenshot.Editor.Drawing.Fields
{
    [Serializable]
    public class FieldType(string name) : IFieldType
    {
        public string Name { get; set; } = name;
    }

    [Serializable]
    public class Field(string type, object value, string scope) : IField
    {
        private object _myValue = value;

        public IFieldType FieldType { get; set; } = new FieldType(type);

        public string Scope { get; set; } = scope;
    }

    [Serializable]
    public abstract class AbstractFieldHolder : IFieldHolder
    {
        private readonly IList<IField> fields = new List<IField>();

        internal void AddField(string type, object value) => fields.Add(new Field(type, value, GetType().Name));
    }

    public static class FieldHolderExtensions
    {
        public static T Set<T>(this T holder, string type, object value) where T : AbstractFieldHolder
        {
            holder.AddField(type, value);
            return holder;
        }
    }

    [Serializable]
    public abstract class AbstractFieldHolderWithChildren : AbstractFieldHolder
    {
        public IList<IFieldHolder> Children = new List<IFieldHolder>();
    }
}

namespace Greenshot.Editor.Drawing
{
    [Serializable]
    public class DrawableContainerList : List<IDrawableContainer>
    {
        public Guid ParentID { get; private set; } = Guid.NewGuid();
    }

    [Serializable]
    public abstract class DrawableContainer : AbstractFieldHolderWithChildren, IDrawableContainer
    {
        protected EditStatus _defaultEditMode = EditStatus.DRAWING;
        private int left;
        private int top;
        private int width;
        private int height;
        private bool accountForShadowChange;

        public T At<T>(int x, int y, int w, int h) where T : DrawableContainer
        {
            left = x;
            top = y;
            width = w;
            height = h;
            return (T)this;
        }

        public DrawableContainer At(int x, int y, int w, int h)
        {
            left = x;
            top = y;
            width = w;
            height = h;
            return this;
        }
    }

    [Serializable]
    public class RectangleContainer : DrawableContainer;

    [Serializable]
    public class EllipseContainer : DrawableContainer;

    [Serializable]
    public class TextContainer : RectangleContainer
    {
        private bool makeUndoable;
        private string text;

        public TextContainer WithText(string value)
        {
            text = value;
            return this;
        }
    }

    [Serializable]
    public class SpeechbubbleContainer : TextContainer
    {
        private Point _initialGripperPoint;
        private Point _storedTargetGripperLocation;

        public SpeechbubbleContainer Pointing(Point target)
        {
            _storedTargetGripperLocation = target;
            return this;
        }
    }

    [Serializable]
    public class LineContainer : DrawableContainer;

    [Serializable]
    public class ArrowContainer : LineContainer
    {
        public enum ArrowHeadCombination
        {
            NONE,
            START_POINT,
            END_POINT,
            BOTH,
        }
    }

    [Serializable]
    public class FreehandContainer : DrawableContainer
    {
        private Rectangle myBounds;
        private Point lastMouse;
        private List<Point> capturePoints = new();
        private bool isRecalculated;

        public FreehandContainer Through(params Point[] points)
        {
            capturePoints.AddRange(points);
            return this;
        }
    }

    [Serializable]
    public class StepLabelContainer : DrawableContainer
    {
        private readonly bool _drawAsRectangle = false;
        private int _number;
        private int _counterStart = 1;

        public StepLabelContainer Starting(int start)
        {
            _counterStart = start;
            return this;
        }
    }

    [Serializable]
    public class FilterContainer : DrawableContainer
    {
        public enum PreparedFilter
        {
            BLUR,
            PIXELIZE,
            TEXT_HIGHTLIGHT,
            AREA_HIGHLIGHT,
            GRAYSCALE,
            MAGNIFICATION,
        }
    }

    [Serializable]
    public class ObfuscateContainer : FilterContainer;

    [Serializable]
    public class HighlightContainer : FilterContainer;

    public enum CutMarkStyle
    {
        None,
        Line,
        ZigZag,
        Wave,
        Torn,
    }

    [Serializable]
    public sealed class CutMarkContainer(bool horizontal) : DrawableContainer
    {
        private readonly bool _horizontal = horizontal;
        private int _seed = 4711;
        private float _shadowDarkness = 0.5f;
        private int _shadowSize = 6;
        private int _shadowOffsetX = -1;
        private int _shadowOffsetY = -1;
    }

    [Serializable]
    public sealed class TornEdgeContainer : DrawableContainer
    {
        private int _seed = 42;
        private bool[] _edges = [true, false, true, true];
        private int[] _margins = [8, 0, 8, 8];
        private float _shadowDarkness = 0.6f;
        private int _shadowSize = 7;
        private int _shadowOffsetX = -1;
        private int _shadowOffsetY = -1;
    }

    [Serializable]
    public class VectorGraphicsContainer : DrawableContainer
    {
        private int _rotationAngle;
    }

    [Serializable]
    public class ImageContainer(FakeBitmap image) : DrawableContainer
    {
        private FakeBitmap _image = image;
    }

    /// <summary>
    /// System.Drawing.Bitmap writes its PNG as "Data"
    /// </summary>
    [Serializable]
    public sealed class FakeBitmap(byte[] png) : ISerializable
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("Data", png);
    }

    /// <summary>
    /// System.Drawing.StringAlignment, written under that name
    /// </summary>
    public enum FakeStringAlignment
    {
        Near,
        Center,
        Far,
    }

    /// <summary>
    /// A container of a later Windows version
    /// </summary>
    [Serializable]
    public class FutureContainer : DrawableContainer;
}

namespace Greenshot.Editor.Drawing.Emoji
{
    [Serializable]
    public sealed class EmojiContainer(string emoji) : VectorGraphicsContainer
    {
        private string _emoji = emoji;
    }
}

namespace Greenshot.Editor.Drawing.Filters
{
    [Serializable]
    public abstract class AbstractFilter : AbstractFieldHolder
    {
        private bool invert;
        protected Greenshot.Editor.Drawing.DrawableContainer parent;
    }

    [Serializable]
    public class PixelizationFilter : AbstractFilter;

    [Serializable]
    public class BlurFilter : AbstractFilter
    {
        public double previewQuality;
    }

    [Serializable]
    public class HighlightFilter : AbstractFilter;
}
