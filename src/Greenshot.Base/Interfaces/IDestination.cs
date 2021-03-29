/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;

namespace Greenshot.Base.Interfaces
{
    public class ExportInformation
    {
        public ExportInformation(string destinationDesignation, string destinationDescription)
        {
            DestinationDesignation = destinationDesignation;
            DestinationDescription = destinationDescription;
        }

        public ExportInformation(string destinationDesignation, string destinationDescription, bool exportMade) : this(destinationDesignation, destinationDescription)
        {
            ExportMade = exportMade;
        }

        public string DestinationDesignation { get; }

        public string DestinationDescription { get; set; }

        /// <summary>
        /// Set to true to specify if the export worked.
        /// </summary>
        public bool ExportMade { get; set; }

        public string Uri { get; set; }

        public string ErrorMessage { get; set; }

        public string Filepath { get; set; }
    }

    /// <summary>
    /// Description of IDestination.
    /// </summary>
    public interface IDestination : IDisposable, IComparable
    {
        /// <summary>
        /// Simple "designation" like "File", "Editor" etc, used to store the configuration
        /// </summary>
        string Designation { get; }

        /// <summary>
        /// Description which will be shown in the settings form, destination picker etc
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Priority, used for sorting
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets an icon for the destination
        /// </summary>
        Image DisplayIcon { get; }

        /// <summary>
        /// Returns if the destination is active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Return a menu item
        /// </summary>
        /// <param name="addDynamics">Resolve the dynamic destinations too?</param>
        /// <param name="menu">The menu for which the item is created</param>
        /// <param name="destinationClickHandler">Handler which is called when clicked</param>
        /// <returns>ToolStripMenuItem</returns>
        ToolStripMenuItem GetMenuItem(bool addDynamics, ContextMenuStrip menu, EventHandler destinationClickHandler);

        /// <summary>
        /// Gets the ShortcutKeys for the Editor
        /// </summary>
        Keys EditorShortcutKeys { get; }

        /// <summary>
        /// Gets the dynamic destinations
        /// </summary>
        IEnumerable<IDestination> DynamicDestinations();

        /// <summary>
        /// Returns true if this destination can be dynamic
        /// </summary>
        bool IsDynamic { get; }

        /// <summary>
        /// Returns if the destination is active
        /// </summary>
        bool UseDynamicsOnly { get; }

        /// <summary>
        /// Returns true if this destination returns a link
        /// </summary>
        bool IsLinkable { get; }

        /// <summary>
        /// If a capture is made, and the destination is enabled, this method is called.
        /// </summary>
        /// <param name="manuallyInitiated">true if the user selected this destination from a GUI, false if it was called as part of a process</param>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        /// <returns>DestinationExportInformation with information, like if the destination has "exported" the capture</returns>
        ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails);
    }
}