#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Collections.Generic;
using System.Globalization;
using System.Text;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     This parses a JSON response, a modified version of the code found at:
	///     See: http://techblog.procurios.nl/k/n618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
	///     This file is under the MIT License, which is GPL Compatible and according to:
	///     http://en.wikipedia.org/wiki/MIT_License
	///     can be used under the GPL "umbrella".
	///     TODO: code should be replaced when upgrading to .NET 3.5 or higher!!
	/// </summary>
	public class JSONHelper
	{
		public const int TOKEN_NONE = 0;
		public const int TOKEN_CURLY_OPEN = 1;
		public const int TOKEN_CURLY_CLOSE = 2;
		public const int TOKEN_SQUARED_OPEN = 3;
		public const int TOKEN_SQUARED_CLOSE = 4;
		public const int TOKEN_COLON = 5;
		public const int TOKEN_COMMA = 6;
		public const int TOKEN_STRING = 7;
		public const int TOKEN_NUMBER = 8;
		public const int TOKEN_TRUE = 9;
		public const int TOKEN_FALSE = 10;
		public const int TOKEN_NULL = 11;

		private const int BUILDER_CAPACITY = 2000;

		/// <summary>
		///     Parses the string json into a value
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
		public static IDictionary<string, object> JsonDecode(string json)
		{
			var success = true;

			return JsonDecode(json, ref success);
		}

		/// <summary>
		///     Parses the string json into a value; and fills 'success' with the successfullness of the parse.
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <param name="success">Successful parse?</param>
		/// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
		public static IDictionary<string, object> JsonDecode(string json, ref bool success)
		{
			success = true;
			if (json != null)
			{
				var charArray = json.ToCharArray();
				var index = 0;
				var value = ParseValue(charArray, ref index, ref success) as IDictionary<string, object>;
				return value;
			}
			return null;
		}

		protected static IDictionary<string, object> ParseObject(char[] json, ref int index, ref bool success)
		{
			IDictionary<string, object> table = new Dictionary<string, object>();
			int token;

			// {
			NextToken(json, ref index);

			var done = false;
			while (!done)
			{
				token = LookAhead(json, index);
				if (token == TOKEN_NONE)
				{
					success = false;
					return null;
				}
				if (token == TOKEN_COMMA)
				{
					NextToken(json, ref index);
				}
				else if (token == TOKEN_CURLY_CLOSE)
				{
					NextToken(json, ref index);
					return table;
				}
				else
				{
					// name
					var name = ParseString(json, ref index, ref success);
					if (!success)
					{
						success = false;
						return null;
					}

					// :
					token = NextToken(json, ref index);
					if (token != TOKEN_COLON)
					{
						success = false;
						return null;
					}

					// value
					var value = ParseValue(json, ref index, ref success);
					if (!success)
					{
						success = false;
						return null;
					}

					table.Add(name, value);
				}
			}

			return table;
		}

		protected static IList<object> ParseArray(char[] json, ref int index, ref bool success)
		{
			IList<object> array = new List<object>();

			// [
			NextToken(json, ref index);

			var done = false;
			while (!done)
			{
				var token = LookAhead(json, index);
				if (token == TOKEN_NONE)
				{
					success = false;
					return null;
				}
				if (token == TOKEN_COMMA)
				{
					NextToken(json, ref index);
				}
				else if (token == TOKEN_SQUARED_CLOSE)
				{
					NextToken(json, ref index);
					break;
				}
				else
				{
					var value = ParseValue(json, ref index, ref success);
					if (!success)
					{
						return null;
					}

					array.Add(value);
				}
			}

			return array;
		}

		protected static object ParseValue(char[] json, ref int index, ref bool success)
		{
			switch (LookAhead(json, index))
			{
				case TOKEN_STRING:
					return ParseString(json, ref index, ref success);
				case TOKEN_NUMBER:
					return ParseNumber(json, ref index, ref success);
				case TOKEN_CURLY_OPEN:
					return ParseObject(json, ref index, ref success);
				case TOKEN_SQUARED_OPEN:
					return ParseArray(json, ref index, ref success);
				case TOKEN_TRUE:
					NextToken(json, ref index);
					return true;
				case TOKEN_FALSE:
					NextToken(json, ref index);
					return false;
				case TOKEN_NULL:
					NextToken(json, ref index);
					return null;
				case TOKEN_NONE:
					break;
			}

			success = false;
			return null;
		}

		protected static string ParseString(char[] json, ref int index, ref bool success)
		{
			var s = new StringBuilder(BUILDER_CAPACITY);

			EatWhitespace(json, ref index);

			// "
			var c = json[index++];

			var complete = false;
			while (!complete)
			{
				if (index == json.Length)
				{
					break;
				}

				c = json[index++];
				if (c == '"')
				{
					complete = true;
					break;
				}
				if (c == '\\')
				{
					if (index == json.Length)
					{
						break;
					}
					c = json[index++];
					if (c == '"')
					{
						s.Append('"');
					}
					else if (c == '\\')
					{
						s.Append('\\');
					}
					else if (c == '/')
					{
						s.Append('/');
					}
					else if (c == 'b')
					{
						s.Append('\b');
					}
					else if (c == 'f')
					{
						s.Append('\f');
					}
					else if (c == 'n')
					{
						s.Append('\n');
					}
					else if (c == 'r')
					{
						s.Append('\r');
					}
					else if (c == 't')
					{
						s.Append('\t');
					}
					else if (c == 'u')
					{
						var remainingLength = json.Length - index;
						if (remainingLength >= 4)
						{
							// parse the 32 bit hex into an integer codepoint
							uint codePoint;
							if (!(success = uint.TryParse(new string(json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint)))
							{
								return "";
							}
							// convert the integer codepoint to a unicode char and add to string
							s.Append(char.ConvertFromUtf32((int) codePoint));
							// skip 4 chars
							index += 4;
						}
						else
						{
							break;
						}
					}
				}
				else
				{
					s.Append(c);
				}
			}

			if (!complete)
			{
				success = false;
				return null;
			}

			return s.ToString();
		}

		protected static double ParseNumber(char[] json, ref int index, ref bool success)
		{
			EatWhitespace(json, ref index);

			var lastIndex = GetLastIndexOfNumber(json, index);
			var charLength = lastIndex - index + 1;

			double number;
			success = double.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);

			index = lastIndex + 1;
			return number;
		}

		protected static int GetLastIndexOfNumber(char[] json, int index)
		{
			int lastIndex;

			for (lastIndex = index; lastIndex < json.Length; lastIndex++)
			{
				if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
				{
					break;
				}
			}
			return lastIndex - 1;
		}

		protected static void EatWhitespace(char[] json, ref int index)
		{
			for (; index < json.Length; index++)
			{
				if (" \t\n\r".IndexOf(json[index]) == -1)
				{
					break;
				}
			}
		}

		protected static int LookAhead(char[] json, int index)
		{
			var saveIndex = index;
			return NextToken(json, ref saveIndex);
		}

		protected static int NextToken(char[] json, ref int index)
		{
			EatWhitespace(json, ref index);

			if (index == json.Length)
			{
				return TOKEN_NONE;
			}

			var c = json[index];
			index++;
			switch (c)
			{
				case '{':
					return TOKEN_CURLY_OPEN;
				case '}':
					return TOKEN_CURLY_CLOSE;
				case '[':
					return TOKEN_SQUARED_OPEN;
				case ']':
					return TOKEN_SQUARED_CLOSE;
				case ',':
					return TOKEN_COMMA;
				case '"':
					return TOKEN_STRING;
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '-':
					return TOKEN_NUMBER;
				case ':':
					return TOKEN_COLON;
			}
			index--;

			var remainingLength = json.Length - index;

			// false
			if (remainingLength >= 5)
			{
				if (json[index] == 'f' &&
					json[index + 1] == 'a' &&
					json[index + 2] == 'l' &&
					json[index + 3] == 's' &&
					json[index + 4] == 'e')
				{
					index += 5;
					return TOKEN_FALSE;
				}
			}

			// true
			if (remainingLength >= 4)
			{
				if (json[index] == 't' &&
					json[index + 1] == 'r' &&
					json[index + 2] == 'u' &&
					json[index + 3] == 'e')
				{
					index += 4;
					return TOKEN_TRUE;
				}
			}

			// null
			if (remainingLength >= 4)
			{
				if (json[index] == 'n' &&
					json[index + 1] == 'u' &&
					json[index + 2] == 'l' &&
					json[index + 3] == 'l')
				{
					index += 4;
					return TOKEN_NULL;
				}
			}

			return TOKEN_NONE;
		}
	}
}