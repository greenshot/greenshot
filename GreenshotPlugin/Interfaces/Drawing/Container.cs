/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;

namespace Greenshot.Plugin.Drawing
{
    [Serializable]
    public enum RenderMode
    {
        EDIT,
        EXPORT
    };

    [Serializable]
    public enum EditStatus
    {
        UNDRAWN,
        DRAWING,
        MOVING,
        RESIZING,
        IDLE
    };

    [Serializable]
    public enum ArrowHeadCombination
    {
        NONE,
        START_POINT,
        END_POINT,
        BOTH
    };

    /// <summary>
    /// This is used to mark the fields that are important for the editor in the container
    /// </summary>
    [Serializable]
    public enum FieldTypes
    {
        ARROWHEADS,
        BLUR_RADIUS,
        BRIGHTNESS,
        FILL_COLOR,
        FONT_BOLD,
        FONT_FAMILY,
        FONT_ITALIC,
        FONT_SIZE,
        TEXT_HORIZONTAL_ALIGNMENT,
        TEXT_VERTICAL_ALIGNMENT,
        HIGHLIGHT_COLOR,
        LINE_COLOR,
        LINE_THICKNESS,
        MAGNIFICATION_FACTOR,
        PIXEL_SIZE,
        SHADOW,
        PREPARED_FILTER_OBFUSCATE,
        PREPARED_FILTER_HIGHLIGHT,
        FLAGS
    }

    [Flags, Serializable]
    public enum FieldFlag
    {
        NONE = 0,
        CONFIRMABLE = 1,
        COUNTER = 2
    }

    /// <summary>
    /// Attribute for telling that a property is a field-property, meaning it is linked to a changable value for the editor
    /// </summary>
    [Serializable, AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FieldAttribute : Attribute
    {
        private FieldTypes fieldType;
        private string scope;

        [NonSerialized]
        private PropertyInfo linkedProperty;

        public FieldAttribute(FieldTypes fieldType)
        {
            this.fieldType = fieldType;
        }

        /// <summary>
        /// FieldType of this FieldAttribute
        /// </summary>
        public FieldTypes FieldType
        {
            get
            {
                return fieldType;
            }
        }

        /// <summary>
        /// Scope of the field, default is set to the Type of the class
        /// </summary>
        public string Scope
        {
            get
            {
                return scope;
            }
            set
            {
                scope = value;
            }
        }

        /// <summary>
        /// Helper method to get the property info for this attribute
        /// </summary>
        /// <param name="typeForAttribute"></param>
        private void Reflect(Type typeForAttribute)
        {
            if (linkedProperty == null)
            {
                foreach (PropertyInfo propertyInfo in typeForAttribute.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (Attribute attribute in propertyInfo.GetCustomAttributes(true))
                    {
                        FieldAttribute fieldAttribute = attribute as FieldAttribute;
                        if (fieldAttribute != null && fieldAttribute.FieldType == fieldType)
                        {
                            linkedProperty = propertyInfo;
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is used for caching the Reflection and should only be set from the InitializeFields
        /// </summary>
        public PropertyInfo LinkedProperty
        {
            private get
            {
                return linkedProperty;
            }
            set
            {
                linkedProperty = value;
            }
        }

        /// <summary>
        /// Change the field value on the IFieldHolder
        /// </summary>
        /// <param name="target">IFieldHolder</param>
        /// <param name="value">value to set</param>
        public void SetValue(IFieldHolder target, object value)
        {
            if (linkedProperty == null)
            {
                Reflect(target.GetType());
            }
            if (LinkedProperty.CanWrite)
            {
                LinkedProperty.SetValue(target, value, null);
            }
        }

        /// <summary>
        /// Get the field value from the IFieldHolder, this is needed for serialization
        /// </summary>
        /// <param name="target">IFieldHolder</param>
        /// <returns>value</returns>
        public object GetValue(IFieldHolder target)
        {
            if (linkedProperty == null)
            {
                Reflect(target.GetType());
            }
            if (LinkedProperty.CanRead)
            {
                return LinkedProperty.GetValue(target, null);
            }
            return null;
        }
    }

    /// <summary>
    /// All elements that have "fields" that need to be bound in the editor must implement this interface
    /// </summary>
    public interface IFieldHolder : INotifyPropertyChanged
    {
        IDictionary<FieldTypes, FieldAttribute> FieldAttributes
        {
            get;
        }

        void Invalidate();
    }

    /// <summary>
    /// The IFilter is an interface for all our filters like blur etc.
    /// </summary>
    public interface IFilter : IFieldHolder
    {
        IDrawableContainer Parent
        {
            get;
            set;
        }
        void Apply(Graphics graphics, Bitmap bmp, Rectangle rect, RenderMode renderMode);
        bool Invert
        {
            get;
            set;
        }
    }

    public interface IDrawableContainer : IFieldHolder, IDisposable
    {
        ISurface Parent
        {
            get;
        }
        bool Selected
        {
            get;
            set;
        }

        int Left
        {
            get;
            set;
        }

        int Top
        {
            get;
            set;
        }

        int Width
        {
            get;
            set;
        }

        int Height
        {
            get;
            set;
        }

        Point Location
        {
            get;
        }

        Size Size
        {
            get;
        }

        Rectangle Bounds
        {
            get;
        }

        Rectangle DrawingBounds
        {
            get;
        }

        bool hasFilters
        {
            get;
        }
        List<IFilter> Filters
        {
            get;
        }
        EditStatus Status
        {
            get;
            set;
        }
        void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment);
        bool ClickableAt(int x, int y);
        void HideGrippers();
        void ShowGrippers();
        void MoveBy(int x, int y);
        void Transform(Matrix matrix);
        bool HandleMouseDown(int x, int y);
        void HandleMouseUp(int x, int y);
        bool HandleMouseMove(int x, int y);
        bool InitContent();
        void MakeBoundsChangeUndoable(bool allowMerge);
        EditStatus DefaultEditMode
        {
            get;
        }
    }

    public interface ITextContainer : IDrawableContainer
    {
        string Text
        {
            get;
            set;
        }
        void FitToText();
        void ChangeText(string newText, bool allowUndoable);
    }

    public interface IImageContainer : IDrawableContainer
    {
        Image Image
        {
            get;
            set;
        }
        void Load(string filename);
    }
    public interface ICursorContainer : IDrawableContainer
    {
        Cursor Cursor
        {
            get;
            set;
        }
        void Load(string filename);
    }
    public interface IIconContainer : IDrawableContainer
    {
        Icon Icon
        {
            get;
            set;
        }
        void Load(string filename);
    }

    public interface IMetafileContainer : IDrawableContainer
    {
        Metafile Metafile
        {
            get;
            set;
        }
        void Load(string filename);
    }
}
