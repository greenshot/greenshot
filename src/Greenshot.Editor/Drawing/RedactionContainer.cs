using System;
using System.Drawing;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Represents a redaction object on the Surface
    /// </summary>
    [Serializable]
    public class RedactionContainer : RectangleContainer
    {
        private readonly Color lineColor = Color.Black;
        private readonly Color fillColor = Color.Black;
        private readonly int lineThickness = 0;
        private readonly bool shadow = false;

        public RedactionContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        /// <summary>
        /// Do some logic to make sure all fields are initiated correctly
        /// </summary>
        /// <param name="streamingContext">StreamingContext</param>
        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();
        }

        protected override void InitializeFields()
        {
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            var rect = new NativeRect(Left, Top, Width, Height).Normalize();

            DrawRectangle(rect, graphics, rm, lineThickness, lineColor, fillColor, shadow);
        }

        public override bool ClickableAt(int x, int y)
        {
            var rect = new NativeRect(Left, Top, Width, Height).Normalize();

            return RectangleClickableAt(rect, lineThickness, fillColor, x, y);
        }
    }
}
