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
using Greenshot.Tests.Fixtures.Collections;

namespace Greenshot.Tests.Fixtures.Base;

/// <summary>
/// Provides a test fixture that manages a shared <see cref="SimpleServiceProvider"/> 
/// instance with pre-configured services for integration testing.
/// </summary>
/// <remarks>
/// <para>
/// This fixture follows the xUnit collection fixture pattern, ensuring that:
/// <list type="bullet">
/// <item>Setup occurs once before any tests in the collection run</item>
/// <item>The same fixture instance is shared across all tests in the collection</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong> Apply i.e. the <c>[Collection("DefaultCollection")]</c> <see cref="DefaultCollection"/> attribute 
/// to your test class to use this fixture. The fixture will be automatically injected 
/// as a constructor parameter.
/// </para>
/// <para>
/// <strong>Current Services:</strong>
/// <list type="bullet">
/// <item><see cref="ISurface"/> factory - Provides <see cref="Surface"/> instances</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Collection("DefaultCollection")]
/// public class MyIntegrationTests
/// {
///     private readonly SimpleServiceProviderFixture _fixture;
///     
///     public MyIntegrationTests(SimpleServiceProviderFixture fixture)
///     {
///         _fixture = fixture;
///     }
/// }
/// </code>
/// </example>
public class SimpleServiceProviderFixture
{
    /// <summary>
    /// Initializes the shared test environment before the first test in the collection executes.
    /// </summary>
    public SimpleServiceProviderFixture()
    {
        TestEnvironment.EnsureInitialized();
    }
}
