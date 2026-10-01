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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Trigger which offers a recipe as a tool to AI tools (MCP clients using greenshot-mcp.exe).
    /// Only the verified greenshot-mcp.exe connection can fire it, after the user allowed the AI tool.
    /// </summary>
    public class AiToolTrigger : TriggerBase
    {
        private static readonly Regex ToolNameRegex = new Regex("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

        /// <summary>
        /// The tool name the AI uses
        /// </summary>
        public string ToolName { get; }

        public string Description { get; }

        public IReadOnlyList<CommandlineArgument> Arguments { get; }

        public AiToolTrigger(string id, string name, string targetRecipeId, string toolName, string description = null, IEnumerable<CommandlineArgument> arguments = null)
            : base(id, name, targetRecipeId)
        {
            ToolName = toolName;
            Description = description;
            Arguments = new List<CommandlineArgument>(arguments ?? new List<CommandlineArgument>()).AsReadOnly();
        }

        public override string TriggerType => TriggerConfig.TypeAiTool;

        public override void Start() { }
        public override void Stop() { }

        /// <summary>
        /// MCP tool names: 1 to 64 letters, digits, _ or -.
        /// </summary>
        public static bool IsValidToolName(string toolName) => !string.IsNullOrEmpty(toolName) && ToolNameRegex.IsMatch(toolName);
    }
}
