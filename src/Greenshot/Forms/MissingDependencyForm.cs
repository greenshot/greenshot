/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Configuration;

namespace Greenshot.Forms
{
    public partial class MissingDependencyForm : BaseForm
    {
        private MissingDependencyForm()
        {
            InitializeComponent();
            ToFront = true;
        }

        public MissingDependencyForm(Exception exception, string technicalDetails) : this()
        {
            textBoxDetails.Text = technicalDetails;
            labelMissingAssembly.Text = ExtractAssemblyName(exception);
        }

        /// <summary>
        /// Walks the exception chain and extracts the short assembly name from the first
        /// "Could not load file or assembly '...'" message found.
        /// Returns an empty string if none is found.
        /// </summary>
        private static string ExtractAssemblyName(Exception ex)
        {
            while (ex != null)
            {
                if (ex is FileNotFoundException)
                {
                    // Message format: "Could not load file or assembly 'Name, Version=...' or one of its dependencies."
                    var match = Regex.Match(ex.Message, @"'([^,'"]+)");
                    if (match.Success)
                        return match.Groups[1].Value.Trim();
                }
                ex = ex.InnerException;
            }
            return string.Empty;
        }

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://getgreenshot.org/downloads/");
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Language.GetFormattedString(LangKey.error_openlink, "https://getgreenshot.org/downloads/"),
                    Language.GetString(LangKey.error));
            }
        }

        private void LinkDetails_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            panelDetails.Visible = !panelDetails.Visible;
            linkDetails.Text = panelDetails.Visible
                ? Language.GetString(LangKey.missing_dependency_hide_details)
                : Language.GetString(LangKey.missing_dependency_show_details);
        }
    }
}
