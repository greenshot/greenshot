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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Base.Pipeline.Contracts
{
    /// <summary>
    /// Global registry indexing step contracts by step type identifier.
    /// </summary>
    public static class StepContractRegistry
    {
        private static readonly ConcurrentDictionary<string, StepContract> _contracts =
            new ConcurrentDictionary<string, StepContract>(StringComparer.OrdinalIgnoreCase);

        public static void Register(StepContract contract)
        {
            if (contract == null || string.IsNullOrWhiteSpace(contract.StepType)) return;
            _contracts[contract.StepType] = contract;
        }

        public static void Register<T>() where T : ICaptureStep
        {
            var contract = StepContractBuilder.FromType(typeof(T));
            if (contract != null)
            {
                Register(contract);
            }
        }

        public static void Register(Type stepType)
        {
            if (stepType == null) return;
            var contract = StepContractBuilder.FromType(stepType);
            if (contract != null)
            {
                Register(contract);
            }
        }

        public static StepContract GetContract(string stepType)
        {
            if (string.IsNullOrWhiteSpace(stepType)) return null;
            if (_contracts.TryGetValue(stepType, out var contract))
            {
                return contract;
            }
            return null;
        }

        public static bool TryGetContract(string stepType, out StepContract contract)
        {
            if (string.IsNullOrWhiteSpace(stepType))
            {
                contract = null;
                return false;
            }
            return _contracts.TryGetValue(stepType, out contract);
        }

        public static IReadOnlyCollection<StepContract> GetAllContracts()
        {
            return _contracts.Values.ToList().AsReadOnly();
        }
    }
}
