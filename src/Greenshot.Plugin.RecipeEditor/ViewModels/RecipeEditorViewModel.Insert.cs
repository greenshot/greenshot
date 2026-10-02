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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    /// <summary>
    /// Putting a step between two others: a step without connections dropped onto an arrow goes into it
    /// </summary>
    public partial class RecipeEditorViewModel
    {
        /// <summary>
        /// How close (pixels) a dropped step has to be to an arrow to go into it
        /// </summary>
        private const double DropOnArrowDistance = 40;

        private ICommand _nodesDraggedCommand;

        /// <summary>
        /// The canvas finished dragging steps
        /// </summary>
        public ICommand NodesDraggedCommand => _nodesDraggedCommand ??= new RelayCommand(OnNodesDragged);

        /// <summary>
        /// The ports through which a step leads on: the branches of a decision or prompt, the output of other steps
        /// </summary>
        private static IReadOnlyList<StepPortViewModel> GetOutgoingPorts(StepNodeViewModel node)
        {
            if (node.IsConditional && node.ConditionBranches.Count > 0)
            {
                return node.ConditionBranches.Select(b => b.Port).ToList();
            }
            if (node.IsUserPrompt && node.PromptChoices.Count > 0)
            {
                return node.PromptChoices.Select(p => p.Port).ToList();
            }
            return new[] { node.OutputPort };
        }

        /// <summary>
        /// A step without connections dropped onto an arrow: A -> B becomes A -> step -> B
        /// </summary>
        private void OnNodesDragged()
        {
            var node = SelectedNode;
            if (node == null || node.IsBoundary || !Nodes.Contains(node)) return;
            bool connected = Connections.Any(c => c.SourceNode == node || c.TargetNode == node);
            if (connected) return;

            var center = GetCenter(node);
            StepConnectionViewModel nearest = null;
            double nearestDistance = DropOnArrowDistance;
            foreach (var connection in Connections)
            {
                double distance = DistanceToConnection(connection, center);
                if (distance < nearestDistance)
                {
                    nearest = connection;
                    nearestDistance = distance;
                }
            }
            if (nearest != null)
            {
                PutNodeIntoConnection(nearest, node);
                StatusMessage = $"Inserted '{node.DisplayName}' between '{nearest.SourceNode.DisplayName}' and '{nearest.TargetNode.DisplayName}'";
            }
        }

        /// <summary>
        /// Puts the step between the two steps of the connection; its branches (decisions) all lead to the next step
        /// </summary>
        private void PutNodeIntoConnection(StepConnectionViewModel connection, StepNodeViewModel node)
        {
            var source = connection.Source;
            var target = connection.Target;
            RemoveConnection(connection);
            Connect(source, node.InputPort);
            foreach (var port in GetOutgoingPorts(node))
            {
                Connect(port, target);
            }
            SelectedNode = node;
        }

        private static Point GetCenter(StepNodeViewModel node)
        {
            var top = node.InputPort.Anchor;
            var bottom = node.OutputPort.Anchor;
            if (top == default || bottom == default)
            {
                // Not drawn yet: the card is 220 wide
                return new Point(node.Location.X + 110, node.Location.Y + 50);
            }
            return new Point((top.X + bottom.X) / 2, (top.Y + bottom.Y) / 2);
        }

        /// <summary>
        /// The distance of a point to an arrow, which is drawn as a vertical curve from its source to its target
        /// </summary>
        private static double DistanceToConnection(StepConnectionViewModel connection, Point point)
        {
            var p0 = connection.Source.Anchor;
            var p3 = connection.Target.Anchor;
            double offset = Math.Max(Math.Abs(p3.Y - p0.Y) / 2, 30);
            var p1 = new Point(p0.X, p0.Y + offset);
            var p2 = new Point(p3.X, p3.Y - offset);
            double best = double.MaxValue;
            for (int i = 0; i <= 24; i++)
            {
                double t = i / 24.0;
                double u = 1 - t;
                double x = u * u * u * p0.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X;
                double y = u * u * u * p0.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y;
                best = Math.Min(best, (new Point(x, y) - point).Length);
            }
            return best;
        }
    }
}
