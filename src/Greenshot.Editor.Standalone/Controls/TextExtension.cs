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
using System.ComponentModel;
using Avalonia;

namespace Greenshot.Editor.Standalone.Controls
{
    /// <summary>
    /// {l:Text Save} in the XAML: the text of the language pack with that key. It is a binding, so it follows when the
    /// language changes. The text is asked from the language pack by its key and not from a property found by its name
    /// while running, so it also works when the editor is compiled ahead and trimmed (native AOT).
    /// </summary>
    public sealed class TextExtension
    {
        public TextExtension()
        {
        }

        public TextExtension(string key)
        {
            Key = key;
        }

        /// <summary>
        /// The name of the property of <see cref="IEditorLanguage"/>
        /// </summary>
        public string Key { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider) => new Texts(Key).ToBinding();

        // The text now and after every change of the language
        private sealed class Texts : IObservable<object>
        {
            private readonly string _key;

            public Texts(string key) => _key = key;

            public IDisposable Subscribe(IObserver<object> observer)
            {
                var texts = EditorTexts.Text;
                observer.OnNext(EditorTexts.TextOf(_key));
                void Changed(object sender, PropertyChangedEventArgs e)
                {
                    if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == _key || e.PropertyName == "Item[]")
                    {
                        observer.OnNext(EditorTexts.TextOf(_key));
                    }
                }

                texts.PropertyChanged += Changed;
                return new Subscription(() => texts.PropertyChanged -= Changed);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action _end;

            public Subscription(Action end) => _end = end;

            public void Dispose()
            {
                _end?.Invoke();
                _end = null;
            }
        }
    }
}
