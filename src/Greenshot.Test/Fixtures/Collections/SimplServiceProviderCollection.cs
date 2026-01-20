/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Test.Fixtures.Base;
using Xunit;

namespace Greenshot.Test.Fixtures.Collections;

/// <summary>
/// Defines a test collection that groups related integration tests together.
/// This class serves as a marker for xUnit's collection definition and has no implementation.
/// </summary>
/// <remarks>
/// <para>
/// This collection ensures that all tests within it share the same fixture instance
/// and run sequentially rather than in parallel, which is essential for integration
/// tests that may have shared dependencies or state.
/// </para>
/// <para>
/// Currently includes:
/// <list type="bullet">
/// <item><see cref="SimpleServiceProviderFixture"/> - Provides shared service container setup</item>
/// </list>
/// </para>
/// <para>
/// To use this collection, apply the <c>[Collection("DefaultCollection")]</c> attribute
/// to your test classes. Additional fixtures can be added by extending this class
/// or creating new collection definitions as needed.
/// </para>
/// </remarks>
/// TODO: Add possibility to define a dedicated greenshot.ini for all/ special tests.( e.g. fields in container classes read config)
[CollectionDefinition("DefaultCollection")]
public class DefaultCollection : ICollectionFixture<SimpleServiceProviderFixture> { }
