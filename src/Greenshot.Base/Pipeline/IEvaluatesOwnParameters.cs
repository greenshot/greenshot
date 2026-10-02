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

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Marks a step that evaluates the <c>${...}</c> expressions in its parameters itself, e.g. because it needs extra
    /// variables that only exist while it runs (like the surface size for annotations).
    /// </summary>
    /// <remarks>
    /// Expressions are evaluated exactly once. Normally the DAG engine resolves all node parameters before the step runs,
    /// and steps must use those values as they are: evaluating them again would expand <c>${...}</c> text that came from
    /// data (OCR text, a QR code, a web page title) and let that data read environment or configuration values.
    /// For steps implementing this interface the engine passes the raw parameters instead; such a step must also evaluate
    /// each parameter only once.
    /// </remarks>
    public interface IEvaluatesOwnParameters
    {
    }
}
