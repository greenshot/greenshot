/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.IO;
using Greenshot.Base.Trust;
using Xunit;

namespace Greenshot.Tests.Definitions
{
    /// <summary>
    /// The approvals of definition files: kept protected in a file of the user, per path and content
    /// </summary>
    public class FileApprovalsTests
    {
        [Fact]
        public void Approve_IsKeptForThisContent_UntilRevoked()
        {
            string directory = Path.Combine(Path.GetTempPath(), "greenshot-approvals-" + Guid.NewGuid().ToString("N"));
            string store = Path.Combine(directory, "definition_trust.dat");
            string file = Path.Combine(directory, "a.gscommand.json");
            try
            {
                string hash = ContentHash.Sha256(new byte[] { 1, 2, 3 });
                new FileApprovals(() => store).Approve(file, hash);

                // Read again, like after a restart
                var approvals = new FileApprovals(() => store);
                Assert.True(approvals.IsApproved(file, hash));
                Assert.True(approvals.IsKnown(file));
                Assert.False(approvals.IsApproved(file, ContentHash.Sha256(new byte[] { 1, 2, 4 })));
                Assert.DoesNotContain("a.gscommand.json", File.ReadAllText(store));

                approvals.Revoke(file);
                Assert.False(new FileApprovals(() => store).IsApproved(file, hash));
                Assert.False(new FileApprovals(() => store).IsKnown(file));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
