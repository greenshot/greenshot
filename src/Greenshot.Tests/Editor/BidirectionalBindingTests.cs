/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Fields.Binding;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The editor binds its tool bar to the fields of the capture which is shown: switching tabs binds the controls to the fields of another capture
    /// </summary>
    public class BidirectionalBindingTests
    {
        private sealed class FakeControl : INotifyPropertyChanged
        {
            private decimal _value;

            public event PropertyChangedEventHandler PropertyChanged;

            public decimal Value
            {
                get => _value;
                set
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        [Fact]
        public void UpdateControl_ShowsTheValueOfTheField()
        {
            var control = new FakeControl { Value = 1 };
            var field = new Field(FieldType.LINE_THICKNESS, "Test") { Value = 5 };

            using var binding = new BidirectionalBinding(control, nameof(FakeControl.Value), field, nameof(Field.Value), DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
            binding.UpdateControl();

            Assert.Equal(5m, control.Value);
            // Showing the value doesn't write it back
            Assert.Equal(5, field.Value);
        }

        [Fact]
        public void Dispose_StopsTheSynchronization()
        {
            var control = new FakeControl();
            var field = new Field(FieldType.LINE_THICKNESS, "Test") { Value = 2 };
            var binding = new BidirectionalBinding(control, nameof(FakeControl.Value), field, nameof(Field.Value), DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());

            control.Value = 3;
            Assert.Equal(3, field.Value);

            binding.Dispose();
            control.Value = 4;
            field.Value = 6;

            Assert.Equal(4m, control.Value);
            Assert.Equal(6, field.Value);
        }
    }
}
