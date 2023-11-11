/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Editor.Controls.Emoji;

/// <summary>
/// This seems to enumerate multiple IEnumerables.
/// TODO: Replace this with LINQ
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class ChunkHelper<T> : IEnumerable<IEnumerable<T>>
{
    private readonly IEnumerable<T> _elements;
    private readonly int _size;
    private bool _hasMore;

    public ChunkHelper(IEnumerable<T> elements, int size)
    {
        _elements = elements;
        _size = size;
    }

    public IEnumerator<IEnumerable<T>> GetEnumerator()
    {
        using var enumerator = _elements.GetEnumerator();
        _hasMore = enumerator.MoveNext();
        while (_hasMore)
        {
            yield return GetNextBatch(enumerator).ToList();
        }
    }

    private IEnumerable<T> GetNextBatch(IEnumerator<T> enumerator)
    {
        for (int i = 0; i < _size; ++i)
        {
            yield return enumerator.Current;
            _hasMore = enumerator.MoveNext();
            if (!_hasMore)
            {
                yield break;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}