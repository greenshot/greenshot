﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Greenshot.Base.Core;

namespace Greenshot.Helpers
{
    public enum CommandEnum
    {
        OpenFile,
        Exit,
        FirstLaunch,
        ReloadConfig
    };

    /// <summary>
    /// Code from vbAccelerator, location:
    /// https://www.vbaccelerator.com/home/NET/Code/Libraries/Windows_Messages/Simple_Interprocess_Communication/WM_COPYDATA_Demo_zip_SimpleInterprocessCommunicationsCS_CopyData_cs.asp
    /// </summary>
    [Serializable()]
    public class CopyDataTransport
    {
        public List<KeyValuePair<CommandEnum, string>> Commands { get; }

        public CopyDataTransport() => Commands = new List<KeyValuePair<CommandEnum, string>>();

        public CopyDataTransport(CommandEnum command) : this() => AddCommand(command, null);

        public CopyDataTransport(CommandEnum command, string commandData) : this() => AddCommand(command, commandData);

        public void AddCommand(CommandEnum command) => Commands.Add(new KeyValuePair<CommandEnum, string>(command, null));

        public void AddCommand(CommandEnum command, string commandData) => Commands.Add(new KeyValuePair<CommandEnum, string>(command, commandData));
    }

    public delegate void CopyDataReceivedEventHandler(object sender, CopyDataReceivedEventArgs e);

    /// <summary>
    /// A class which wraps using Windows native WM_COPYDATA
    /// message to send interprocess data between applications.
    /// This is a simple technique for interprocess data sends
    /// using Windows.  The alternative to this is to use
    /// Remoting, which requires a network card and a way
    /// to register the Remoting name of an object so it
    /// can be read by other applications.
    /// </summary>
    public class CopyData : NativeWindow, IDisposable
    {
        /// <summary>
        /// Event raised when data is received on any of the channels
        /// this class is subscribed to.
        /// </summary>
        public event CopyDataReceivedEventHandler CopyDataReceived;

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public readonly IntPtr dwData;
            public readonly int cbData;
            public readonly IntPtr lpData;
        }

        private const int WM_COPYDATA = 0x4A;
        private const int WM_DESTROY = 0x2;

        private CopyDataChannels _channels;

