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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The Delay step: unattended it shows nothing and never asks, the choices and limits of the seconds
    /// </summary>
    public class DelayStepTests
    {
        public DelayStepTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static (DelayStep Step, CaptureFlowContext Context) Create(Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            var node = new RecipeNodeConfig("wait", WellKnownStepTypes.Delay, "Wait");
            foreach (var parameter in parameters)
            {
                node.Set(parameter.Key, parameter.Value);
            }
            var recipe = new CaptureRecipe("delay_test", "Delay test").AddNode(node);
            var context = new CaptureFlowContext(recipe, cancellationToken: cancellationToken)
            {
                UserInteraction = HeadlessUserInteraction.Instance,
                // Unattended nothing may be shown
                Ui = new NoUiDispatcher()
            };
            return (new DelayStep(node), context);
        }

        [Fact]
        public async Task Unattended_AskUser_DoesNotAsk_AndWaitsTheSeconds()
        {
            var (step, context) = Create(new Dictionary<string, object> { [DelayStep.SecondsParameter] = 0, [DelayStep.AskUserParameter] = true });

            await step.ExecuteAsync(context);

            Assert.False(context.IsAborted, context.AbortReason);
            Assert.Equal(0, context.Properties[DelayStep.OutputVariable]);
        }

        [Fact]
        public async Task CancelledFlow_StopsTheWait()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var (step, context) = Create(new Dictionary<string, object> { [DelayStep.SecondsParameter] = DelayStep.MaxSeconds }, cancellation.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => step.ExecuteAsync(context, cancellation.Token));
        }

        [Fact]
        public async Task SecondsFromText_AreRead()
        {
            // An expression (e.g. ${option.delay}) gives text
            var (step, context) = Create(new Dictionary<string, object> { [DelayStep.SecondsParameter] = "0" });

            await step.ExecuteAsync(context);

            Assert.Equal(0, context.Properties[DelayStep.OutputVariable]);
        }

        [Theory]
        [InlineData(-5, 0)]
        [InlineData(0, 0)]
        [InlineData(7, 7)]
        [InlineData(601, DelayStep.MaxSeconds)]
        public void Seconds_AreLimited(int seconds, int expected)
        {
            Assert.Equal(expected, DelayStep.Clamp(seconds));
        }

        [Theory]
        [InlineData(5, new[] { 3, 5, 10, 30 })]
        [InlineData(7, new[] { 3, 5, 7, 10, 30 })]
        [InlineData(0, new[] { 3, 5, 10, 30 })]
        public void Choices_HaveTheSecondsOfTheStep(int seconds, int[] expected)
        {
            Assert.Equal(expected, DelayStep.ChoicesFor(seconds));
        }

        /// <summary>
        /// Fails every call: the step must not show anything
        /// </summary>
        private sealed class NoUiDispatcher : IUiDispatcher
        {
            public bool CheckAccess() => false;

            public void VerifyAccess() => throw new InvalidOperationException("No UI in an unattended run");

            public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No UI in an unattended run");

            public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No UI in an unattended run");

            public Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No UI in an unattended run");
        }
    }
}
