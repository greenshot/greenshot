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

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Windows.Forms;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Interop
{
    /// <summary>
    /// Wraps a late-bound COM server.
    /// </summary>
    public sealed class COMWrapper : RealProxy, IDisposable, IRemotingTypeInfo
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(COMWrapper));
        public const int RPC_E_CALL_REJECTED = unchecked((int) 0x80010001);
        public const int RPC_E_FAIL = unchecked((int) 0x80004005);

        /// <summary>
        /// Holds reference to the actual COM object which is wrapped by this proxy
        /// </summary>
        private readonly object _comObject;

        /// <summary>
        /// Type of the COM object, set on constructor after getting the COM reference
        /// </summary>
        private readonly Type _comType;

        /// <summary>
        /// The type of which method calls are intercepted and executed on the COM object.
        /// </summary>
        private readonly Type _interceptType;

        /// <summary>
        /// The humanly readable target name
        /// </summary>
        private readonly string _targetName;

        /// <summary>
        /// A simple create instance, doesn't create a wrapper!!
        /// </summary>
        /// <returns>T</returns>
        public static T CreateInstance<T>()
        {
            Type type = typeof(T);
            if (null == type)
            {
                throw new ArgumentNullException(nameof(T));
            }

            if (!type.IsInterface)
            {
                throw new ArgumentException("The specified type must be an interface.", nameof(T));
            }

            ComProgIdAttribute progIdAttribute = ComProgIdAttribute.GetAttribute(type);
            if (string.IsNullOrEmpty(progIdAttribute?.Value))
            {
                throw new ArgumentException("The specified type must define a ComProgId attribute.", nameof(T));
            }

            string progId = progIdAttribute.Value;
            Type comType = null;
            if (progId.StartsWith("clsid:"))
            {
                Guid guid = new Guid(progId.Substring(6));
                try
                {
                    comType = Type.GetTypeFromCLSID(guid);
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Error {1} type for {0}", progId, ex.Message);
                }
            }
            else
            {
                try
                {
                    comType = Type.GetTypeFromProgID(progId, true);
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Error {1} type for {0}", progId, ex.Message);
                }
            }

            object comObject = null;
            if (comType != null)
            {
                try
                {
                    comObject = Activator.CreateInstance(comType);
                    if (comObject != null)
                    {
                        Log.DebugFormat("Created new instance of {0} object.", progId);
                    }
                }
                catch (Exception e)
                {
                    Log.WarnFormat("Error {1} creating object for {0}", progId, e.Message);
                    throw;
                }
            }

            if (comObject != null)
            {
                return (T) comObject;
            }

            return default;
        }

        /// <summary>
        /// Wrap an object and return the transparent proxy which intercepts all calls to the object
        /// </summary>
        /// <param name="comObject">An object to intercept</param>
        /// <param name="type">Interface which defines the method and properties to intercept</param>
        /// <param name="targetName"></param>
        /// <returns>Transparent proxy to the real proxy for the object</returns>
        private static object Wrap(object comObject, Type type, string targetName)
        {
            if (null == comObject)
            {
                throw new ArgumentNullException(nameof(comObject));
            }

            if (null == type)
            {
                throw new ArgumentNullException(nameof(type));
            }

            COMWrapper wrapper = new COMWrapper(comObject, type, targetName);
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
        /// <param name="targetName"></param>
        private COMWrapper(object comObject, Type type, string targetName) : base(type)
        {
            _comObject = comObject;
            _comType = comObject.GetType();
            _interceptType = type;
            _targetName = targetName;
        }

        /// <summary>
        /// If <see cref="Dispose"/> is not called, we need to make
        /// sure that the COM object is still cleaned up.
        /// </summary>
        ~COMWrapper()
        {
            Log.DebugFormat("Finalize {0}", _interceptType);
            Dispose(false);
        }

        /// <summary>
        /// Cleans up the COM object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release the COM reference
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> if this was called from the
        /// <see cref="IDisposable"/> interface.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (null != _comObject)
            {
                Log.DebugFormat("Disposing {0}", _interceptType);
                if (Marshal.IsComObject(_comObject))
                {
                    try
                    {
                        int count;
                        do
                        {
                            count = Marshal.ReleaseComObject(_comObject);
                            Log.DebugFormat("RCW count for {0} now is {1}", _interceptType, count);
                        } while (count > 0);
                    }
                    catch (Exception ex)
                    {
                        Log.WarnFormat("Problem releasing COM object {0}", _comType);
                        Log.Warn("Error: ", ex);
                    }
                }
                else
                {
                    Log.WarnFormat("{0} is not a COM object", _comType);
                }
            }
        }

        /// <summary>
        /// Returns a string representing the wrapped object.
        /// </summary>
        /// <returns>
        /// The full name of the intercepted type.
        /// </returns>
        public override string ToString()
        {
            return _interceptType.FullName;
        }

        /// <summary>
        /// Returns the hash code of the wrapped object.
        /// </summary>
        /// <returns>
        /// The hash code of the wrapped object.
        /// </returns>
        public override int GetHashCode()
        {
            return _comObject.GetHashCode();
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
        public override bool Equals(object value)
        {
            if (null != value && RemotingServices.IsTransparentProxy(value))
            {
                COMWrapper wrapper = RemotingServices.GetRealProxy(value) as COMWrapper;
                if (null != wrapper)
                {
                    return _comObject == wrapper._comObject;
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
        private static Type GetByValType(Type byRefType)
        {
            if (null == byRefType)
            {
                throw new ArgumentNullException(nameof(byRefType));
            }

            if (byRefType.IsByRef)
            {
                string name = byRefType.FullName;
                name = name.Substring(0, name.Length - 1);
                byRefType = byRefType.Assembly.GetType(name, true);
            }

            return byRefType;
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
        public override IMessage Invoke(IMessage myMessage)
        {
            if (!(myMessage is IMethodCallMessage callMessage))
            {
                Log.DebugFormat("Message type not implemented: {0}", myMessage.GetType());
                return null;
            }

            MethodInfo method = callMessage.MethodBase as MethodInfo;
            if (null == method)
            {
                Log.DebugFormat("Unrecognized Invoke call: {0}", callMessage.MethodBase);
                return null;
            }

            object returnValue = null;
            object[] outArgs = null;
            int outArgsCount = 0;

            string methodName = method.Name;
            Type returnType = method.ReturnType;
            BindingFlags flags = BindingFlags.InvokeMethod;
            int argCount = callMessage.ArgCount;

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
                bool removeHandler = methodName.StartsWith("remove_");
                methodName = methodName.Substring(removeHandler ? 7 : 4);
                // TODO: Something is missing here
                if (!(callMessage.InArgs[0] is Delegate handler))
                {
                    return new ReturnMessage(new ArgumentNullException(nameof(handler)), callMessage);
                }
            }
            else
            {
                var invokeObject = _comObject;
                var invokeType = _comType;

                object[] args;
                ParameterInfo parameter;
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
                        for (int i = 0; i < parameters.Length; i++)
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
                Type byValType;
                COMWrapper wrapper;
                COMWrapper[] originalArgs;
                if (null == args || 0 == args.Length)
                {
                    originalArgs = null;
                }
                else
                {
                    originalArgs = new COMWrapper[args.Length];
                    for (int i = 0; i < args.Length; i++)
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
                        else if (0 != outArgsCount && argModifiers[0][i])
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
                                // https://support.microsoft.com/?kbid=837378
                                args[i] = new CurrencyWrapper(args[i]);
                            }
                        }
                    }
                }

                do
                {
                    try
                    {
                        returnValue = invokeType.InvokeMember(methodName, flags, null, invokeObject, args, argModifiers, null, null);
                        break;
                    }
                    catch (InvalidComObjectException icoEx)
                    {
                        // Should assist BUG-1616 and others
                        Log.WarnFormat("COM object {0} has been separated from its underlying RCW cannot be used. The COM object was released while it was still in use on another thread.", _interceptType.FullName);
                        return new ReturnMessage(icoEx, callMessage);
                    }
                    catch (Exception ex)
                    {
                        // Test for rejected
                        if (!(ex is COMException comEx))
                        {
                            comEx = ex.InnerException as COMException;
                        }

                        if (comEx != null && (comEx.ErrorCode == RPC_E_CALL_REJECTED || comEx.ErrorCode == RPC_E_FAIL))
                        {
                            string destinationName = _targetName;
                            // Try to find a "catchy" name for the rejecting application
                            if (destinationName != null && destinationName.Contains("."))
                            {
                                destinationName = destinationName.Substring(0, destinationName.IndexOf(".", StringComparison.Ordinal));
                            }

                            if (destinationName == null)
                            {
                                destinationName = _interceptType.FullName;
                            }

                            var form = SimpleServiceProvider.Current.GetInstance<Form>();

                            DialogResult result = MessageBox.Show(form, Language.GetFormattedString("com_rejected", destinationName), Language.GetString("com_rejected_title"),
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                            if (result == DialogResult.OK)
                            {
                                continue;
                            }
                        }

                        // Not rejected OR pressed cancel
                        return new ReturnMessage(ex, callMessage);
                    }
                } while (true);

                // Handle enum and interface return types
                if (null != returnValue)
                {
                    if (returnType.IsInterface)
                    {
                        // Wrap the returned value in an intercepting COM wrapper
                        if (Marshal.IsComObject(returnValue))
                        {
                            returnValue = Wrap(returnValue, returnType, _targetName);
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
                    outArgs = new object[args.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (!argModifiers[0][i])
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
                                wrapper = originalArgs[i];
                                if (null != wrapper && wrapper._comObject != arg)
                                {
                                    wrapper.Dispose();
                                    wrapper = null;
                                }

                                if (null == wrapper)
                                {
                                    wrapper = new COMWrapper(arg, byValType, _targetName);
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
        public bool CanCastTo(Type toType, object o)
        {
            bool returnValue = _interceptType.IsAssignableFrom(toType);
            return returnValue;
        }

        /// <summary>
        /// Implementation for the interface IRemotingTypeInfo
        /// </summary>
        public string TypeName
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}