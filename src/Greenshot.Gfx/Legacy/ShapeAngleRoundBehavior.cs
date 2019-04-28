// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace Greenshot.Gfx.Legacy
{
    public static partial class ScaleHelper
	{
        public class ShapeAngleRoundBehavior : IDoubleProcessor
		{
			public static ShapeAngleRoundBehavior Instance = new ShapeAngleRoundBehavior();

			private ShapeAngleRoundBehavior()
			{
			}

			public double Process(double angle)
			{
				return Math.Round((angle + 45) / 90) * 90 - 45;
			}
		}


		/*public static int FindGripperPostition(float anchorX, float anchorY, float gripperX, float gripperY) {
			if(gripperY > anchorY) {
				if(gripperX > anchorY) return Gripper.POSITION_BOTTOM_RIGHT;
				else return Gripper.POSITION_BOTTOM_LEFT;
			} else {
				if(gripperX > anchorY) return Gripper.POSITION_TOP_RIGHT;
				else return Gripper.POSITION_TOP_LEFT;
			}
		}*/
	}
}