#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

#endregion

namespace Greenshot.Addon.OcrCommand
{
	/// <summary>
	///     Wraps a late-bound COM server.
	/// </summary>
	public sealed class COMWrapper : RealProxy, IDisposable, IRemotingTypeInfo
	{
		private const int MK_E_UNAVAILABLE = -2147221021;
		private const int CO_E_CLASSSTRING = -2147221005;

		/// <summary>
		///     Implementation for the interface IRemotingTypeInfo
		///     This makes it possible to cast the COMWrapper
		/// </summary>
		/// <param name="toType">Type to cast to</param>
		/// <param name="o">object to cast</param>
		/// <returns></returns>
		public bool CanCastTo(Type toType, object o)
		{
			var returnValue = _interceptType.IsAssignableFrom(toType);
			return returnValue;
		}

		/// <summary>
		///     Implementation for the interface IRemotingTypeInfo
		/// </summary>
		public string TypeName
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		///     Intercept method calls
		/// </summary>
		/// <param name="myMessage">
		///     Contains information about the method being called
		/// </param>
		/// <returns>
		///     A <see cref="ReturnMessage" />.
		/// </returns>
		public override IMessage Invoke(IMessage myMessage)
		{
			var callMessage = myMessage as IMethodCallMessage;

			var method = callMessage?.MethodBase as MethodInfo;
			if (method == null)
			{
				if (callMessage != null)
				{
					Debug.WriteLine($"Unrecognized Invoke call: {callMessage.MethodBase}");
				}
				return null;
			}

			object returnValue = null;
			object[] outArgs = null;
			var outArgsCount = 0;

			var methodName = method.Name;
			var returnType = method.ReturnType;
			var flags = BindingFlags.InvokeMethod;
			var argCount = callMessage.ArgCount;

			ParameterModifier[] argModifiers = null;
			ParameterInfo[] parameters = null;

			if ("Dispose" == methodName && 0 == argCount && typeof(void) == returnType)
			{
				Dispose();
			}
			else if ("ToString" == methodName && 0 == argCount && typeof(string) == returnType)
			{
				returnValue = ToString();
			}
			else if ("GetType" == methodName && 0 == argCount && typeof(Type) == returnType)
			{
				returnValue = _interceptType;
			}
			else if ("GetHashCode" == methodName && 0 == argCount && typeof(int) == returnType)
			{
				returnValue = GetHashCode();
			}
			else if ("Equals" == methodName && 1 == argCount && typeof(bool) == returnType)
			{
				returnValue = Equals(callMessage.Args[0]);
			}
			else if (1 == argCount && typeof(void) == returnType && (methodName.StartsWith("add_") || methodName.StartsWith("remove_")))
			{
				var handler = callMessage.InArgs[0] as Delegate;
				if (null == handler)
				{
					return new ReturnMessage(new ArgumentNullException(nameof(handler)), callMessage);
				}
			}
			else
			{
				var invokeObject = _comObject;
				var invokeType = _comType;

				ParameterInfo parameter;
				object[] args;
				if (methodName.StartsWith("get_"))
				{
					// Property Get
					methodName = methodName.Substring(4);
					flags = BindingFlags.GetProperty;
					args = callMessage.InArgs;
				}
				else if (methodName.StartsWith("set_"))
				{
					// Property Set
					methodName = methodName.Substring(4);
					flags = BindingFlags.SetProperty;
					args = callMessage.InArgs;
				}
				else
				{
					args = callMessage.Args;
					if (null != args && 0 != args.Length)
					{
						// Modifiers for ref / out parameters
						argModifiers = new ParameterModifier[1];
						argModifiers[0] = new ParameterModifier(args.Length);

						parameters = method.GetParameters();
						for (var i = 0; i < parameters.Length; i++)
						{
							parameter = parameters[i];
							if (parameter.IsOut || parameter.ParameterType.IsByRef)
							{
								argModifiers[0][i] = true;
								outArgsCount++;
							}
						}

						if (0 == outArgsCount)
						{
							argModifiers = null;
						}
					}
				}

				// Un-wrap wrapped COM objects before passing to the method
				COMWrapper[] originalArgs;
				COMWrapper wrapper;
				Type byValType;
				if (null == args || 0 == args.Length)
				{
					originalArgs = null;
				}
				else
				{
					originalArgs = new COMWrapper[args.Length];
					for (var i = 0; i < args.Length; i++)
					{
						if (null != args[i] && RemotingServices.IsTransparentProxy(args[i]))
						{
							wrapper = RemotingServices.GetRealProxy(args[i]) as COMWrapper;
							if (null != wrapper)
							{
								originalArgs[i] = wrapper;
								args[i] = wrapper._comObject;
							}
						}
						else if (argModifiers != null && 0 != outArgsCount && argModifiers[0][i])
						{
							byValType = GetByValType(parameters[i].ParameterType);
							if (byValType.IsInterface)
							{
								// If we're passing a COM object by reference, and
								// the parameter is null, we need to pass a
								// DispatchWrapper to avoid a type mismatch exception.
								if (null == args[i])
								{
									args[i] = new DispatchWrapper(null);
								}
							}
							else if (typeof(decimal) == byValType)
							{
								// If we're passing a decimal value by reference,
								// we need to pass a CurrencyWrapper to avoid a 
								// type mismatch exception.
								// http://support.microsoft.com/?kbid=837378
								args[i] = new CurrencyWrapper(args[i]);
							}
						}
					}
				}

				try
				{
					returnValue = invokeType.InvokeMember(methodName, flags, null, invokeObject, args, argModifiers, null, null);
				}
				catch (Exception ex)
				{
					return new ReturnMessage(ex, callMessage);
				}

				// Handle enum and interface return types
				if (null != returnValue)
				{
					if (returnType.IsInterface)
					{
						// Wrap the returned value in an intercepting COM wrapper
						if (Marshal.IsComObject(returnValue))
						{
							returnValue = Wrap(returnValue, returnType);
						}
					}
					else if (returnType.IsEnum)
					{
						// Convert to proper Enum type
						returnValue = Enum.Parse(returnType, returnValue.ToString());
					}
				}

				// Handle out args
				if (0 != outArgsCount)
				{
					if (args != null && parameters != null)
					{
						outArgs = new object[args.Length];
						for (var i = 0; i < parameters.Length; i++)
						{
							if (argModifiers != null && !argModifiers[0][i])
							{
								continue;
							}

							var arg = args[i];
							if (null == arg)
							{
								continue;
							}

							parameter = parameters[i];
							wrapper = null;

							byValType = GetByValType(parameter.ParameterType);
							if (typeof(decimal) == byValType)
							{
								if (arg is CurrencyWrapper)
								{
									arg = ((CurrencyWrapper) arg).WrappedObject;
								}
							}
							else if (byValType.IsEnum)
							{
								arg = Enum.Parse(byValType, arg.ToString());
							}
							else if (byValType.IsInterface)
							{
								if (Marshal.IsComObject(arg))
								{
									if (originalArgs != null)
									{
										wrapper = originalArgs[i];
									}
									if (null != wrapper && wrapper._comObject != arg)
									{
										wrapper.Dispose();
										wrapper = null;
									}

									if (null == wrapper)
									{
										wrapper = new COMWrapper(arg, byValType);
									}
									arg = wrapper.GetTransparentProxy();
								}
							}
							outArgs[i] = arg;
						}
					}
				}
			}

			return new ReturnMessage(returnValue, outArgs, outArgsCount, callMessage.LogicalCallContext, callMessage);
		}