        /// <summary>
        /// Override for a form's Window Procedure to handle WM_COPYDATA
        /// messages sent by other instances of this class.
        /// </summary>
        /// <param name="m">The Windows Message information.</param>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_COPYDATA)
            {
                var cds = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT));
                if (cds.cbData > 0)
                {
                    byte[] data = new byte[cds.cbData];
                    Marshal.Copy(cds.lpData, data, 0, cds.cbData);
                    MemoryStream stream = new(data);
                    BinaryFormatter b = new();
                    CopyDataObjectData cdo = (CopyDataObjectData)b.Deserialize(stream);

                    if (_channels?.Contains(cdo.Channel) == true)
                    {
                        CopyDataReceivedEventArgs d = new(cdo.Channel, cdo.Data, cdo.Sent);
                        OnCopyDataReceived(d);
                        m.Result = (IntPtr)1;
                    }
                }
            }
            else if (m.Msg == WM_DESTROY)
            {
                // WM_DESTROY fires before OnHandleChanged and is
                // a better place to ensure that we've cleared 
                // everything up.
                _channels?.OnHandleChange();
                base.OnHandleChange();
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Raises the DataReceived event from this class.
        /// </summary>
        /// <param name="e">The data which has been received.</param>
        protected void OnCopyDataReceived(CopyDataReceivedEventArgs e) => CopyDataReceived?.Invoke(this, e);

        /// <summary>
        /// If the form's handle changes, the properties associated
        /// with the window need to be cleared up. This override ensures
        /// that it is done.  Note that the CopyData class will then
        /// stop responding to events and it should be recreated once
        /// the new handle has been assigned.
        /// </summary>
        protected override void OnHandleChange()
        {
            // need to clear up everything we had set.
            _channels?.OnHandleChange();
            base.OnHandleChange();
        }

        /// <summary>
        /// Gets the collection of channels.
        /// </summary>
        public CopyDataChannels Channels => _channels;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Clears up any resources associated with this object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _channels == null) return;

            _channels.Clear();
            _channels = null;
        }

        /// <summary>
        /// Constructs a new instance of the CopyData class
        /// </summary>
        public CopyData() => _channels = new CopyDataChannels(this);

        /// <summary>
        /// Finalises a CopyData class which has not been disposed.
        /// There may be a minor resource leak if this class is finalised
        /// after the form it is associated with.
        /// </summary>
        ~CopyData()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Contains data and other information associated with data
    /// which has been sent from another application.
    /// </summary>
    public class CopyDataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the channel name that this data was sent on.
        /// </summary>
        public string ChannelName { get; }

        /// <summary>
        /// Gets the data object which was sent.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Gets the date and time which at the data was sent
        /// by the sending application.
        /// </summary>
        public DateTime Sent { get; }

        /// <summary>
        /// Gets the date and time which this data item as
        /// received.
        /// </summary>
        public DateTime Received { get; }

        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="channelName">The channel that the data was received from</param>
        /// <param name="data">The data which was sent</param>
        /// <param name="sent">The date and time the data was sent</param>
        internal CopyDataReceivedEventArgs(string channelName, object data, DateTime sent)
        {
            ChannelName = channelName;
            Data = data;
            Sent = sent;
            Received = DateTime.Now;
        }
    }

    /// <summary>
    /// A strongly-typed collection of channels associated with the CopyData
    /// class.
    /// </summary>
    public class CopyDataChannels : DictionaryBase
    {
        private readonly NativeWindow _owner;

        /// <summary>
        /// Returns an enumerator for each of the CopyDataChannel objects
        /// within this collection.
        /// </summary>
        /// <returns>An enumerator for each of the CopyDataChannel objects
        /// within this collection.</returns>
        public new IEnumerator GetEnumerator() => Dictionary.Values.GetEnumerator();

        /// <summary>
        /// Returns the CopyDataChannel at the specified 0-based index.
        /// </summary>
        public CopyDataChannel this[int index]
        {
            get
            {
                CopyDataChannel ret = null;
                int i = 0;
                foreach (CopyDataChannel cdc in Dictionary.Values)
                {
                    i++;
                    if (i != index) continue;

                    ret = cdc;
                    break;
                }

                return ret;
            }
        }

        /// <summary>
        /// Returns the CopyDataChannel for the specified channelName
        /// </summary>
        public CopyDataChannel this[string channelName] => (CopyDataChannel)Dictionary[channelName];

        /// <summary>
        /// Adds a new channel on which this application can send and
        /// receive messages.
        /// </summary>
        public void Add(string channelName)
        {
            CopyDataChannel cdc = new(_owner, channelName);
            Dictionary.Add(channelName, cdc);
        }

        /// <summary>
        /// Removes an existing channel.
        /// </summary>
        /// <param name="channelName">The channel to remove</param>
        public void Remove(string channelName) => Dictionary.Remove(channelName);

        /// <summary>
        /// Gets/sets whether this channel contains a CopyDataChannel
        /// for the specified channelName.
        /// </summary>
        public bool Contains(string channelName) => Dictionary.Contains(channelName);

        /// <summary>
        /// Ensures the resources associated with a CopyDataChannel
        /// object collected by this class are cleared up.
        /// </summary>
        protected override void OnClear()
        {
            foreach (CopyDataChannel cdc in Dictionary.Values)
            {
                cdc.Dispose();
            }

            base.OnClear();
        }

        /// <summary>
        /// Ensures any resoures associated with the CopyDataChannel object
        /// which has been removed are cleared up.
        /// </summary>
        /// <param name="key">The channelName</param>
        /// <param name="data">The CopyDataChannel object which has
        /// just been removed</param>
        protected override void OnRemoveComplete(object key, object data)
        {
            ((CopyDataChannel)data).Dispose();
            OnRemove(key, data);
        }

        /// <summary>
        /// If the form's handle changes, the properties associated
        /// with the window need to be cleared up. This override ensures
        /// that it is done.  Note that the CopyData class will then
        /// stop responding to events and it should be recreated once
        /// the new handle has been assigned.
        /// </summary>
        public void OnHandleChange()
        {
            foreach (CopyDataChannel cdc in Dictionary.Values)
            {
                cdc.OnHandleChange();
            }
        }

        /// <summary>
        /// Constructs a new instance of the CopyDataChannels collection.
        /// Automatically managed by the CopyData class.
        /// </summary>
        /// <param name="owner">The NativeWindow this collection
        /// will be associated with</param>
        internal CopyDataChannels(NativeWindow owner) => _owner = owner;
    }

    /// <summary>
    /// A channel on which messages can be sent.
    /// </summary>
    public class CopyDataChannel : IDisposable
    {
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetProp(IntPtr hWnd, string lpString);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetProp(IntPtr hWnd, string lpString, IntPtr hData);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RemoveProp(IntPtr hWnd, string lpString);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        private const int WM_COPYDATA = 0x4A;

        private readonly NativeWindow _owner;
        private bool _recreateChannel;

        /// <summary>
        /// Gets the name associated with this channel.
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// Sends the specified object on this channel to any other
        /// applications which are listening.  The object must have the
        /// SerializableAttribute set, or must implement ISerializable.
        /// </summary>
        /// <param name="obj">The object to send</param>
        /// <returns>The number of recipients</returns>
        public int Send(object obj)
        {
            int recipients = 0;

            if (_recreateChannel)
            {
                // handle has changed 
                AddChannel();
            }

            CopyDataObjectData cdo = new(obj, ChannelName);

            // Try to do a binary serialization on obj.
            // This will throw and exception if the object to
            // be passed isn't serializable.
            BinaryFormatter b = new();
            MemoryStream stream = new();
            b.Serialize(stream, cdo);
            stream.Flush();

            // Now move the data into a pointer so we can send
            // it using WM_COPYDATA:
            // Get the length of the data:
            int dataSize = (int)stream.Length;
            if (dataSize > 0)
            {
                // This isn't very efficient if your data is very large.
                // First we copy to a byte array, then copy to a CoTask 
                // Mem object... And when we use WM_COPYDATA windows will
                // make yet another copy!  But if you're talking about 4K
                // or less of data then it doesn't really matter.
                byte[] data = new byte[dataSize];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(data, 0, dataSize);
                IntPtr ptrData = Marshal.AllocCoTaskMem(dataSize);
                Marshal.Copy(data, 0, ptrData, dataSize);

                // Send the data to each window identified on
                // the channel:
                foreach (WindowDetails window in WindowDetails.GetAllWindows())
                {
                    if (!window.Handle.Equals(_owner.Handle) && GetProp(window.Handle, ChannelName) != IntPtr.Zero)
                    {
                        COPYDATASTRUCT cds = new()
                        {
                            cbData = dataSize,
                            dwData = IntPtr.Zero,
                            lpData = ptrData
                        };
                        SendMessage(window.Handle, WM_COPYDATA, _owner.Handle, ref cds);
                        recipients += Marshal.GetLastWin32Error() == 0 ? 1 : 0;
                    }
                }

                // Clear up the data:
                Marshal.FreeCoTaskMem(ptrData);
            }

            stream.Close();

            return recipients;
        }

        private void AddChannel() =>
            // Tag this window with property "channelName"
            SetProp(_owner.Handle, ChannelName, _owner.Handle);

        private void RemoveChannel() =>
            // Remove the "channelName" property from this window
            RemoveProp(_owner.Handle, ChannelName);

        /// <summary>
        /// If the form's handle changes, the properties associated
        /// with the window need to be cleared up. This method ensures
        /// that it is done.  Note that the CopyData class will then
        /// stop responding to events and it should be recreated once
        /// the new handle has been assigned.
        /// </summary>
        public void OnHandleChange()
        {
            RemoveChannel();
            _recreateChannel = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Clears up any resources associated with this channel.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (ChannelName.Length > 0)
            {
                RemoveChannel();
            }

            ChannelName = string.Empty;
        }

        /// <summary>
        /// Constructs a new instance of a CopyData channel.  Called
        /// automatically by the CopyDataChannels collection.
        /// </summary>
        /// <param name="owner">The owning native window</param>
        /// <param name="channelName">The name of the channel to
        /// send messages on</param>
        internal CopyDataChannel(NativeWindow owner, string channelName)
        {
            _owner = owner;
            ChannelName = channelName;
            AddChannel();
        }

        ~CopyDataChannel()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// A class which wraps the data being copied, used
    /// internally within the CopyData class objects.
    /// </summary>
    [Serializable()]
    internal class CopyDataObjectData
    {
        /// <summary>
        /// The Object to copy.  Must be Serializable.
        /// </summary>
        public object Data;

        /// <summary>
        /// The date and time this object was sent.
        /// </summary>
        public DateTime Sent;

        /// <summary>
        /// The name of the channel this object is being sent on
        /// </summary>
        public string Channel;

        /// <summary>
        /// Constructs a new instance of this object
        /// </summary>
        /// <param name="data">The data to copy</param>
        /// <param name="channel">The channel name to send on</param>
        /// <exception cref="ArgumentException">If data is not serializable.</exception>
        public CopyDataObjectData(object data, string channel)
        {
            Data = data;
            if (!data.GetType().IsSerializable)
            {
                throw new ArgumentException("Data object must be serializable.",
                    nameof(data));
            }

            Channel = channel;
            Sent = DateTime.Now;
        }
    }
}