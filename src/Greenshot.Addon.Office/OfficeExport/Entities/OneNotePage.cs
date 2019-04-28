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

namespace Greenshot.Addon.Office.OfficeExport.Entities
{
    /// <summary>
    ///     Container for transporting Page information
    /// </summary>
    public class OneNotePage
    {
        /// <inherit />
        public string DisplayName
        {
            get
            {
                var notebook = Parent.Parent;
                if (string.IsNullOrEmpty(notebook.Name))
                {
                    return string.Format("{0} / {1}", Parent.Name, Name);
                }
                return string.Format("{0} / {1} / {2}", Parent.Parent.Name, Parent.Name, Name);
            }
        }

        /// <inherit />
        public string Id { get; set; }

        /// <inherit />
        public bool IsCurrentlyViewed { get; set; }

        /// <inherit />
        public string Name { get; set; }

        /// <inherit />
        public OneNoteSection Parent { get; set; }
    }
}