		#region Private Data

		/// <summary>
		///     Holds reference to the actual COM object which is wrapped by this proxy
		/// </summary>
		private readonly object _comObject;

		/// <summary>
		///     Type of the COM object, set on constructor after getting the COM reference
		/// </summary>
		private readonly Type _comType;

		/// <summary>
		///     The type of which method calls are intercepted and executed on the COM object.
		/// </summary>
		private readonly Type _interceptType;

		#endregion

		#region Construction

		/// <summary>
		///     Gets a COM object and returns the transparent proxy which intercepts all calls to the object
		/// </summary>
		/// <typeparam name="T">Interface which defines the method and properties to intercept</typeparam>
		/// <returns>Transparent proxy to the real proxy for the object</returns>
		/// <remarks>T must be an interface decorated with the <see cref="ComProgIdAttribute" />attribute.</remarks>
		public static T GetInstance<T>()
		{
			var type = typeof(T);
			if (null == type)
			{
				throw new ArgumentNullException(nameof(T));
			}
			if (!type.IsInterface)
			{
				throw new ArgumentException("The specified type must be an interface.", nameof(T));
			}

			var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
			if (string.IsNullOrEmpty(progIdAttribute?.Value))
			{
				throw new ArgumentException("The specified type must define a ComProgId attribute.", nameof(T));
			}
			var progId = progIdAttribute.Value;

			object comObject = null;
			try
			{
				comObject = Marshal.GetActiveObject(progId);
			}
			catch (COMException comE)
			{
				if (comE.ErrorCode == MK_E_UNAVAILABLE)
				{
					Debug.WriteLine($"No current instance of {progId} object available.");
				}
				else if (comE.ErrorCode == CO_E_CLASSSTRING)
				{
					Debug.WriteLine($"Unknown progId {progId}");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error getting active object for {progId} {ex.Message}");
			}

			if (comObject != null)
			{
				var wrapper = new COMWrapper(comObject, type);
				return (T) wrapper.GetTransparentProxy();
			}
			return default;
		}

		/// <summary>
		///     Gets or creates a COM object and returns the transparent proxy which intercepts all calls to the object
		///     The ComProgId can be a normal ComProgId or a GUID prefixed with "clsid:"
		/// </summary>
		/// <typeparam name="T">Interface which defines the method and properties to intercept</typeparam>
		/// <returns>Transparent proxy to the real proxy for the object</returns>
		/// <remarks>The type must be an interface decorated with the <see cref="ComProgIdAttribute" />attribute.</remarks>
		public static T GetOrCreateInstance<T>()
		{
			var type = typeof(T);
			if (null == type)
			{
				throw new ArgumentNullException(nameof(T));
			}
			if (!type.IsInterface)
			{
				throw new ArgumentException("The specified type must be an interface.", nameof(T));
			}

			var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
			if (string.IsNullOrEmpty(progIdAttribute?.Value))
			{
				throw new ArgumentException("The specified type must define a ComProgId attribute.", nameof(T));
			}

			object comObject = null;
			Type comType = null;
			var progId = progIdAttribute.Value;

			try
			{
				comObject = Marshal.GetActiveObject(progId);
			}
			catch (COMException comE)
			{
				if (comE.ErrorCode == MK_E_UNAVAILABLE)
				{
					Debug.WriteLine($"No current instance of {progId} object available.");
				}
				else if (comE.ErrorCode == CO_E_CLASSSTRING)
				{
					Debug.WriteLine($"Unknown progId {progId}");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error getting active object for {progId} {ex.Message}");
			}
			// Did we get the current instance? If not, try to create a new
			if (comObject == null)
			{
				try
				{
					comType = Type.GetTypeFromProgID(progId, true);
				}
				catch (Exception)
				{
					Debug.WriteLine($"Error getting type for {progId}");
				}
				if (comType != null)
				{
					try
					{
						comObject = Activator.CreateInstance(comType);
						if (comObject != null)
						{
							Debug.WriteLine($"Created new instance of {progId} object.");
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error creating object for {progId} {ex.Message}");
					}
				}
			}
			if (comObject != null)
			{
				var wrapper = new COMWrapper(comObject, type);
				return (T) wrapper.GetTransparentProxy();
			}
			return default;
		}

		/// <summary>
		///     Wrap an object and return the transparent proxy which intercepts all calls to the object
		/// </summary>
		/// <param name="comObject">An object to intercept</param>
		/// <param name="type">Interface which defines the method and properties to intercept</param>
		/// <returns>Transparent proxy to the real proxy for the object</returns>
		private static object Wrap(object comObject, Type type)
		{
			if (null == comObject)
			{
				throw new ArgumentNullException(nameof(comObject));
			}
			if (null == type)
			{
				throw new ArgumentNullException(nameof(type));
			}

			var wrapper = new COMWrapper(comObject, type);
			return wrapper.GetTransparentProxy();
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="comObject">
		///     The COM object to wrap.
		/// </param>
		/// <param name="type">
		///     The interface type to impersonate.
		/// </param>
		private COMWrapper(object comObject, Type type)
			: base(type)
		{
			_comObject = comObject;
			_comType = comObject.GetType();
			_interceptType = type;
		}

		#endregion

		#region Clean up

		/// <summary>
		///     If dispose() is not called, we need to make
		///     sure that the COM object is still cleaned up.
		/// </summary>
		~COMWrapper()
		{
			Dispose(false);
		}

		/// <summary>
		///     Cleans up the COM object.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///     Release the COM reference
		/// </summary>
		/// <param name="disposing">
		///     <see langword="true" /> if this was called from the
		///     <see cref="IDisposable" /> interface.
		/// </param>
		private void Dispose(bool disposing)
		{
			if (disposing && null != _comObject)
			{
				if (Marshal.IsComObject(_comObject))
				{
					try
					{
						while (Marshal.ReleaseComObject(_comObject) > 0)
						{
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Problem releasing {_comType}");
						Debug.WriteLine("Error: " + ex);
					}
				}
			}
		}

		#endregion

		#region Object methods

		/// <summary>
		///     Returns a string representing the wrapped object.
		/// </summary>
		/// <returns>
		///     The full name of the intercepted type.
		/// </returns>
		public override string ToString()
		{
			return _interceptType.FullName;
		}

		/// <summary>
		///     Returns the hash code of the wrapped object.
		/// </summary>
		/// <returns>
		///     The hash code of the wrapped object.
		/// </returns>
		public override int GetHashCode()
		{
			return _comObject.GetHashCode();
		}

		/// <summary>
		///     Compares this object to another.
		/// </summary>
		/// <param name="value">
		///     The value to compare to.
		/// </param>
		/// <returns>
		///     <see langword="true" /> if the objects are equal.
		/// </returns>
		public override bool Equals(object value)
		{
			if (null != value && RemotingServices.IsTransparentProxy(value))
			{
				var wrapper = RemotingServices.GetRealProxy(value) as COMWrapper;
				if (null != wrapper)
				{
					return _comObject == wrapper._comObject;
				}
			}

			return base.Equals(value);
		}

		/// <summary>
		///     Returns the base type for a reference type.
		/// </summary>
		/// <param name="byRefType">
		///     The reference type.
		/// </param>
		/// <returns>
		///     The base value type.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="byRefType" /> is <see langword="null" />.
		/// </exception>
		private static Type GetByValType(Type byRefType)
		{
			if (null == byRefType)
			{
				throw new ArgumentNullException(nameof(byRefType));
			}

			if (byRefType.IsByRef)
			{
				var name = byRefType.FullName;
				name = name.Substring(0, name.Length - 1);
				byRefType = byRefType.Assembly.GetType(name, true);
			}

			return byRefType;
		}

		#endregion
	}
}