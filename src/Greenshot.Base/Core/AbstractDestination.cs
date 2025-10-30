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
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.User32;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The AbstractDestination is a default implementation of IDestination
    /// </summary>
    public abstract class AbstractDestination : IDestination
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AbstractDestination));
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

        public virtual int CompareTo(object obj)
        {
            if (obj is not IDestination other)
            {
                return 1;
            }

            if (Priority == other.Priority)
            {
                return string.Compare(Description, other.Description, StringComparison.Ordinal);
            }

            return Priority - other.Priority;
        }

        public abstract string Designation { get; }

        public abstract string Description { get; }

        public virtual int Priority => 10;

        public virtual Image DisplayIcon => null;

        public virtual Keys EditorShortcutKeys => Keys.None;

        public virtual IEnumerable<IDestination> DynamicDestinations()
        {
            yield break;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            //if (disposing) {}
        }

        public virtual bool IsDynamic => false;

        public virtual bool UseDynamicsOnly => false;

        public virtual bool IsLinkable => false;

        public virtual bool IsActive
        {
            get
            {
                if (CoreConfig.ExcludeDestinations != null && CoreConfig.ExcludeDestinations.Contains(Designation))
                {
                    return false;
                }

                return true;
            }
        }

        public abstract ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails);

        /// <summary>
        /// A small helper method to perform some default destination actions, like inform the surface of the export
        /// </summary>
        /// <param name="exportInformation"></param>
        /// <param name="surface"></param>
        public void ProcessExport(ExportInformation exportInformation, ISurface surface)
        {
            if (exportInformation != null && exportInformation.ExportMade)
            {
                if (!string.IsNullOrEmpty(exportInformation.Uri))
                {
                    surface.UploadUrl = exportInformation.Uri;
                    surface.SendMessageEvent(this, SurfaceMessageTyp.UploadedUri, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
                }
                else if (!string.IsNullOrEmpty(exportInformation.Filepath))
                {
                    surface.LastSaveFullPath = exportInformation.Filepath;
                    surface.SendMessageEvent(this, SurfaceMessageTyp.FileSaved, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
                }
                else
                {
                    surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
                }

                surface.Modified = false;
            }
            else if (!string.IsNullOrEmpty(exportInformation?.ErrorMessage))
            {
                surface.SendMessageEvent(this, SurfaceMessageTyp.Error,
                    Language.GetFormattedString("exported_to_error", exportInformation.DestinationDescription) + " " + exportInformation.ErrorMessage);
            }
        }

        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// Helper method to add events which set the tag, this way we can see why there might be a close.
        /// </summary>
        /// <param name="menuItem">Item to add the events to</param>
        /// <param name="menu">Menu to set the tag</param>
        /// <param name="tagValue">Value for the tag</param>
        private void AddTagEvents(ToolStripMenuItem menuItem, ContextMenuStrip menu, string tagValue)
        {
            if (menuItem != null && menu != null)
            {
                menuItem.MouseDown += delegate
                {
                    Log.DebugFormat("Setting tag to '{0}'", tagValue);
                    menu.Tag = tagValue;
                };
                menuItem.MouseUp += delegate
                {
                    Log.Debug("Deleting tag");
                    menu.Tag = null;
                };
            }
        }

        /// <summary>
        /// This method will create and show the destination picker menu
        /// </summary>
        /// <param name="addDynamics">Boolean if the dynamic values also need to be added</param>
        /// <param name="surface">The surface which can be exported</param>
        /// <param name="captureDetails">Details for the surface</param>
        /// <param name="destinations">The list of destinations to show</param>
        /// <returns></returns>
        public ExportInformation ShowPickerMenu(bool addDynamics, ISurface surface, ICaptureDetails captureDetails, IEnumerable<IDestination> destinations)
        {
            // Generate an empty ExportInformation object, for when nothing was selected.
            ExportInformation exportInformation = new ExportInformation(Designation, Language.GetString("settings_destination_picker"));
            var menu = new ContextMenuStrip
            {
                ImageScalingSize = CoreConfig.IconSize,
                Tag = null,
                TopLevel = true
            };

            menu.SetupAutoDispose();

            menu.Opening += (sender, args) =>
            {
                // find the DPI settings for the screen where this is going to land
                var screenDpi = NativeDpiMethods.GetDpi(menu.Location);
                var scaledIconSize = DpiCalculator.ScaleWithDpi(CoreConfig.IconSize, screenDpi);
                menu.SuspendLayout();
                var fontSize = DpiCalculator.ScaleWithDpi(12f, screenDpi);
                menu.Font = new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                menu.ImageScalingSize = scaledIconSize;
                menu.ResumeLayout();
            };

            menu.Closing += delegate(object source, ToolStripDropDownClosingEventArgs eventArgs)
            {
                Log.DebugFormat("Close reason: {0}", eventArgs.CloseReason);
                switch (eventArgs.CloseReason)
                {
                    case ToolStripDropDownCloseReason.AppFocusChange:
                        if (menu.Tag == null)
                        {
                            // Do not allow the close if the tag is not set, this means the user clicked somewhere else.
                            eventArgs.Cancel = true;
                        }
                        else
                        {
                            Log.DebugFormat("Letting the menu 'close' as the tag is set to '{0}'", menu.Tag);
                        }

                        break;
                    case ToolStripDropDownCloseReason.ItemClicked:
                    case ToolStripDropDownCloseReason.CloseCalled:
                        // The ContextMenuStrip can be "closed" for these reasons.
                        break;
                    case ToolStripDropDownCloseReason.Keyboard:
                        // Dispose as the close is clicked
                        if (!captureDetails.HasDestination("Editor"))
                        {
                            surface.Dispose();
                            surface = null;
                        }

                        break;
                    default:
                        eventArgs.Cancel = true;
                        break;
                }
            };
            menu.MouseEnter += delegate
            {
                // in case the menu has been unfocused, focus again so that dropdown menus will still open on mouseenter
                if (!menu.ContainsFocus)
                {
                    menu.Focus();
                }
            };
            foreach (IDestination destination in destinations)
            {
                // Fix foreach loop variable for the delegate
                ToolStripMenuItem item = destination.GetMenuItem(addDynamics, menu,
                    delegate(object sender, EventArgs e)
                    {
                        ToolStripMenuItem toolStripMenuItem = sender as ToolStripMenuItem;
                        IDestination clickedDestination = (IDestination) toolStripMenuItem?.Tag;
                        if (clickedDestination == null)
                        {
                            return;
                        }

                        menu.Tag = clickedDestination.Designation;
                        // Export
                        exportInformation = clickedDestination.ExportCapture(true, surface, captureDetails);
                        if (exportInformation != null && exportInformation.ExportMade)
                        {
                            Log.InfoFormat("Export to {0} success, closing menu", exportInformation.DestinationDescription);
                            // close menu if the destination wasn't the editor
                            menu.Close();

                            // Cleanup surface, only if there is no editor in the destinations and we didn't export to the editor
                            if (!captureDetails.HasDestination("Editor") && !"Editor".Equals(clickedDestination.Designation))
                            {
                                surface.Dispose();
                                surface = null;
                            }
                        }
                        else
                        {
                            Log.Info("Export cancelled or failed, showing menu again");

                            // Make sure a click besides the menu don't close it.
                            menu.Tag = null;

                            // This prevents the problem that the context menu shows in the task-bar
                            ShowMenuAtCursor(menu);
                        }
                    }
                );
                if (item != null)
                {
                    menu.Items.Add(item);
                }
            }

            // Close
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem closeItem = new ToolStripMenuItem(Language.GetString("editor_close"))
            {
                Image = GreenshotResources.GetImage("Close.Image")
            };
            closeItem.Click += delegate
            {
                // This menu entry is the close itself, we can dispose the surface
                menu.Close();
                if (!captureDetails.HasDestination("Editor"))
                {
                    surface.Dispose();
                    surface = null;
                }
            };
            menu.Items.Add(closeItem);

            ShowMenuAtCursor(menu);
            return exportInformation;
        }
        
        /// <summary>
        /// This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not visible in the taskbar.
        /// </summary>
        /// <param name="menu"></param>
        private static void ShowMenuAtCursor(ContextMenuStrip menu)
        {
            // find a suitable location
            NativePoint location = Cursor.Position;
            var menuRectangle = new NativeRect(location, menu.Size);

            menuRectangle = menuRectangle.Intersect(DisplayInfo.ScreenBounds);
            if (menuRectangle.Height < menu.Height)
            {
                location = location.Offset(-40, -(menuRectangle.Height - menu.Height));
            }
            else
            {
                location = location.Offset(-40, -10);
            }

            // This prevents the problem that the context menu shows in the task-bar
            User32Api.SetForegroundWindow(SimpleServiceProvider.Current.GetInstance<NotifyIcon>().ContextMenuStrip.Handle);
            menu.Show(location);
            menu.Focus();
        }

        /// <summary>
        /// Return a menu item
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="addDynamics"></param>
        /// <param name="destinationClickHandler"></param>
        /// <returns>ToolStripMenuItem</returns>
        public virtual ToolStripMenuItem GetMenuItem(bool addDynamics, ContextMenuStrip menu, EventHandler destinationClickHandler)
        {
            var basisMenuItem = new ToolStripMenuItem(Description)
            {
                Image = DisplayIcon,
                Tag = this,
                Text = Description
            };
            AddTagEvents(basisMenuItem, menu, Description);
            basisMenuItem.Click -= destinationClickHandler;
            basisMenuItem.Click += destinationClickHandler;

            if (IsDynamic && addDynamics)
            {
                basisMenuItem.DropDownOpening += delegate
                {
                    if (basisMenuItem.DropDownItems.Count == 0)
                    {
                        List<IDestination> subDestinations = new List<IDestination>();
                        // Fixing Bug #3536968 by catching the COMException (every exception) and not displaying the "subDestinations"
                        try
                        {
                            subDestinations.AddRange(DynamicDestinations());
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Skipping {0}, due to the following error: {1}", Description, ex.Message);
                        }

                        if (subDestinations.Count > 0)
                        {
                            if (UseDynamicsOnly && subDestinations.Count == 1)
                            {
                                basisMenuItem.Tag = subDestinations[0];
                                basisMenuItem.Text = subDestinations[0].Description;
                                basisMenuItem.Click -= destinationClickHandler;
                                basisMenuItem.Click += destinationClickHandler;
                            }
                            else
                            {
                                foreach (IDestination subDestination in subDestinations)
                                {
                                    var destinationMenuItem = new ToolStripMenuItem(subDestination.Description)
                                    {
                                        Tag = subDestination,
                                        Image = subDestination.DisplayIcon
                                    };
                                    destinationMenuItem.Click += destinationClickHandler;
                                    AddTagEvents(destinationMenuItem, menu, subDestination.Description);
                                    basisMenuItem.DropDownItems.Add(destinationMenuItem);
                                }
                            }
                        }
                    }
                };
            }

            return basisMenuItem;
        }
    }
}