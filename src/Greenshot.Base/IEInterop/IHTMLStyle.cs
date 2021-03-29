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

using System.Runtime.InteropServices;

namespace Greenshot.Base.IEInterop
{
    [ComImport, Guid("3050F25E-98B5-11CF-BB82-00AA00BDCE0B"),
     TypeLibType(TypeLibTypeFlags.FDual),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLStyle
    {
        /// <summary><para><c>setAttribute</c> method of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>setAttribute</c> method was the following:  <c>HRESULT setAttribute (BSTR strAttributeName, VARIANT AttributeValue, [optional, defaultvalue(1)] long lFlags)</c>;</para></remarks>
        // IDL: HRESULT setAttribute (BSTR strAttributeName, VARIANT AttributeValue, [optional, defaultvalue(1)] long lFlags);
        // VB6: Sub setAttribute (ByVal strAttributeName As String, ByVal AttributeValue As Any, [ByVal lFlags As Long = 1])
        [DispId(-2147417611)]
        void setAttribute([MarshalAs(UnmanagedType.BStr)] string strAttributeName, object AttributeValue, int lFlags);

        /// <summary><para><c>getAttribute</c> method of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>getAttribute</c> method was the following:  <c>HRESULT getAttribute (BSTR strAttributeName, [optional, defaultvalue(0)] long lFlags, [out, retval] VARIANT* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT getAttribute (BSTR strAttributeName, [optional, defaultvalue(0)] long lFlags, [out, retval] VARIANT* ReturnValue);
        // VB6: Function getAttribute (ByVal strAttributeName As String, [ByVal lFlags As Long = 0]) As Any
        [DispId(-2147417610)]
        object getAttribute([MarshalAs(UnmanagedType.BStr)] string strAttributeName, int lFlags);

        /// <summary><para><c>removeAttribute</c> method of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>removeAttribute</c> method was the following:  <c>HRESULT removeAttribute (BSTR strAttributeName, [optional, defaultvalue(1)] long lFlags, [out, retval] VARIANT_BOOL* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT removeAttribute (BSTR strAttributeName, [optional, defaultvalue(1)] long lFlags, [out, retval] VARIANT_BOOL* ReturnValue);
        // VB6: Function removeAttribute (ByVal strAttributeName As String, [ByVal lFlags As Long = 1]) As Boolean
        [DispId(-2147417609)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool removeAttribute([MarshalAs(UnmanagedType.BStr)] string strAttributeName, int lFlags);

        /// <summary><para><c>toString</c> method of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>toString</c> method was the following:  <c>HRESULT toString ([out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT toString ([out, retval] BSTR* ReturnValue);
        // VB6: Function toString As String
        [DispId(-2147414104)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string toString();

        /// <summary><para><c>background</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>background</c> property was the following:  <c>BSTR background</c>;</para></remarks>
        // IDL: BSTR background;
        // VB6: background As String
        string background
        {
            // IDL: HRESULT background ([out, retval] BSTR* ReturnValue);
            // VB6: Function background As String
            [DispId(-2147413080)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT background (BSTR value);
            // VB6: Sub background (ByVal value As String)
            [DispId(-2147413080)] set;
        }

        /// <summary><para><c>backgroundAttachment</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>backgroundAttachment</c> property was the following:  <c>BSTR backgroundAttachment</c>;</para></remarks>
        // IDL: BSTR backgroundAttachment;
        // VB6: backgroundAttachment As String
        string backgroundAttachment
        {
            // IDL: HRESULT backgroundAttachment ([out, retval] BSTR* ReturnValue);
            // VB6: Function backgroundAttachment As String
            [DispId(-2147413067)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT backgroundAttachment (BSTR value);
            // VB6: Sub backgroundAttachment (ByVal value As String)
            [DispId(-2147413067)] set;
        }

        /// <summary><para><c>backgroundColor</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>backgroundColor</c> property was the following:  <c>VARIANT backgroundColor</c>;</para></remarks>
        // IDL: VARIANT backgroundColor;
        // VB6: backgroundColor As Any
        object backgroundColor
        {
            // IDL: HRESULT backgroundColor ([out, retval] VARIANT* ReturnValue);
            // VB6: Function backgroundColor As Any
            [DispId(-501)] get;
            // IDL: HRESULT backgroundColor (VARIANT value);
            // VB6: Sub backgroundColor (ByVal value As Any)
            [DispId(-501)] set;
        }

        /// <summary><para><c>backgroundImage</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>backgroundImage</c> property was the following:  <c>BSTR backgroundImage</c>;</para></remarks>
        // IDL: BSTR backgroundImage;
        // VB6: backgroundImage As String
        string backgroundImage
        {
            // IDL: HRESULT backgroundImage ([out, retval] BSTR* ReturnValue);
            // VB6: Function backgroundImage As String
            [DispId(-2147413111)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT backgroundImage (BSTR value);
            // VB6: Sub backgroundImage (ByVal value As String)
            [DispId(-2147413111)] set;
        }

        /// <summary><para><c>backgroundPosition</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>backgroundPosition</c> property was the following:  <c>BSTR backgroundPosition</c>;</para></remarks>
        // IDL: BSTR backgroundPosition;
        // VB6: backgroundPosition As String
        string backgroundPosition
        {
            // IDL: HRESULT backgroundPosition ([out, retval] BSTR* ReturnValue);
            // VB6: Function backgroundPosition As String
            [DispId(-2147413066)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT backgroundPosition (BSTR value);
            // VB6: Sub backgroundPosition (ByVal value As String)
            [DispId(-2147413066)] set;
        }

        /// <summary><para><c>backgroundPositionX</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>backgroundPositionX</c> property was the following:  <c>VARIANT backgroundPositionX</c>;</para></remarks>
        // IDL: VARIANT backgroundPositionX;
        // VB6: backgroundPositionX As Any
        object backgroundPositionX
        {
            // IDL: HRESULT backgroundPositionX ([out, retval] VARIANT* ReturnValue);
            // VB6: Function backgroundPositionX As Any
            [DispId(-2147413079)] get;
            // IDL: HRESULT backgroundPositionX (VARIANT value);
            // VB6: Sub backgroundPositionX (ByVal value As Any)
            [DispId(-2147413079)] set;
        }

        /// <summary><para><c>backgroundPositionY</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>backgroundPositionY</c> property was the following:  <c>VARIANT backgroundPositionY</c>;</para></remarks>
        // IDL: VARIANT backgroundPositionY;
        // VB6: backgroundPositionY As Any
        object backgroundPositionY
        {
            // IDL: HRESULT backgroundPositionY ([out, retval] VARIANT* ReturnValue);
            // VB6: Function backgroundPositionY As Any
            [DispId(-2147413078)] get;
            // IDL: HRESULT backgroundPositionY (VARIANT value);
            // VB6: Sub backgroundPositionY (ByVal value As Any)
            [DispId(-2147413078)] set;
        }

        /// <summary><para><c>backgroundRepeat</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>backgroundRepeat</c> property was the following:  <c>BSTR backgroundRepeat</c>;</para></remarks>
        // IDL: BSTR backgroundRepeat;
        // VB6: backgroundRepeat As String
        string backgroundRepeat
        {
            // IDL: HRESULT backgroundRepeat ([out, retval] BSTR* ReturnValue);
            // VB6: Function backgroundRepeat As String
            [DispId(-2147413068)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT backgroundRepeat (BSTR value);
            // VB6: Sub backgroundRepeat (ByVal value As String)
            [DispId(-2147413068)] set;
        }

        /// <summary><para><c>border</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>border</c> property was the following:  <c>BSTR border</c>;</para></remarks>
        // IDL: BSTR border;
        // VB6: border As String
        string border
        {
            // IDL: HRESULT border ([out, retval] BSTR* ReturnValue);
            // VB6: Function border As String
            [DispId(-2147413063)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT border (BSTR value);
            // VB6: Sub border (ByVal value As String)
            [DispId(-2147413063)] set;
        }

        /// <summary><para><c>borderBottom</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderBottom</c> property was the following:  <c>BSTR borderBottom</c>;</para></remarks>
        // IDL: BSTR borderBottom;
        // VB6: borderBottom As String
        string borderBottom
        {
            // IDL: HRESULT borderBottom ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderBottom As String
            [DispId(-2147413060)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderBottom (BSTR value);
            // VB6: Sub borderBottom (ByVal value As String)
            [DispId(-2147413060)] set;
        }

        /// <summary><para><c>borderBottomColor</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderBottomColor</c> property was the following:  <c>VARIANT borderBottomColor</c>;</para></remarks>
        // IDL: VARIANT borderBottomColor;
        // VB6: borderBottomColor As Any
        object borderBottomColor
        {
            // IDL: HRESULT borderBottomColor ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderBottomColor As Any
            [DispId(-2147413055)] get;
            // IDL: HRESULT borderBottomColor (VARIANT value);
            // VB6: Sub borderBottomColor (ByVal value As Any)
            [DispId(-2147413055)] set;
        }

        /// <summary><para><c>borderBottomStyle</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderBottomStyle</c> property was the following:  <c>BSTR borderBottomStyle</c>;</para></remarks>
        // IDL: BSTR borderBottomStyle;
        // VB6: borderBottomStyle As String
        string borderBottomStyle
        {
            // IDL: HRESULT borderBottomStyle ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderBottomStyle As String
            [DispId(-2147413045)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderBottomStyle (BSTR value);
            // VB6: Sub borderBottomStyle (ByVal value As String)
            [DispId(-2147413045)] set;
        }

        /// <summary><para><c>borderBottomWidth</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderBottomWidth</c> property was the following:  <c>VARIANT borderBottomWidth</c>;</para></remarks>
        // IDL: VARIANT borderBottomWidth;
        // VB6: borderBottomWidth As Any
        object borderBottomWidth
        {
            // IDL: HRESULT borderBottomWidth ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderBottomWidth As Any
            [DispId(-2147413050)] get;
            // IDL: HRESULT borderBottomWidth (VARIANT value);
            // VB6: Sub borderBottomWidth (ByVal value As Any)
            [DispId(-2147413050)] set;
        }

        /// <summary><para><c>borderColor</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderColor</c> property was the following:  <c>BSTR borderColor</c>;</para></remarks>
        // IDL: BSTR borderColor;
        // VB6: borderColor As String
        string borderColor
        {
            // IDL: HRESULT borderColor ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderColor As String
            [DispId(-2147413058)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderColor (BSTR value);
            // VB6: Sub borderColor (ByVal value As String)
            [DispId(-2147413058)] set;
        }

        /// <summary><para><c>borderLeft</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderLeft</c> property was the following:  <c>BSTR borderLeft</c>;</para></remarks>
        // IDL: BSTR borderLeft;
        // VB6: borderLeft As String
        string borderLeft
        {
            // IDL: HRESULT borderLeft ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderLeft As String
            [DispId(-2147413059)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderLeft (BSTR value);
            // VB6: Sub borderLeft (ByVal value As String)
            [DispId(-2147413059)] set;
        }

        /// <summary><para><c>borderLeftColor</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderLeftColor</c> property was the following:  <c>VARIANT borderLeftColor</c>;</para></remarks>
        // IDL: VARIANT borderLeftColor;
        // VB6: borderLeftColor As Any
        object borderLeftColor
        {
            // IDL: HRESULT borderLeftColor ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderLeftColor As Any
            [DispId(-2147413054)] get;
            // IDL: HRESULT borderLeftColor (VARIANT value);
            // VB6: Sub borderLeftColor (ByVal value As Any)
            [DispId(-2147413054)] set;
        }

        /// <summary><para><c>borderLeftStyle</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderLeftStyle</c> property was the following:  <c>BSTR borderLeftStyle</c>;</para></remarks>
        // IDL: BSTR borderLeftStyle;
        // VB6: borderLeftStyle As String
        string borderLeftStyle
        {
            // IDL: HRESULT borderLeftStyle ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderLeftStyle As String
            [DispId(-2147413044)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderLeftStyle (BSTR value);
            // VB6: Sub borderLeftStyle (ByVal value As String)
            [DispId(-2147413044)] set;
        }

        /// <summary><para><c>borderLeftWidth</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderLeftWidth</c> property was the following:  <c>VARIANT borderLeftWidth</c>;</para></remarks>
        // IDL: VARIANT borderLeftWidth;
        // VB6: borderLeftWidth As Any
        object borderLeftWidth
        {
            // IDL: HRESULT borderLeftWidth ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderLeftWidth As Any
            [DispId(-2147413049)] get;
            // IDL: HRESULT borderLeftWidth (VARIANT value);
            // VB6: Sub borderLeftWidth (ByVal value As Any)
            [DispId(-2147413049)] set;
        }

        /// <summary><para><c>borderRight</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderRight</c> property was the following:  <c>BSTR borderRight</c>;</para></remarks>
        // IDL: BSTR borderRight;
        // VB6: borderRight As String
        string borderRight
        {
            // IDL: HRESULT borderRight ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderRight As String
            [DispId(-2147413061)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderRight (BSTR value);
            // VB6: Sub borderRight (ByVal value As String)
            [DispId(-2147413061)] set;
        }

        /// <summary><para><c>borderRightColor</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderRightColor</c> property was the following:  <c>VARIANT borderRightColor</c>;</para></remarks>
        // IDL: VARIANT borderRightColor;
        // VB6: borderRightColor As Any
        object borderRightColor
        {
            // IDL: HRESULT borderRightColor ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderRightColor As Any
            [DispId(-2147413056)] get;
            // IDL: HRESULT borderRightColor (VARIANT value);
            // VB6: Sub borderRightColor (ByVal value As Any)
            [DispId(-2147413056)] set;
        }

        /// <summary><para><c>borderRightStyle</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderRightStyle</c> property was the following:  <c>BSTR borderRightStyle</c>;</para></remarks>
        // IDL: BSTR borderRightStyle;
        // VB6: borderRightStyle As String
        string borderRightStyle
        {
            // IDL: HRESULT borderRightStyle ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderRightStyle As String
            [DispId(-2147413046)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderRightStyle (BSTR value);
            // VB6: Sub borderRightStyle (ByVal value As String)
            [DispId(-2147413046)] set;
        }

        /// <summary><para><c>borderRightWidth</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderRightWidth</c> property was the following:  <c>VARIANT borderRightWidth</c>;</para></remarks>
        // IDL: VARIANT borderRightWidth;
        // VB6: borderRightWidth As Any
        object borderRightWidth
        {
            // IDL: HRESULT borderRightWidth ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderRightWidth As Any
            [DispId(-2147413051)] get;
            // IDL: HRESULT borderRightWidth (VARIANT value);
            // VB6: Sub borderRightWidth (ByVal value As Any)
            [DispId(-2147413051)] set;
        }

        /// <summary><para><c>borderStyle</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderStyle</c> property was the following:  <c>BSTR borderStyle</c>;</para></remarks>
        // IDL: BSTR borderStyle;
        // VB6: borderStyle As String
        string borderStyle
        {
            // IDL: HRESULT borderStyle ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderStyle As String
            [DispId(-2147413048)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderStyle (BSTR value);
            // VB6: Sub borderStyle (ByVal value As String)
            [DispId(-2147413048)] set;
        }

        /// <summary><para><c>borderTop</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderTop</c> property was the following:  <c>BSTR borderTop</c>;</para></remarks>
        // IDL: BSTR borderTop;
        // VB6: borderTop As String
        string borderTop
        {
            // IDL: HRESULT borderTop ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderTop As String
            [DispId(-2147413062)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderTop (BSTR value);
            // VB6: Sub borderTop (ByVal value As String)
            [DispId(-2147413062)] set;
        }

        /// <summary><para><c>borderTopColor</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderTopColor</c> property was the following:  <c>VARIANT borderTopColor</c>;</para></remarks>
        // IDL: VARIANT borderTopColor;
        // VB6: borderTopColor As Any
        object borderTopColor
        {
            // IDL: HRESULT borderTopColor ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderTopColor As Any
            [DispId(-2147413057)] get;
            // IDL: HRESULT borderTopColor (VARIANT value);
            // VB6: Sub borderTopColor (ByVal value As Any)
            [DispId(-2147413057)] set;
        }

        /// <summary><para><c>borderTopStyle</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderTopStyle</c> property was the following:  <c>BSTR borderTopStyle</c>;</para></remarks>
        // IDL: BSTR borderTopStyle;
        // VB6: borderTopStyle As String
        string borderTopStyle
        {
            // IDL: HRESULT borderTopStyle ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderTopStyle As String
            [DispId(-2147413047)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderTopStyle (BSTR value);
            // VB6: Sub borderTopStyle (ByVal value As String)
            [DispId(-2147413047)] set;
        }

        /// <summary><para><c>borderTopWidth</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderTopWidth</c> property was the following:  <c>VARIANT borderTopWidth</c>;</para></remarks>
        // IDL: VARIANT borderTopWidth;
        // VB6: borderTopWidth As Any
        object borderTopWidth
        {
            // IDL: HRESULT borderTopWidth ([out, retval] VARIANT* ReturnValue);
            // VB6: Function borderTopWidth As Any
            [DispId(-2147413052)] get;
            // IDL: HRESULT borderTopWidth (VARIANT value);
            // VB6: Sub borderTopWidth (ByVal value As Any)
            [DispId(-2147413052)] set;
        }

        /// <summary><para><c>borderWidth</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>borderWidth</c> property was the following:  <c>BSTR borderWidth</c>;</para></remarks>
        // IDL: BSTR borderWidth;
        // VB6: borderWidth As String
        string borderWidth
        {
            // IDL: HRESULT borderWidth ([out, retval] BSTR* ReturnValue);
            // VB6: Function borderWidth As String
            [DispId(-2147413053)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT borderWidth (BSTR value);
            // VB6: Sub borderWidth (ByVal value As String)
            [DispId(-2147413053)] set;
        }

        /// <summary><para><c>clear</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>clear</c> property was the following:  <c>BSTR clear</c>;</para></remarks>
        // IDL: BSTR clear;
        // VB6: clear As String
        string clear
        {
            // IDL: HRESULT clear ([out, retval] BSTR* ReturnValue);
            // VB6: Function clear As String
            [DispId(-2147413096)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT clear (BSTR value);
            // VB6: Sub clear (ByVal value As String)
            [DispId(-2147413096)] set;
        }

        /// <summary><para><c>clip</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>clip</c> property was the following:  <c>BSTR clip</c>;</para></remarks>
        // IDL: BSTR clip;
        // VB6: clip As String
        string clip
        {
            // IDL: HRESULT clip ([out, retval] BSTR* ReturnValue);
            // VB6: Function clip As String
            [DispId(-2147413020)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT clip (BSTR value);
            // VB6: Sub clip (ByVal value As String)
            [DispId(-2147413020)] set;
        }

        /// <summary><para><c>color</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>color</c> property was the following:  <c>VARIANT color</c>;</para></remarks>
        // IDL: VARIANT color;
        // VB6: color As Any
        object color
        {
            // IDL: HRESULT color ([out, retval] VARIANT* ReturnValue);
            // VB6: Function color As Any
            [DispId(-2147413110)] get;
            // IDL: HRESULT color (VARIANT value);
            // VB6: Sub color (ByVal value As Any)
            [DispId(-2147413110)] set;
        }

        /// <summary><para><c>cssText</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>cssText</c> property was the following:  <c>BSTR cssText</c>;</para></remarks>
        // IDL: BSTR cssText;
        // VB6: cssText As String
        string cssText
        {
            // IDL: HRESULT cssText ([out, retval] BSTR* ReturnValue);
            // VB6: Function cssText As String
            [DispId(-2147413013)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT cssText (BSTR value);
            // VB6: Sub cssText (ByVal value As String)
            [DispId(-2147413013)] set;
        }

        /// <summary><para><c>cursor</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>cursor</c> property was the following:  <c>BSTR cursor</c>;</para></remarks>
        // IDL: BSTR cursor;
        // VB6: cursor As String
        string cursor
        {
            // IDL: HRESULT cursor ([out, retval] BSTR* ReturnValue);
            // VB6: Function cursor As String
            [DispId(-2147413010)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT cursor (BSTR value);
            // VB6: Sub cursor (ByVal value As String)
            [DispId(-2147413010)] set;
        }

        /// <summary><para><c>display</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>display</c> property was the following:  <c>BSTR display</c>;</para></remarks>
        // IDL: BSTR display;
        // VB6: display As String
        string display
        {
            // IDL: HRESULT display ([out, retval] BSTR* ReturnValue);
            // VB6: Function display As String
            [DispId(-2147413041)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT display (BSTR value);
            // VB6: Sub display (ByVal value As String)
            [DispId(-2147413041)] set;
        }

        /// <summary><para><c>filter</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>filter</c> property was the following:  <c>BSTR filter</c>;</para></remarks>
        // IDL: BSTR filter;
        // VB6: filter As String
        string filter
        {
            // IDL: HRESULT filter ([out, retval] BSTR* ReturnValue);
            // VB6: Function filter As String
            [DispId(-2147413030)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT filter (BSTR value);
            // VB6: Sub filter (ByVal value As String)
            [DispId(-2147413030)] set;
        }

        /// <summary><para><c>font</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>font</c> property was the following:  <c>BSTR font</c>;</para></remarks>
        // IDL: BSTR font;
        // VB6: font As String
        string font
        {
            // IDL: HRESULT font ([out, retval] BSTR* ReturnValue);
            // VB6: Function font As String
            [DispId(-2147413071)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT font (BSTR value);
            // VB6: Sub font (ByVal value As String)
            [DispId(-2147413071)] set;
        }

        /// <summary><para><c>fontFamily</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>fontFamily</c> property was the following:  <c>BSTR fontFamily</c>;</para></remarks>
        // IDL: BSTR fontFamily;
        // VB6: fontFamily As String
        string fontFamily
        {
            // IDL: HRESULT fontFamily ([out, retval] BSTR* ReturnValue);
            // VB6: Function fontFamily As String
            [DispId(-2147413094)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT fontFamily (BSTR value);
            // VB6: Sub fontFamily (ByVal value As String)
            [DispId(-2147413094)] set;
        }

        /// <summary><para><c>fontSize</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>fontSize</c> property was the following:  <c>VARIANT fontSize</c>;</para></remarks>
        // IDL: VARIANT fontSize;
        // VB6: fontSize As Any
        object fontSize
        {
            // IDL: HRESULT fontSize ([out, retval] VARIANT* ReturnValue);
            // VB6: Function fontSize As Any
            [DispId(-2147413093)] get;
            // IDL: HRESULT fontSize (VARIANT value);
            // VB6: Sub fontSize (ByVal value As Any)
            [DispId(-2147413093)] set;
        }

        /// <summary><para><c>fontStyle</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>fontStyle</c> property was the following:  <c>BSTR fontStyle</c>;</para></remarks>
        // IDL: BSTR fontStyle;
        // VB6: fontStyle As String
        string fontStyle
        {
            // IDL: HRESULT fontStyle ([out, retval] BSTR* ReturnValue);
            // VB6: Function fontStyle As String
            [DispId(-2147413088)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT fontStyle (BSTR value);
            // VB6: Sub fontStyle (ByVal value As String)
            [DispId(-2147413088)] set;
        }

        /// <summary><para><c>fontVariant</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>fontVariant</c> property was the following:  <c>BSTR fontVariant</c>;</para></remarks>
        // IDL: BSTR fontVariant;
        // VB6: fontVariant As String
        string fontVariant
        {
            // IDL: HRESULT fontVariant ([out, retval] BSTR* ReturnValue);
            // VB6: Function fontVariant As String
            [DispId(-2147413087)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT fontVariant (BSTR value);
            // VB6: Sub fontVariant (ByVal value As String)
            [DispId(-2147413087)] set;
        }

        /// <summary><para><c>fontWeight</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>fontWeight</c> property was the following:  <c>BSTR fontWeight</c>;</para></remarks>
        // IDL: BSTR fontWeight;
        // VB6: fontWeight As String
        string fontWeight
        {
            // IDL: HRESULT fontWeight ([out, retval] BSTR* ReturnValue);
            // VB6: Function fontWeight As String
            [DispId(-2147413085)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT fontWeight (BSTR value);
            // VB6: Sub fontWeight (ByVal value As String)
            [DispId(-2147413085)] set;
        }

        /// <summary><para><c>height</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>height</c> property was the following:  <c>VARIANT height</c>;</para></remarks>
        // IDL: VARIANT height;
        // VB6: height As Any
        object height
        {
            // IDL: HRESULT height ([out, retval] VARIANT* ReturnValue);
            // VB6: Function height As Any
            [DispId(-2147418106)] get;
            // IDL: HRESULT height (VARIANT value);
            // VB6: Sub height (ByVal value As Any)
            [DispId(-2147418106)] set;
        }

        /// <summary><para><c>left</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>left</c> property was the following:  <c>VARIANT left</c>;</para></remarks>
        // IDL: VARIANT left;
        // VB6: left As Any
        object left
        {
            // IDL: HRESULT left ([out, retval] VARIANT* ReturnValue);
            // VB6: Function left As Any
            [DispId(-2147418109)] get;
            // IDL: HRESULT left (VARIANT value);
            // VB6: Sub left (ByVal value As Any)
            [DispId(-2147418109)] set;
        }

        /// <summary><para><c>letterSpacing</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>letterSpacing</c> property was the following:  <c>VARIANT letterSpacing</c>;</para></remarks>
        // IDL: VARIANT letterSpacing;
        // VB6: letterSpacing As Any
        object letterSpacing
        {
            // IDL: HRESULT letterSpacing ([out, retval] VARIANT* ReturnValue);
            // VB6: Function letterSpacing As Any
            [DispId(-2147413104)] get;
            // IDL: HRESULT letterSpacing (VARIANT value);
            // VB6: Sub letterSpacing (ByVal value As Any)
            [DispId(-2147413104)] set;
        }

        /// <summary><para><c>lineHeight</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>lineHeight</c> property was the following:  <c>VARIANT lineHeight</c>;</para></remarks>
        // IDL: VARIANT lineHeight;
        // VB6: lineHeight As Any
        object lineHeight
        {
            // IDL: HRESULT lineHeight ([out, retval] VARIANT* ReturnValue);
            // VB6: Function lineHeight As Any
            [DispId(-2147413106)] get;
            // IDL: HRESULT lineHeight (VARIANT value);
            // VB6: Sub lineHeight (ByVal value As Any)
            [DispId(-2147413106)] set;
        }

        /// <summary><para><c>listStyle</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>listStyle</c> property was the following:  <c>BSTR listStyle</c>;</para></remarks>
        // IDL: BSTR listStyle;
        // VB6: listStyle As String
        string listStyle
        {
            // IDL: HRESULT listStyle ([out, retval] BSTR* ReturnValue);
            // VB6: Function listStyle As String
            [DispId(-2147413037)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT listStyle (BSTR value);
            // VB6: Sub listStyle (ByVal value As String)
            [DispId(-2147413037)] set;
        }

        /// <summary><para><c>listStyleImage</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>listStyleImage</c> property was the following:  <c>BSTR listStyleImage</c>;</para></remarks>
        // IDL: BSTR listStyleImage;
        // VB6: listStyleImage As String
        string listStyleImage
        {
            // IDL: HRESULT listStyleImage ([out, retval] BSTR* ReturnValue);
            // VB6: Function listStyleImage As String
            [DispId(-2147413038)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT listStyleImage (BSTR value);
            // VB6: Sub listStyleImage (ByVal value As String)
            [DispId(-2147413038)] set;
        }

        /// <summary><para><c>listStylePosition</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>listStylePosition</c> property was the following:  <c>BSTR listStylePosition</c>;</para></remarks>
        // IDL: BSTR listStylePosition;
        // VB6: listStylePosition As String
        string listStylePosition
        {
            // IDL: HRESULT listStylePosition ([out, retval] BSTR* ReturnValue);
            // VB6: Function listStylePosition As String
            [DispId(-2147413039)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT listStylePosition (BSTR value);
            // VB6: Sub listStylePosition (ByVal value As String)
            [DispId(-2147413039)] set;
        }

        /// <summary><para><c>listStyleType</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>listStyleType</c> property was the following:  <c>BSTR listStyleType</c>;</para></remarks>
        // IDL: BSTR listStyleType;
        // VB6: listStyleType As String
        string listStyleType
        {
            // IDL: HRESULT listStyleType ([out, retval] BSTR* ReturnValue);
            // VB6: Function listStyleType As String
            [DispId(-2147413040)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT listStyleType (BSTR value);
            // VB6: Sub listStyleType (ByVal value As String)
            [DispId(-2147413040)] set;
        }

        /// <summary><para><c>margin</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>margin</c> property was the following:  <c>BSTR margin</c>;</para></remarks>
        // IDL: BSTR margin;
        // VB6: margin As String
        string margin
        {
            // IDL: HRESULT margin ([out, retval] BSTR* ReturnValue);
            // VB6: Function margin As String
            [DispId(-2147413076)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT margin (BSTR value);
            // VB6: Sub margin (ByVal value As String)
            [DispId(-2147413076)] set;
        }

        /// <summary><para><c>marginBottom</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>marginBottom</c> property was the following:  <c>VARIANT marginBottom</c>;</para></remarks>
        // IDL: VARIANT marginBottom;
        // VB6: marginBottom As Any
        object marginBottom
        {
            // IDL: HRESULT marginBottom ([out, retval] VARIANT* ReturnValue);
            // VB6: Function marginBottom As Any
            [DispId(-2147413073)] get;
            // IDL: HRESULT marginBottom (VARIANT value);
            // VB6: Sub marginBottom (ByVal value As Any)
            [DispId(-2147413073)] set;
        }

        /// <summary><para><c>marginLeft</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>marginLeft</c> property was the following:  <c>VARIANT marginLeft</c>;</para></remarks>
        // IDL: VARIANT marginLeft;
        // VB6: marginLeft As Any
        object marginLeft
        {
            // IDL: HRESULT marginLeft ([out, retval] VARIANT* ReturnValue);
            // VB6: Function marginLeft As Any
            [DispId(-2147413072)] get;
            // IDL: HRESULT marginLeft (VARIANT value);
            // VB6: Sub marginLeft (ByVal value As Any)
            [DispId(-2147413072)] set;
        }

        /// <summary><para><c>marginRight</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>marginRight</c> property was the following:  <c>VARIANT marginRight</c>;</para></remarks>
        // IDL: VARIANT marginRight;
        // VB6: marginRight As Any
        object marginRight
        {
            // IDL: HRESULT marginRight ([out, retval] VARIANT* ReturnValue);
            // VB6: Function marginRight As Any
            [DispId(-2147413074)] get;
            // IDL: HRESULT marginRight (VARIANT value);
            // VB6: Sub marginRight (ByVal value As Any)
            [DispId(-2147413074)] set;
        }

        /// <summary><para><c>marginTop</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>marginTop</c> property was the following:  <c>VARIANT marginTop</c>;</para></remarks>
        // IDL: VARIANT marginTop;
        // VB6: marginTop As Any
        object marginTop
        {
            // IDL: HRESULT marginTop ([out, retval] VARIANT* ReturnValue);
            // VB6: Function marginTop As Any
            [DispId(-2147413075)] get;
            // IDL: HRESULT marginTop (VARIANT value);
            // VB6: Sub marginTop (ByVal value As Any)
            [DispId(-2147413075)] set;
        }

        /// <summary><para><c>overflow</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>overflow</c> property was the following:  <c>BSTR overflow</c>;</para></remarks>
        // IDL: BSTR overflow;
        // VB6: overflow As String
        string overflow
        {
            // IDL: HRESULT overflow ([out, retval] BSTR* ReturnValue);
            // VB6: Function overflow As String
            [DispId(-2147413102)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT overflow (BSTR value);
            // VB6: Sub overflow (ByVal value As String)
            [DispId(-2147413102)] set;
        }

        /// <summary><para><c>padding</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>padding</c> property was the following:  <c>BSTR padding</c>;</para></remarks>
        // IDL: BSTR padding;
        // VB6: padding As String
        string padding
        {
            // IDL: HRESULT padding ([out, retval] BSTR* ReturnValue);
            // VB6: Function padding As String
            [DispId(-2147413101)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT padding (BSTR value);
            // VB6: Sub padding (ByVal value As String)
            [DispId(-2147413101)] set;
        }

        /// <summary><para><c>paddingBottom</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>paddingBottom</c> property was the following:  <c>VARIANT paddingBottom</c>;</para></remarks>
        // IDL: VARIANT paddingBottom;
        // VB6: paddingBottom As Any
        object paddingBottom
        {
            // IDL: HRESULT paddingBottom ([out, retval] VARIANT* ReturnValue);
            // VB6: Function paddingBottom As Any
            [DispId(-2147413098)] get;
            // IDL: HRESULT paddingBottom (VARIANT value);
            // VB6: Sub paddingBottom (ByVal value As Any)
            [DispId(-2147413098)] set;
        }

        /// <summary><para><c>paddingLeft</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>paddingLeft</c> property was the following:  <c>VARIANT paddingLeft</c>;</para></remarks>
        // IDL: VARIANT paddingLeft;
        // VB6: paddingLeft As Any
        object paddingLeft
        {
            // IDL: HRESULT paddingLeft ([out, retval] VARIANT* ReturnValue);
            // VB6: Function paddingLeft As Any
            [DispId(-2147413097)] get;
            // IDL: HRESULT paddingLeft (VARIANT value);
            // VB6: Sub paddingLeft (ByVal value As Any)
            [DispId(-2147413097)] set;
        }

        /// <summary><para><c>paddingRight</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object paddingRight { [DispId(-2147413099)] get; }

        /// <summary><para><c>paddingTop</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object paddingTop { [DispId(-2147413100)] get; }

        /// <summary><para><c>pageBreakAfter</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string pageBreakAfter
        {
            [DispId(-2147413034)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>pageBreakBefore</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string pageBreakBefore
        {
            [DispId(-2147413035)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>pixelHeight</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        int pixelHeight { [DispId(-2147414109)] get; }

        /// <summary><para><c>pixelLeft</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        int pixelLeft { [DispId(-2147414111)] get; }

        /// <summary><para><c>pixelTop</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        int pixelTop { [DispId(-2147414112)] get; }

        /// <summary><para><c>pixelWidth</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        int pixelWidth { [DispId(-2147414110)] get; }

        /// <summary><para><c>posHeight</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        float posHeight { [DispId(-2147414105)] get; }

        /// <summary><para><c>position</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string position
        {
            [DispId(-2147413022)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>posLeft</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        float posLeft { [DispId(-2147414107)] get; }

        /// <summary><para><c>posTop</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        float posTop { [DispId(-2147414108)] get; }

        /// <summary><para><c>posWidth</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        float posWidth { [DispId(-2147414106)] get; }

        /// <summary><para><c>styleFloat</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string styleFloat
        {
            [DispId(-2147413042)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>textAlign</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string textAlign
        {
            [DispId(-2147418040)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>textDecoration</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string textDecoration
        {
            [DispId(-2147413077)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>textDecorationBlink</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        bool textDecorationBlink
        {
            [DispId(-2147413090)]
            [return: MarshalAs(UnmanagedType.VariantBool)]
            get;
        }

        /// <summary><para><c>textDecorationLineThrough</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        bool textDecorationLineThrough
        {
            [DispId(-2147413092)]
            [return: MarshalAs(UnmanagedType.VariantBool)]
            get;
        }

        /// <summary><para><c>textDecorationNone</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        bool textDecorationNone
        {
            [DispId(-2147413089)]
            [return: MarshalAs(UnmanagedType.VariantBool)]
            get;
        }

        /// <summary><para><c>textDecorationOverline</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        bool textDecorationOverline
        {
            [DispId(-2147413043)]
            [return: MarshalAs(UnmanagedType.VariantBool)]
            get;
        }

        /// <summary><para><c>textDecorationUnderline</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        bool textDecorationUnderline
        {
            [DispId(-2147413091)]
            [return: MarshalAs(UnmanagedType.VariantBool)]
            get;
        }

        /// <summary><para><c>textIndent</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object textIndent { [DispId(-2147413105)] get; }

        /// <summary><para><c>textTransform</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string textTransform
        {
            [DispId(-2147413108)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>top</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object top { [DispId(-2147418108)] get; }

        /// <summary><para><c>verticalAlign</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object verticalAlign { [DispId(-2147413064)] get; }

        /// <summary><para><c>visibility</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string visibility
        {
            [DispId(-2147413032)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>whiteSpace</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string whiteSpace
        {
            [DispId(-2147413036)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>width</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object width { [DispId(-2147418107)] get; }

        /// <summary><para><c>wordSpacing</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object wordSpacing { [DispId(-2147413065)] get; }

        /// <summary><para><c>zIndex</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        object zIndex { [DispId(-2147413021)] get; }
    }
}