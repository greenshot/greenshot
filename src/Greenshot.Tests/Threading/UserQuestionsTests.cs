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

using System.Collections.Generic;
using System.Threading.Tasks;
using Greenshot.Base.Threading;
using Xunit;

namespace Greenshot.Tests.Threading
{
    /// <summary>
    /// The questions Greenshot asks on its own come one after another: a question asked while another is shown waits for it
    /// </summary>
    public class UserQuestionsTests
    {
        [Fact]
        public async Task Questions_AreAskedOneAfterAnother_InOrder()
        {
            var order = new List<string>();
            var firstAnswered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var allAsked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            UserQuestions.Enqueue("first", async () =>
            {
                lock (order) order.Add("first shown");
                // A question asked while this one is open
                UserQuestions.Enqueue("third", () =>
                {
                    lock (order) order.Add("third");
                    allAsked.TrySetResult(true);
                });
                await firstAnswered.Task.ConfigureAwait(false);
                lock (order) order.Add("first answered");
            });
            UserQuestions.Enqueue("second", () =>
            {
                lock (order) order.Add("second");
            });

            firstAnswered.SetResult(true);
            Assert.True(await Task.WhenAny(allAsked.Task, Task.Delay(10000)).ConfigureAwait(true) == allAsked.Task, "the questions weren't asked");
            lock (order)
            {
                Assert.Equal(new[] { "first shown", "first answered", "second", "third" }, order);
            }
        }
    }
}
