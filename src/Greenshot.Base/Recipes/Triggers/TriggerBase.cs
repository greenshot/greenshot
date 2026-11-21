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
using System.Linq;

namespace Greenshot.Base.Recipes.Triggers
{
    /// <summary>
    /// Abstract base class for ITrigger implementations providing standard event handling and lifecycle.
    /// </summary>
    public abstract class TriggerBase : ITrigger
    {
        private bool _disposed;

        public string Id { get; protected set; }
        public string Name { get; set; }
        public virtual string TriggerType => GetType().Name.Replace("Trigger", "");
        public string TargetRecipeId { get; set; }
        public bool IsEnabled { get; set; } = true;

        public event EventHandler<TriggerEventArgs> Triggered;

        protected TriggerBase(string id, string name, string targetRecipeId)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            Name = name;
            TargetRecipeId = targetRecipeId;
        }

        /// <summary>
        /// What defines the trigger next to its type, id, name and recipe, e.g. the hotkey or the menu text.
        /// Every trigger with settings of its own lists them here.
        /// </summary>
        protected virtual IEnumerable<object> DefinitionValues => Enumerable.Empty<object>();

        /// <summary>
        /// The other trigger does the same as this one: same type, id, name, recipe and definition.
        /// A running trigger is kept when the new one has the same definition, it isn't registered again.
        /// </summary>
        public bool HasSameDefinition(ITrigger other) =>
            other is TriggerBase otherTrigger
            && otherTrigger.GetType() == GetType()
            && string.Equals(otherTrigger.Id, Id, StringComparison.Ordinal)
            && string.Equals(otherTrigger.Name, Name, StringComparison.Ordinal)
            && string.Equals(otherTrigger.TargetRecipeId, TargetRecipeId, StringComparison.Ordinal)
            && otherTrigger.IsEnabled == IsEnabled
            && DefinitionValues.SequenceEqual(otherTrigger.DefinitionValues);

        /// <summary>
        /// The values of the arguments of a trigger, for <see cref="DefinitionValues"/>
        /// </summary>
        protected static IEnumerable<object> ArgumentValues(IReadOnlyCollection<CommandlineArgument> arguments)
        {
            yield return arguments?.Count ?? 0;
            if (arguments == null) yield break;
            foreach (var argument in arguments)
            {
                yield return argument?.Name;
                yield return argument?.Variable;
                yield return argument?.Description;
                yield return argument?.Required;
                yield return argument?.DefaultValue;
                yield return argument?.Type;
                yield return argument?.AllowedValues == null ? null : string.Join("\n", argument.AllowedValues);
            }
        }

        public abstract void Start();
        public abstract void Stop();

        /// <summary>
        /// Fires the Triggered event to start the pipeline.
        /// </summary>
        protected virtual void OnTriggered(IDictionary<string, object> parameters = null)
        {
            if (!IsEnabled) return;
            Triggered?.Invoke(this, new TriggerEventArgs(TargetRecipeId, parameters));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Stop();
            }
        }
    }
}
