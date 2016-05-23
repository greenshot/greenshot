﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Greenshot.Interop {
	/// <summary>
	/// Wraps a late-bound COM server.
	/// </summary>
	public sealed class COMWrapper : RealProxy, IDisposable, IRemotingTypeInfo {
		private const int MK_E_UNAVAILABLE = -2147221021;
		private const int CO_E_CLASSSTRING = -2147221005;

		#region Private Data

		/// <summary>
		/// Holds reference to the actual COM object which is wrapped by this proxy
		/// </summary>
		private object _COMObject;

		/// <summary>
		/// Type of the COM object, set on constructor after getting the COM reference
		/// </summary>
		private readonly Type _COMType;

		/// <summary>
		/// The type of which method calls are intercepted and executed on the COM object.
		/// </summary>
		private readonly Type _InterceptType;

		#endregion
		[DllImport("ole32.dll")]
		static extern int ProgIDFromCLSID([In] ref Guid clsid, [MarshalAs(UnmanagedType.LPWStr)] out string lplpszProgID);

		#region Construction

		/// <summary>
		/// Gets a COM object and returns the transparent proxy which intercepts all calls to the object
		/// </summary>
		/// <param name="type">Interface which defines the method and properties to intercept</param>
		/// <returns>Transparent proxy to the real proxy for the object</returns>
		/// <remarks>The <paramref name="type"/> must be an interface decorated with the <see cref="ComProgIdAttribute"/>attribute.</remarks>
		public static T GetInstance<T>() {
			Type type = typeof(T);
			if (null == type) {
				throw new ArgumentNullException("type");
			}
			if (!type.IsInterface) {
				throw new ArgumentException("The specified type must be an interface.", "type");
			}

			ComProgIdAttribute progIDAttribute = ComProgIdAttribute.GetAttribute(type);
			if (null == progIDAttribute || null == progIDAttribute.Value || 0 == progIDAttribute.Value.Length) {
				throw new ArgumentException("The specified type must define a ComProgId attribute.", "type");
			}
			string progId = progIDAttribute.Value;

			// Convert from clsid to Prog ID, if needed
			if (progId.StartsWith("clsid:")) {
				Guid guid = new Guid(progId.Substring(6));
				int result = ProgIDFromCLSID(ref guid, out progId);
				if (result != 0) {
					//LOG.WarnFormat("Error {0} getting progId {1}", result, progIDAttribute.Value);
				} else {
					//LOG.InfoFormat("Mapped {0} to progId {1}", progIDAttribute.Value, progId);
				}
			}

			object comObject = null;
			try {
				comObject = Marshal.GetActiveObject(progId);
			} catch (COMException comE) {
				if (comE.ErrorCode == MK_E_UNAVAILABLE) {
					//LOG.DebugFormat("No current instance of {0} object available.", progId);
				} else if (comE.ErrorCode == CO_E_CLASSSTRING) {
					//LOG.WarnFormat("Unknown progId {0}", progId);
				} else {
					//LOG.Warn("Error getting active object for " + progId, comE);
				}
			} catch (Exception) {
				//LOG.Warn("Error getting active object for " + progId, e);
			}

			if (comObject != null) {
				COMWrapper wrapper = new COMWrapper(comObject, type);
				return (T)wrapper.GetTransparentProxy();
			}
			return default(T);
		}

		/// <summary>
		/// Gets or creates a COM object and returns the transparent proxy which intercepts all calls to the object
		/// The ComProgId can be a normal ComProgId or a GUID prefixed with "clsid:"
		/// </summary>
		/// <param name="type">Interface which defines the method and properties to intercept</param>
		/// <returns>Transparent proxy to the real proxy for the object</returns>
		/// <remarks>The <paramref name="type"/> must be an interface decorated with the <see cref="ComProgIdAttribute"/>attribute.</remarks>
		public static T GetOrCreateInstance<T>() {
			Type type = typeof(T);
			if (null == type) {
				throw new ArgumentNullException("type");
			}
			if (!type.IsInterface) {
				throw new ArgumentException("The specified type must be an interface.", "type");
			}

			ComProgIdAttribute progIDAttribute = ComProgIdAttribute.GetAttribute(type);
			if (null == progIDAttribute || null == progIDAttribute.Value || 0 == progIDAttribute.Value.Length) {
				throw new ArgumentException("The specified type must define a ComProgId attribute.", "type");
			}

			object comObject = null;
			Type comType = null;
			string progId = progIDAttribute.Value;

			// Convert from clsid to Prog ID, if needed
			if (progId.StartsWith("clsid:")) {
				Guid guid = new Guid(progId.Substring(6));
				int result = ProgIDFromCLSID(ref guid, out progId);
				if (result != 0) {
					//LOG.WarnFormat("Error {0} getting progId {1}", result, progIDAttribute.Value);
				} else {
					//LOG.InfoFormat("Mapped {0} to progId {1}", progIDAttribute.Value, progId);
				}
			}

			try {
				comObject = Marshal.GetActiveObject(progId);
			} catch (COMException comE) {
				if (comE.ErrorCode == MK_E_UNAVAILABLE) {
					//LOG.DebugFormat("No current instance of {0} object available.", progId);
				} else if (comE.ErrorCode == CO_E_CLASSSTRING) {
					//LOG.WarnFormat("Unknown progId {0} (application not installed)", progId);
					return default(T);
				} else {
					//LOG.Warn("Error getting active object for " + progId, comE);
				}
			} catch (Exception) {
				//LOG.Warn("Error getting active object for " + progId, e);
			}
			// Did we get the current instance? If not, try to create a new
			if (comObject == null) {
				try {
					comType = Type.GetTypeFromProgID(progId, true);
				} catch (Exception) {
					//LOG.Warn("Error type for " + progId, ex);
				}
				if (comType != null) {
					try {
						comObject = Activator.CreateInstance(comType);
						if (comObject != null) {
							//LOG.DebugFormat("Created new instance of {0} object.", progId);
						}
					} catch (Exception) {
						//LOG.Warn("Error creating object for " + progId, e);
					}
				}
			}
			if (comObject != null) {
				COMWrapper wrapper = new COMWrapper(comObject, type);
				return (T)wrapper.GetTransparentProxy();
			}
			return default(T);
		}

		/// <summary>
		/// Wrap an object and return the transparent proxy which intercepts all calls to the object
		/// </summary>
		/// <param name="comObject">An object to intercept</param>
		/// <param name="type">Interface which defines the method and properties to intercept</param>
		/// <returns>Transparent proxy to the real proxy for the object</returns>
		private static object Wrap(object comObject, Type type) {
			if (null == comObject) {
				throw new ArgumentNullException("comObject");
			}
			if (null == type) {
				throw new ArgumentNullException("type");
			}

			COMWrapper wrapper = new COMWrapper(comObject, type);
			return wrapper.GetTransparentProxy();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="comObject">
		/// The COM object to wrap.
		/// </param>
		/// <param name="type">
		/// The interface type to impersonate.
		/// </param>
		private COMWrapper(object comObject, Type type)
			: base(type) {
			this._COMObject = comObject;
			this._COMType = comObject.GetType();
			this._InterceptType = type;
		}

		#endregion

		#region Clean up

		/// <summary>
		/// If <see cref="Dispose"/> is not called, we need to make
		/// sure that the COM object is still cleaned up.
		/// </summary>
		~COMWrapper() {
			this.Dispose(false);
		}

		/// <summary>
		/// Cleans up the COM object.
		/// </summary>
		public void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Release the COM reference
		/// </summary>
		/// <param name="disposing">
		/// <see langword="true"/> if this was called from the
		/// <see cref="IDisposable"/> interface.
		/// </param>
		private void Dispose(bool disposing) {
			if (disposing &&  null != this._COMObject) {
				if (Marshal.IsComObject(this._COMObject)) {
					try {
						while (Marshal.ReleaseComObject(this._COMObject) > 0);
					} catch (Exception) {
						//LOG.WarnFormat("Problem releasing {0}", _COMType);
						//LOG.Warn("Error: ", ex);
					}
				}

				this._COMObject = null;
			}
		}

		#endregion

		#region Object methods

		/// <summary>
		/// Returns a string representing the wrapped object.
		/// </summary>
		/// <returns>
		/// The full name of the intercepted type.
		/// </returns>
		public override string ToString() {
			return this._InterceptType.FullName;
		}

		/// <summary>
		/// Returns the hash code of the wrapped object.
		/// </summary>
		/// <returns>
		/// The hash code of the wrapped object.
		/// </returns>
		public override int GetHashCode() {
			return this._COMObject.GetHashCode();
		}

		/// <summary>
		/// Compares this object to another.
		/// </summary>
		/// <param name="value">
		/// The value to compare to.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if the objects are equal.
		/// </returns>
		public override bool Equals(object value) {
			if (null != value && RemotingServices.IsTransparentProxy(value)) {
				COMWrapper wrapper = RemotingServices.GetRealProxy(value) as COMWrapper;
				if (null != wrapper) {
					return this._COMObject == wrapper._COMObject;
				}
			}

			return base.Equals(value);
		}

		/// <summary>
		/// Returns the base type for a reference type.
		/// </summary>
		/// <param name="byRefType">
		/// The reference type.
		/// </param>
		/// <returns>
		/// The base value type.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="byRefType"/> is <see langword="null"/>.
		/// </exception>
		private static Type GetByValType(Type byRefType) {
			if (null == byRefType) {
				throw new ArgumentNullException("byRefType");
			}

			if (byRefType.IsByRef) {
				string name = byRefType.FullName;
				name = name.Substring(0, name.Length - 1);
				byRefType = byRefType.Assembly.GetType(name, true);
			}

			return byRefType;
		}

		#endregion

		/// <summary>
		/// Use this static method to cast a wrapped proxy to a new wrapper proxy of the supplied type.
		/// In English, use this to cast you base "COM" interface to a specialized interface.
		/// E.G. Outlook Item -> MailItem
		/// </summary>
		/// <typeparam name="T">the type you want to cast to</typeparam>
		/// <param name="wrapperProxy">The wrapper interface, e.g. something you got back from calling GetItem</param>
		/// <returns>A new wrapper proxy for the specified type</returns>
		public static T Cast<T>(object wrapperProxy) {
			if (wrapperProxy == null) {
				return default(T);
			}

			Type newType = typeof(T);
			COMWrapper oldWrapper = RemotingServices.GetRealProxy(wrapperProxy) as COMWrapper;
			if (oldWrapper == null) {
				throw new ArgumentException("wrapper proxy was no COMWrapper");
			}
			if (oldWrapper._InterceptType.IsAssignableFrom(newType)) {
				COMWrapper newWrapper = new COMWrapper(oldWrapper._COMObject, newType);
				return (T)newWrapper.GetTransparentProxy();
			}
			throw new InvalidCastException(string.Format("{0} is not assignable from {1}", oldWrapper._InterceptType, newType));
		}

		/// <summary>
		/// Returns the "com" type of the wrapperproxy, making it possible to perform reflection on it.
		/// </summary>
		/// <param name="wrapperProxy">wrapperProxy to get the type from</param>
		/// <returns>Type</returns>
		public static Type GetUnderlyingTypeForWrapper(object wrapperProxy) {
			Type returnType = null;
			COMWrapper wrapper = RemotingServices.GetRealProxy(wrapperProxy) as COMWrapper;
			if (wrapper != null) {
				IDispatch dispatch = wrapper._COMObject as IDispatch;
				if (dispatch != null) {
					int result = dispatch.GetTypeInfo(0, 0, out returnType);
					if (result != 0) {
						//LOG.DebugFormat("GetTypeInfo : 0x{0} ({1})", result.ToString("X"), result);
					}
				}
			}
			return returnType;
		}

		/// <summary>
		/// Return the Type of a IDispatch
		/// </summary>
		/// <param name="dispatch">IDispatch to get the type object for</param>
		/// <returns>Type of the IDispatch</returns>
		public static Type GetUnderlyingType(IDispatch dispatch) {
			Type returnType = null;
			if (dispatch != null) {
				int result = dispatch.GetTypeInfo(0, 0, out returnType);
				if (result != 0) {
					//LOG.DebugFormat("GetTypeInfo : 0x{0} ({1})", result.ToString("X"), result);
				}
			}
			return returnType;
		}

		/// <summary>
		/// Dump the Type-Information for the Type to the log, this uses reflection
		/// </summary>
		/// <param name="type">Type to inspect</param>
		public static void DumpTypeInfo(Type type) {
			//LOG.InfoFormat("Type information for Type with name: {0}", type.Name);
			try {
				foreach (MemberInfo memberInfo in type.GetMembers()) {
					//LOG.InfoFormat("Member: {0};", memberInfo.ToString());
				}
			} catch (Exception) {
				//LOG.Error(memberException);
			}
			try {
				foreach (PropertyInfo propertyInfo in type.GetProperties()) {
					//LOG.InfoFormat("Property: {0};", propertyInfo.ToString());
				}
			} catch (Exception) {
				//LOG.Error(propertyException);
			}
			try {
				foreach (FieldInfo fieldInfo in type.GetFields()) {
					//LOG.InfoFormat("Field: {0};", fieldInfo.ToString());
				}
			} catch (Exception) {
				//LOG.Error(fieldException);
			}
			//LOG.InfoFormat("Type information end.");
		}

		/// <summary>
		/// Intercept method calls 
		/// </summary>
		/// <param name="myMessage">
		/// Contains information about the method being called
		/// </param>
		/// <returns>
		/// A <see cref="ReturnMessage"/>.
		/// </returns>
		public override IMessage Invoke(IMessage myMessage) {
			IMethodCallMessage callMessage = myMessage as IMethodCallMessage;
			if (null == callMessage) {
				//LOG.DebugFormat("Message type not implemented: {0}", myMessage.GetType().ToString());
				return null;
			}

			MethodInfo method = callMessage.MethodBase as MethodInfo;
			if (null == method) {
				//LOG.DebugFormat("Unrecognized Invoke call: {0}", callMessage.MethodBase.ToString());
				return null;
			}

			object returnValue = null;
			object[] outArgs = null;
			int outArgsCount = 0;

			string methodName = method.Name;
			Type returnType = method.ReturnType;
			BindingFlags flags = BindingFlags.InvokeMethod;
			int argCount = callMessage.ArgCount;

			object invokeObject;
			Type invokeType;
			Type byValType;

			object[] args;
			object arg;
			COMWrapper[] originalArgs;
			COMWrapper wrapper;

			ParameterModifier[] argModifiers = null;
			ParameterInfo[] parameters = null;
			ParameterInfo parameter;

			if ("Dispose" == methodName && 0 == argCount && typeof(void) == returnType) {
				this.Dispose();
			} else if ("ToString" == methodName && 0 == argCount && typeof(string) == returnType) {
				returnValue = this.ToString();
			} else if ("GetType" == methodName && 0 == argCount && typeof(System.Type) == returnType) {
				returnValue = this._InterceptType;
			} else if ("GetHashCode" == methodName && 0 == argCount && typeof(int) == returnType) {
				returnValue = this.GetHashCode();
			} else if ("Equals" == methodName && 1 == argCount && typeof(bool) == returnType) {
				returnValue = this.Equals(callMessage.Args[0]);
			} else if (1 == argCount && typeof(void) == returnType && (methodName.StartsWith("add_") || methodName.StartsWith("remove_"))) {
				bool removeHandler = methodName.StartsWith("remove_");
				methodName = methodName.Substring(removeHandler ? 7 : 4);

				Delegate handler = callMessage.InArgs[0] as Delegate;
				if (null == handler) {
					return new ReturnMessage(new ArgumentNullException("handler"), callMessage);
				}
			} else {
				invokeObject = this._COMObject;
				invokeType = this._COMType;

				if (methodName.StartsWith("get_")) {
					// Property Get
					methodName = methodName.Substring(4);
					flags = BindingFlags.GetProperty;
					args = callMessage.InArgs;
				} else if (methodName.StartsWith("set_")) {
					// Property Set
					methodName = methodName.Substring(4);
					flags = BindingFlags.SetProperty;
					args = callMessage.InArgs;
				} else {
					args = callMessage.Args;
					if (null != args && 0 != args.Length) {
						// Modifiers for ref / out parameters
						argModifiers = new ParameterModifier[1];
						argModifiers[0] = new ParameterModifier(args.Length);

						parameters = method.GetParameters();
						for (int i = 0; i < parameters.Length; i++) {
							parameter = parameters[i];
							if (parameter.IsOut || parameter.ParameterType.IsByRef) {
								argModifiers[0][i] = true;
								outArgsCount++;
							}
						}

						if (0 == outArgsCount) {
							argModifiers = null;
						}
					}
				}

				// Un-wrap wrapped COM objects before passing to the method
				if (null == args || 0 == args.Length) {
					originalArgs = null;
				} else {
					originalArgs = new COMWrapper[args.Length];
					for (int i = 0; i < args.Length; i++) {
						if (null != args[i] && RemotingServices.IsTransparentProxy(args[i])) {
							wrapper = RemotingServices.GetRealProxy(args[i]) as COMWrapper;
							if (null != wrapper) {
								originalArgs[i] = wrapper;
								args[i] = wrapper._COMObject;
							}
						} else if (0 != outArgsCount && argModifiers[0][i]) {
							byValType = GetByValType(parameters[i].ParameterType);
							if (byValType.IsInterface) {
								// If we're passing a COM object by reference, and
								// the parameter is null, we need to pass a
								// DispatchWrapper to avoid a type mismatch exception.
								if (null == args[i]) {
									args[i] = new DispatchWrapper(null);
								}
							} else if (typeof(Decimal) == byValType) {
								// If we're passing a decimal value by reference,
								// we need to pass a CurrencyWrapper to avoid a 
								// type mismatch exception.
								// http://support.microsoft.com/?kbid=837378
								args[i] = new CurrencyWrapper(args[i]);
							}
						}
					}
				}

				try {
					returnValue = invokeType.InvokeMember(methodName, flags, null, invokeObject, args, argModifiers, null, null);
				} catch (Exception ex) {
					return new ReturnMessage(ex, callMessage);
				}

				// Handle enum and interface return types
				if (null != returnValue) {
					if (returnType.IsInterface) {
						// Wrap the returned value in an intercepting COM wrapper
						if (Marshal.IsComObject(returnValue)) {
							returnValue = COMWrapper.Wrap(returnValue, returnType);
						}
					} else if (returnType.IsEnum) {
						// Convert to proper Enum type
						returnValue = Enum.Parse(returnType, returnValue.ToString());
					}
				}

				// Handle out args
				if (0 != outArgsCount) {
					outArgs = new object[args.Length];
					for (int i = 0; i < parameters.Length; i++) {
						if (!argModifiers[0][i]) {
							continue;
						}

						arg = args[i];
						if (null == arg) {
							continue;
						}

						parameter = parameters[i];
						wrapper = null;

						byValType = GetByValType(parameter.ParameterType);
						if (typeof(Decimal) == byValType) {
							if (arg is CurrencyWrapper) {
								arg = ((CurrencyWrapper)arg).WrappedObject;
							}
						} else if (byValType.IsEnum) {
							arg = Enum.Parse(byValType, arg.ToString());
						} else if (byValType.IsInterface) {
							if (Marshal.IsComObject(arg)) {
								wrapper = originalArgs[i];
								if (null != wrapper && wrapper._COMObject != arg) {
									wrapper.Dispose();
									wrapper = null;
								}

								if (null == wrapper) {
									wrapper = new COMWrapper(arg, byValType);
								}
								arg = wrapper.GetTransparentProxy();
							}
						}
						outArgs[i] = arg;
					}
				}
			}

			return new ReturnMessage(returnValue, outArgs, outArgsCount, callMessage.LogicalCallContext, callMessage);
		}

		/// <summary>
		/// Implementation for the interface IRemotingTypeInfo
		/// This makes it possible to cast the COMWrapper
		/// </summary>
		/// <param name="toType">Type to cast to</param>
		/// <param name="o">object to cast</param>
		/// <returns></returns>
		public bool CanCastTo(Type toType, object o) {
			bool returnValue = _InterceptType.IsAssignableFrom(toType);
			return returnValue;
		}

		/// <summary>
		/// Implementation for the interface IRemotingTypeInfo
		/// </summary>
		public string TypeName {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}
	}
}
