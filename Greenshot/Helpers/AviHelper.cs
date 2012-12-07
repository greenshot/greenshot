// AForge Video for Windows Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2007-2009
// andrew.kirillov@aforgenet.com
// 
//
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using GreenshotPlugin.UnmanagedHelpers;
using System.Collections.Generic;
using System.IO;

namespace Greenshot.Helpers {

	/// <summary>
	/// AVI files writing using Video for Windows interface.
	/// </summary>
	/// 
	/// <remarks><para>The class allows to write AVI files using Video for Windows API.</para>
	/// 
	/// <para>Sample usage:</para>
	///     /// // instantiate AVI writer, use WMV3 codec
	/// AVIWriter writer = new AVIWriter( "wmv3" );
	/// // create new AVI file and open it
	/// writer.Open( "test.avi", 320, 240 );
	/// // create frame image
	/// Bitmap image = new Bitmap( 320, 240 );
	/// 
	/// for ( int i = 0; i &lt; 240; i++ )
	/// {
	///     // update image
	///     image.SetPixel( i, i, Color.Red );
	///     // add the image as a new frame of video file
	///     writer.AddFrame( image );
	/// }
	/// writer.Close( );
	/// 
	/// </remarks>
	/// 
	public class AVIWriter : IDisposable {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AVIWriter));
		// AVI file
		private IntPtr file;
		// video stream
		private IntPtr stream;
		// compressed stream
		private IntPtr streamCompressed;
		// width of video frames
		private int width;
		// height of vide frames
		private int height;
		// length of one line
		private int stride;
		// quality
		private int quality = -1;
		// frame rate
		private int rate = 25;
		// current position
		private int position;
		// codec used for video compression
		private string codec = null;	//"DIB ";

		/// <summary>
		/// Width of video frames.
		/// </summary>
		/// 
		/// <remarks><para>The property specifies the width of video frames, which are acceptable
		/// by <see cref="AddFrame"/> method for saving, which is set in <see cref="Open"/>
		/// method.</para></remarks>
		/// 
		public int Width {
			get {
				return width;
			}
		}

		/// <summary>
		/// Height of video frames.
		/// </summary>
		/// 
		/// <remarks><para>The property specifies the height of video frames, which are acceptable
		/// by <see cref="AddFrame"/> method for saving, which is set in <see cref="Open"/>
		/// method.</para></remarks>
		/// 
		public int Height {
			get {
				return height;
			}
		}

		/// <summary>
		/// Current position in video stream.
		/// </summary>
		/// 
		/// <remarks><para>The property tell current position in video stream, which actually equals
		/// to the amount of frames added using <see cref="AddFrame"/> method.</para></remarks>
		/// 
		public int Position {
			get {
				return position;
			}
		}

		/// <summary>
		/// Desired playing frame rate.
		/// </summary>
		/// 
		/// <remarks><para>The property sets the video frame rate, which should be use during playing
		/// of the video to be saved.</para>
		/// 
		/// <para><note>The property should be set befor opening new file to take effect.</note></para>
		/// 
		/// <para>Default frame rate is set to <b>25</b>.</para></remarks>
		/// 
		public int FrameRate {
			get {
				return rate;
			}
			set {
				rate = value;
			}
		}

		/// <summary>
		/// Codec used for video compression.
		/// </summary>
		/// 
		/// <remarks><para>The property sets the FOURCC code of video compression codec, which needs to
		/// be used for video encoding.</para>
		/// 
		/// <para><note>The property should be set befor opening new file to take effect.</note></para>
		/// 
		/// <para>Default video codec is set <b>"DIB "</b>, which means no compression.</para></remarks>
		/// 
		public string Codec {
			get {
				return codec;
			}
			set {
				codec = value;
			}
		}

		/// <summary>
		/// Compression video quality.
		/// </summary>
		/// 
		/// <remarks><para>The property sets video quality used by codec in order to balance compression rate
		/// and image quality. The quality is measured usually in the [0, 100] range.</para>
		/// 
		/// <para><note>The property should be set befor opening new file to take effect.</note></para>
		/// 
		/// <para>Default value is set to <b>-1</b> - default compression quality of the codec.</para></remarks>
		/// 
		public int Quality {
			get {
				return quality;
			}
			set {
				quality = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AVIWriter"/> class.
		/// </summary>
		/// 
		/// <remarks>Initializes Video for Windows library.</remarks>
		/// 
		public AVIWriter() {
			Avi32.AVIFileInit();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AVIWriter"/> class.
		/// </summary>
		/// 
		/// <param name="codec">Codec to use for compression. eg [CVID],[IV50]</param>
		/// 
		/// <remarks>Initializes Video for Windows library.</remarks>
		/// 
		public AVIWriter(string codec)
			: this() {
			this.codec = codec;
		}

		/// <summary>
		/// Destroys the instance of the <see cref="AVIWriter"/> class.
		/// </summary>
		/// 
		~AVIWriter() {
			Dispose(false);
		}

		/// <summary>
		/// Dispose the object.
		/// </summary>
		/// 
		/// <remarks>Frees unmanaged resources used by the object. The object becomes unusable
		/// after that.</remarks>
		/// 
		public void Dispose() {
			Dispose(true);
			// remove me from the Finalization queue 
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose the object.
		/// </summary>
		/// 
		/// <param name="disposing">Indicates if disposing was initiated manually.</param>
		/// 
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				// dispose managed resources
			}
			// close current AVI file if any opened and uninitialize AVI library
			Close();
			Avi32.AVIFileExit();
		}

		/// <summary>
		/// Create new AVI file and open it for writing.
		/// </summary>
		/// 
		/// <param name="fileName">AVI file name to create.</param>
		/// <param name="width">Video width.</param>
		/// <param name="height">Video height.</param>
		/// 
		/// <remarks><para>The method opens (creates) a video files, configure video codec and prepares
		/// the stream for saving video frames with a help of <see cref="AddFrame"/> method.</para></remarks>
		/// 
		/// <exception cref="ApplicationException">Failure of opening video files (the exception message
		/// specifies the issues).</exception>
		/// 
		public bool Open(string fileName, int width, int height) {
			lock (this) {
				// calculate stride
				stride = width * 4;
				if ((stride % 4) != 0) {
					stride += (4 - stride % 4);
				}

				this.width = width;
				this.height = height;

				// describe new stream
				Avi32.AVISTREAMINFO info = new Avi32.AVISTREAMINFO();

				LOG.InfoFormat("Available codecs: {0}", String.Join(", ", Avi32.AvailableCodecs.ToArray()));

				info.type = Avi32.mmioFOURCC("vids");
				if (codec != null) {
					info.handler = Avi32.mmioFOURCC(codec);
				} else {
					info.handler = Avi32.mmioFOURCC("DIB ");
				}
				info.scale = 1;
				info.rate = rate;
				info.suggestedBufferSize = stride * height;

				try {
					// create new file
					if (Avi32.AVIFileOpen(out file, fileName, Avi32.OpenFileMode.Create | Avi32.OpenFileMode.Write, IntPtr.Zero) != 0) {
						throw new ApplicationException("Failed opening file");
					}

					// create stream
					if (Avi32.AVIFileCreateStream(file, out stream, ref info) != 0) {
						throw new ApplicationException("Failed creating stream");
					}

					// describe compression options
					Avi32.AVICOMPRESSOPTIONS options = new Avi32.AVICOMPRESSOPTIONS();
					// uncomment if video settings dialog is required to show
					int retCode = 0;
					if (codec == null) {
						retCode = Avi32.AVISaveOptions(stream, ref options);
						if (retCode == 0) {
							LOG.Debug("Cancel clicked!");
							return false;
						}
						codec = Avi32.decode_mmioFOURCC(options.handler);
						quality = options.quality;
					} else {
						options.handler = Avi32.mmioFOURCC(codec);
						options.quality = quality;
					}
					LOG.DebugFormat("Codec {0} selected with quality {1}.", codec, quality);

					AviError retval;
					// create compressed stream
					try {
						retval = Avi32.AVIMakeCompressedStream(out streamCompressed, stream, ref options, IntPtr.Zero);
					} catch (Exception exCompress) {
						LOG.Warn("Couldn't use compressed stream.", exCompress);
						retval = AviError.AVIERR_OK;
					}
					if (retval != AviError.AVIERR_OK) {
						throw new ApplicationException(string.Format("Failed creating compressed stream: {0}", retval));
					}


					// describe frame format
					BitmapInfoHeader bitmapInfoHeader = new BitmapInfoHeader(width, height, 32);

					// set frame format
					if (streamCompressed != IntPtr.Zero) {
						retval = Avi32.AVIStreamSetFormat(streamCompressed, 0, ref bitmapInfoHeader, Marshal.SizeOf(bitmapInfoHeader.GetType()));
					} else {
						retval = Avi32.AVIStreamSetFormat(stream, 0, ref bitmapInfoHeader, Marshal.SizeOf(bitmapInfoHeader.GetType()));
					}
					if (retval != 0) {
						throw new ApplicationException(string.Format("Failed creating stream: {0}", retval));
					}
					position = 0;
					return true;
				} catch (Exception ex) {
					Close();
					Avi32.AVIFileExit();
					if (File.Exists(fileName)) {
						File.Delete(fileName);
					}
					throw ex;
				}
			}
		}

		/// <summary>
		/// Close video file.
		/// </summary>
		/// 
		public void Close() {
			LOG.Debug("Close called");
			lock (this) {
				// release compressed stream
				if (streamCompressed != IntPtr.Zero) {
					LOG.Debug("AVIStreamRelease streamCompressed");
					Avi32.AVIStreamRelease(streamCompressed);
					streamCompressed = IntPtr.Zero;
				}

				// release stream
				if (stream != IntPtr.Zero) {
					LOG.Debug("AVIStreamRelease stream");
					Avi32.AVIStreamRelease(stream);
					stream = IntPtr.Zero;
				}

				// release file
				if (file != IntPtr.Zero) {
					LOG.Debug("AVIFileRelease file");
					Avi32.AVIFileRelease(file);
					file = IntPtr.Zero;
				}
			}
		}

		public void AddEmptyFrames(int frames) {
			lock (this) {
				position += frames;
			}
		}

		/// <summary>
		/// Add new frame to the AVI file.
		/// </summary>
		/// <param name="frameData">New frame data.</param>
		public void AddLowLevelFrame(IntPtr frameData) {
			lock (this) {
				// write to stream
				if (Avi32.AVIStreamWrite(streamCompressed, position, 1, frameData, stride * height, 0, IntPtr.Zero, IntPtr.Zero) != 0) {
					throw new ApplicationException("Failed adding frame");
				}

				position++;
			}
		}
	}

	/// <summary>
	/// Windows API functions and structures.
	/// </summary>
	/// 
	/// <remarks>The class provides Video for Windows and some other Avi32 functions and structurs.</remarks>
	/// 
	internal static class Avi32 {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Avi32));

		[DllImport("MSVFW32", CharSet = CharSet.Ansi)]
		static extern bool ICInfo(int fccType, int fccHandler, ref ICINFO lpicinfo);

		[DllImport("MSVFW32"), PreserveSig]
		static extern IntPtr ICOpen(int fccType, int fccHandler, ICMODE wMode);

		[DllImport("MSVFW32")]
		static extern int ICClose(IntPtr hic);

		[DllImport("MSVFW32", CharSet = CharSet.Ansi)]
		static extern int ICGetInfo(IntPtr hic, ref ICINFO lpicinfo, int cb);

		// --- Video for Windows Functions

		/// <summary>
		/// Initialize the AVIFile library.
		/// </summary>
		/// 
		[DllImport("avifil32")]
		public static extern void AVIFileInit();

		/// <summary>
		/// Exit the AVIFile library.
		/// </summary>
		[DllImport("avifil32")]
		public static extern void AVIFileExit();

		/// <summary>
		/// Open an AVI file.
		/// </summary>
		/// 
		/// <param name="aviHandler">Opened AVI file interface.</param>
		/// <param name="fileName">AVI file name.</param>
		/// <param name="mode">Opening mode (see <see cref="OpenFileMode"/>).</param>
		/// <param name="handler">Handler to use (<b>null</b> to use default).</param>
		/// 
		/// <returns>Returns zero on success or error code otherwise.</returns>
		/// 
		[DllImport("avifil32", CharSet = CharSet.Unicode)]
		public static extern AviError AVIFileOpen(out IntPtr aviHandler, String fileName, OpenFileMode mode, IntPtr handler);

		/// <summary>
		/// Release an open AVI stream.
		/// </summary>
		/// 
		/// <param name="aviHandler">Open AVI file interface.</param>
		/// 
		/// <returns>Returns the reference count of the file.</returns>
		/// 
		[DllImport("avifil32")]
		public static extern int AVIFileRelease(IntPtr aviHandler);

		/// <summary>
		/// Get stream interface that is associated with a specified AVI file
		/// </summary>
		/// 
		/// <param name="aviHandler">Handler to an open AVI file.</param>
		/// <param name="streamHandler">Stream interface.</param>
		/// <param name="streamType">Stream type to open.</param>
		/// <param name="streamNumner">Count of the stream type. Identifies which occurrence of the specified stream type to access. </param>
		/// 
		/// <returns></returns>
		/// 
		[DllImport("avifil32")]
		public static extern int AVIFileGetStream(IntPtr aviHandler, out IntPtr streamHandler, int streamType, int streamNumner);

		/// <summary>
		/// Create a new stream in an existing file and creates an interface to the new stream. 
		/// </summary>
		/// 
		/// <param name="aviHandler">Handler to an open AVI file.</param>
		/// <param name="streamHandler">Stream interface.</param>
		/// <param name="streamInfo">Pointer to a structure containing information about the new stream.</param>
		/// 
		/// <returns>Returns zero if successful or an error otherwise.</returns>
		/// 
		[DllImport("avifil32")]
		public static extern int AVIFileCreateStream(IntPtr aviHandler, out IntPtr streamHandler, ref AVISTREAMINFO streamInfo);

		/// <summary>
		/// Release an open AVI stream.
		/// </summary>
		/// 
		/// <param name="streamHandler">Handle to an open stream.</param>
		/// 
		/// <returns>Returns the current reference count of the stream.</returns>
		/// 
		[DllImport("avifil32")]
		public static extern int AVIStreamRelease(IntPtr streamHandler);

		/// <summary>
		/// Set the format of a stream at the specified position.
		/// </summary>
		/// 
		/// <param name="streamHandler">Handle to an open stream.</param>
		/// <param name="position">Position in the stream to receive the format.</param>
		/// <param name="format">Pointer to a structure containing the new format.</param>
		/// <param name="formatSize">Size, in bytes, of the block of memory referenced by <b>format</b>.</param>
		/// 
		/// <returns>Returns zero if successful or an error otherwise.</returns>
		/// 
		[DllImport("avifil32")]
		public static extern AviError AVIStreamSetFormat(IntPtr streamHandler, int position, ref BitmapInfoHeader format, int formatSize);

		/// <summary>
		/// Get the starting sample number for the stream.
		/// </summary>
		/// 
		/// <param name="streamHandler">Handle to an open stream.</param>
		/// 
		/// <returns>Returns the number if successful or – 1 otherwise.</returns>
		/// 
		[DllImport("avifil32")]
		public static extern int AVIStreamStart(IntPtr streamHandler);

		/// <summary>
		/// Get the length of the stream.
		/// </summary>
		/// <param name="streamHandler">Handle to an open stream.</param>
		/// <returns>Returns the stream's length, in samples, if successful or -1 otherwise. </returns>
		[DllImport("avifil32")]
		public static extern int AVIStreamLength(IntPtr streamHandler);

		/// <summary>
		/// Obtain stream header information.
		/// </summary>
		/// 
		/// <param name="streamHandler">Handle to an open stream.</param>
		/// <param name="streamInfo">Pointer to a structure to contain the stream information.</param>
		/// <param name="infoSize">Size, in bytes, of the structure used for <b>streamInfo</b>.</param>
		/// 
		/// <returns>Returns zero if successful or an error otherwise.</returns>
		/// 
		[DllImport("avifil32", CharSet = CharSet.Unicode)]
		public static extern int AVIStreamInfo(IntPtr streamHandler, ref AVISTREAMINFO streamInfo, int infoSize);

		/// <summary>
		/// Prepare to decompress video frames from the specified video stream
		/// </summary>
		/// <param name="streamHandler">Pointer to the video stream used as the video source.</param>
		/// <param name="wantedFormat">Pointer to a structure that defines the desired video format. Specify NULL to use a default format.</param>
		/// <returns>Returns an object that can be used with the <see cref="AVIStreamGetFrame"/> function.</returns>
		[DllImport("avifil32")]
		public static extern IntPtr AVIStreamGetFrameOpen(IntPtr streamHandler, ref BitmapInfoHeader wantedFormat);

		/// <summary>
		/// Prepare to decompress video frames from the specified video stream.
		/// </summary>
		/// <param name="streamHandler">Pointer to the video stream used as the video source.</param>
		/// <param name="wantedFormat">Pointer to a structure that defines the desired video format. Specify NULL to use a default format.</param>
		/// <returns>Returns a <b>GetFrame</b> object that can be used with the <see cref="AVIStreamGetFrame"/> function.</returns>
		[DllImport("avifil32")]
		public static extern IntPtr AVIStreamGetFrameOpen(IntPtr streamHandler, int wantedFormat);

		/// <summary>
		/// Releases resources used to decompress video frames.
		/// </summary>
		/// <param name="getFrameObject">Handle returned from the <see cref="AVIStreamGetFrameOpen(IntPtr,int)"/> function.</param>
		/// <returns>Returns zero if successful or an error otherwise.</returns>
		[DllImport("avifil32")]
		public static extern int AVIStreamGetFrameClose(IntPtr getFrameObject);

		/// <summary>
		/// Return the address of a decompressed video frame. 
		/// </summary>
		/// <param name="getFrameObject">Pointer to a GetFrame object.</param>
		/// <param name="position">Position, in samples, within the stream of the desired frame.</param>
		/// <returns>Returns a pointer to the frame data if successful or NULL otherwise.</returns>
		[DllImport("avifil32")]
		public static extern IntPtr AVIStreamGetFrame(IntPtr getFrameObject, int position);

		/// <summary>
		/// Write data to a stream.
		/// </summary>
		/// <param name="streamHandler">Handle to an open stream.</param>
		/// <param name="start">First sample to write.</param>
		/// <param name="samples">Number of samples to write.</param>
		/// <param name="buffer">Pointer to a buffer containing the data to write. </param>
		/// <param name="bufferSize">Size of the buffer referenced by <b>buffer</b>.</param>
		/// <param name="flags">Flag associated with this data.</param>
		/// <param name="samplesWritten">Pointer to a buffer that receives the number of samples written. This can be set to NULL.</param>
		/// <param name="bytesWritten">Pointer to a buffer that receives the number of bytes written. This can be set to NULL.</param>
		/// 
		/// <returns>Returns zero if successful or an error otherwise.</returns>
		/// 
		[DllImport("avifil32")]
		public static extern AviError AVIStreamWrite(IntPtr streamHandler, int start, int samples, IntPtr buffer, int bufferSize, int flags, IntPtr samplesWritten, IntPtr bytesWritten);

		/// <summary>
		/// Retrieve the save options for a file and returns them in a buffer.
		/// </summary>
		/// <param name="window">Handle to the parent window for the Compression Options dialog box.</param>
		/// <param name="flags">Flags for displaying the Compression Options dialog box.</param>
		/// <param name="streams">Number of streams that have their options set by the dialog box.</param>
		/// <param name="streamInterfaces">Pointer to an array of stream interface pointers.</param>
		/// <param name="options">Pointer to an array of pointers to AVICOMPRESSOPTIONS structures.</param>
		/// <returns>Returns TRUE if the user pressed OK, FALSE for CANCEL, or an error otherwise.</returns>
		[DllImport("avifil32")]
		public static extern int AVISaveOptions(IntPtr window, int flags, int streams, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] streamInterfaces, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] options);

		/// <summary>
		/// Free the resources allocated by the AVISaveOptions function. 
		/// </summary>
		/// <param name="streams">Count of the AVICOMPRESSOPTIONS structures referenced in <b>options</b>.</param>
		/// <param name="options">Pointer to an array of pointers to AVICOMPRESSOPTIONS structures.</param>
		/// <returns>Returns 0.</returns>
		[DllImport("avifil32")]
		public static extern AviError AVISaveOptionsFree(int streams, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] options);

		/// <summary>
		/// Create a compressed stream from an uncompressed stream and a
		/// compression filter, and returns the address of a pointer to
		/// the compressed stream.
		/// </summary>
		/// <param name="compressedStream">Pointer to a buffer that receives the compressed stream pointer.</param>
		/// <param name="sourceStream">Pointer to the stream to be compressed.</param>
		/// <param name="options">Pointer to a structure that identifies the type of compression to use and the options to apply.</param>
		/// <param name="clsidHandler">Pointer to a class identifier used to create the stream.</param>
		/// <returns>Returns 0 if successful or an error otherwise.</returns>
		[DllImport("avifil32")]
		public static extern AviError AVIMakeCompressedStream(out IntPtr compressedStream, IntPtr sourceStream, ref AVICOMPRESSOPTIONS options, IntPtr clsidHandler);

		/// <summary>
		/// Code type
		/// </summary>
		public enum ICMODE {
			ICMODE_COMPRESS = 1,
			ICMODE_DECOMPRESS = 2,
			ICMODE_FASTDECOMPRESS = 3,
			ICMODE_QUERY = 4,
			ICMODE_FASTCOMPRESS = 5,
			ICMODE_DRAW = 8
		}

		// --- structures

		/// <summary>
		/// Structor for the codec info
		/// See: http://msdn.microsoft.com/en-us/library/windows/desktop/dd743162%28v=vs.85%29.aspx
		/// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct ICINFO {
			public int dwSize;
			public int fccType;
			public int fccHandler;
			public int dwFlags;
			public int dwVersion;
			public int dwVersionICM;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
			public string szName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szDescription;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szDriver;

			public ICINFO(int type) {
				dwSize = Marshal.SizeOf(typeof(ICINFO));
				fccType = type;
				fccHandler = 0;
				dwFlags = 0;
				dwVersion = 0;
				dwVersionICM = 0;
				szName = null;
				szDescription = null;
				szDriver = null;
			}
		}

		/// <summary>
		/// Structure, which contains information for a single stream .
		/// </summary>
		/// 
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
		public struct AVISTREAMINFO {
			/// <summary>
			/// Four-character code indicating the stream type.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int type;

			/// <summary>
			/// Four-character code of the compressor handler that will compress this video stream when it is saved.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int handler;

			/// <summary>
			/// Applicable flags for the stream.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int flags;

			/// <summary>
			/// Capability flags; currently unused.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int ñapabilities;

			/// <summary>
			/// Priority of the stream.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I2)]
			public short priority;

			/// <summary>
			/// Language of the stream.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I2)]
			public short language;

			/// <summary>
			/// Time scale applicable for the stream.
			/// </summary>
			/// 
			/// <remarks>Dividing <b>rate</b> by <b>scale</b> gives the playback rate in number of samples per second.</remarks>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int scale;

			/// <summary>
			/// Rate in an integer format.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int rate;

			/// <summary>
			/// Sample number of the first frame of the AVI file.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int start;

			/// <summary>
			/// Length of this stream.
			/// </summary>
			/// 
			/// <remarks>The units are defined by <b>rate</b> and <b>scale</b>.</remarks>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int length;

			/// <summary>
			/// Audio skew. This member specifies how much to skew the audio data ahead of the video frames in interleaved files.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int initialFrames;

			/// <summary>
			/// Recommended buffer size, in bytes, for the stream.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int suggestedBufferSize;

			/// <summary>
			/// Quality indicator of the video data in the stream.
			/// </summary>
			/// 
			/// <remarks>Quality is represented as a number between 0 and 10,000.</remarks>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int quality;

			/// <summary>
			/// Size, in bytes, of a single data sample.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int sampleSize;

			/// <summary>
			/// Dimensions of the video destination rectangle.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
			public RECT rectFrame;

			/// <summary>
			/// Number of times the stream has been edited.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int editCount;

			/// <summary>
			/// Number of times the stream format has changed.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int formatChangeCount;

			/// <summary>
			/// Description of the stream.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string name;
		}

		/// <summary>
		/// Structure, which contains information about a stream and how it is compressed and saved. 
		/// </summary>
		/// 
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct AVICOMPRESSOPTIONS {
			/// <summary>
			/// Four-character code indicating the stream type.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int type;

			/// <summary>
			/// Four-character code for the compressor handler that will compress this video stream when it is saved.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int handler;

			/// <summary>
			/// Maximum period between video key frames.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int keyFrameEvery;

			/// <summary>
			/// Quality value passed to a video compressor.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int quality;

			/// <summary>
			/// Video compressor data rate.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int bytesPerSecond;

			/// <summary>
			/// Flags used for compression.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int flags;

			/// <summary>
			/// Pointer to a structure defining the data format.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int format;

			/// <summary>
			/// Size, in bytes, of the data referenced by <b>format</b>.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int formatSize;

			/// <summary>
			/// Video-compressor-specific data; used internally.
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int parameters;

			/// <summary>
			/// Size, in bytes, of the data referenced by <b>parameters</b>.
			/// </summary>
			[MarshalAs(UnmanagedType.I4)]
			public int parametersSize;

			/// <summary>
			/// Interleave factor for interspersing stream data with data from the first stream. 
			/// </summary>
			/// 
			[MarshalAs(UnmanagedType.I4)]
			public int interleaveEvery;
		}

		// --- enumerations

		/// <summary>
		/// File access modes. 
		/// </summary>
		/// 
		[Flags]
		public enum OpenFileMode {
			Read = 0x00000000,
			Write = 0x00000001,
			ReadWrite = 0x00000002,
			ShareCompat = 0x00000000,
			ShareExclusive = 0x00000010,
			ShareDenyWrite = 0x00000020,
			ShareDenyRead = 0x00000030,
			ShareDenyNone = 0x00000040,
			Parse = 0x00000100,
			Delete = 0x00000200,
			Verify = 0x00000400,
			Cancel = 0x00000800,
			Create = 0x00001000,
			Prompt = 0x00002000,
			Exist = 0x00004000,
			Reopen = 0x00008000
		}

		/// <summary>
		/// .NET replacement of mmioFOURCC macros. Converts four characters to code.
		/// </summary>
		/// 
		/// <param name="str">Four characters string.</param>
		/// 
		/// <returns>Returns the code created from provided characters.</returns>
		/// 
		public static int mmioFOURCC(string str) {
			return (((int)(byte)(str[0])) |
				((int)(byte)(str[1]) << 8) |
				((int)(byte)(str[2]) << 16) |
				((int)(byte)(str[3]) << 24));
		}

		/// <summary>
		/// Inverse to <see cref="mmioFOURCC"/>. Converts code to fout characters string.
		/// </summary>
		/// 
		/// <param name="code">Code to convert.</param>
		/// 
		/// <returns>Returns four characters string.</returns>
		/// 
		public static string decode_mmioFOURCC(int code) {
			char[] chs = new char[4];

			for (int i = 0; i < 4; i++) {
				chs[i] = (char)(byte)((code >> (i << 3)) & 0xFF);
				if (!char.IsLetterOrDigit(chs[i])) {
					chs[i] = ' ';
				}
			}
			return new string(chs);
		}

		/// <summary>
		/// Get a list of available codecs.
		/// </summary>
		/// <returns>List<string></returns>
		public static List<string> AvailableCodecs {
			get {
				List<string> returnValues = new List<string>();
				int codecNr = 0;

				ICINFO codecInfo = new ICINFO(mmioFOURCC("VIDC"));

				bool success = true;
				do {
					success = ICInfo(codecInfo.fccType, codecNr++, ref codecInfo);
					if (success) {
						IntPtr hic = ICOpen(codecInfo.fccType, codecInfo.fccHandler, ICMODE.ICMODE_QUERY);
						if (hic != IntPtr.Zero) {
							ICGetInfo(hic, ref codecInfo, Marshal.SizeOf(codecInfo));
							string codecName = decode_mmioFOURCC(codecInfo.fccHandler);
							returnValues.Add(codecName);
							LOG.DebugFormat("Found codec {0} {4}, with name {1} and description {2}, driver {3}", codecName, codecInfo.szName, codecInfo.szDescription, codecInfo.szDriver, codecInfo.dwVersion);
							ICClose(hic);
						}
					}
				} while (success);
				return returnValues;
			}
		}

		/// <summary>
		/// Version of <see cref="AVISaveOptions(IntPtr, int, int, IntPtr[], IntPtr[])"/> for one stream only.
		/// </summary>
		/// 
		/// <param name="stream">Stream to configure.</param>
		/// <param name="options">Stream options.</param>
		/// 
		/// <returns>Returns TRUE if the user pressed OK, FALSE for CANCEL, or an error otherwise.</returns>
		/// 
		public static int AVISaveOptions(IntPtr stream, ref AVICOMPRESSOPTIONS options) {
			IntPtr[] streams = new IntPtr[1];
			IntPtr[] infPtrs = new IntPtr[1];
			IntPtr mem;
			int ret;

			// alloc unmanaged memory
			mem = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(AVICOMPRESSOPTIONS)));

			// copy from managed structure to unmanaged memory
			Marshal.StructureToPtr(options, mem, false);

			streams[0] = stream;
			infPtrs[0] = mem;

			// show dialog with a list of available compresors and configuration
			ret = AVISaveOptions(IntPtr.Zero, 0, 1, streams, infPtrs);

			// copy from unmanaged memory to managed structure
			options = (AVICOMPRESSOPTIONS)Marshal.PtrToStructure(mem, typeof(AVICOMPRESSOPTIONS));

			// free AVI compression options
			AVISaveOptionsFree(1, infPtrs);

			// clear it, because the information already freed by AVISaveOptionsFree
			options.format = 0;
			options.parameters = 0;

			// free unmanaged memory
			Marshal.FreeHGlobal(mem);

			return ret;
		}
	}

	/// <summary>
	/// AVI Error Codes
	/// </summary>
	[Flags]
	public enum AviError : uint {
		/// <summary>
		/// Compression is not supported for this type of data.
		/// This error might be returned if you try to compress
		/// data that is not audio or video.
		/// </summary>
		AVIERR_UNSUPPORTED = 0x80044065,
		/// <summary>
		/// The file couldn't be read, indicating a corrupt file or an unrecognized format
		/// </summary>
		AVIERR_BADFORMAT = 0x80044066,
		/// <summary>
		/// There is not enough memory to complete the operation.
		/// </summary>
		AVIERR_MEMORY = 0x80044067,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_INTERNAL = 0x80044068,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_BADFLAGS = 0x80044069,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_BADPARAM = 0x8004406A,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_BADSIZE = 0x8004406B,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_BADHANDLE = 0x8004406C,
		/// <summary>
		/// A disk error occurred while reading the file
		/// </summary>
		AVIERR_FILEREAD = 0x8004406D,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_FILEWRITE = 0x8004406E,
		/// <summary>
		/// A disk error occurred while opening the file
		/// </summary>
		AVIERR_FILEOPEN = 0x8004406F,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_COMPRESSOR = 0x80044070,
		/// <summary>
		/// A suitable compressor cannot be found.
		/// </summary>
		AVIERR_NOCOMPRESSOR = 0x80044071,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_READONLY = 0x80044072,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_NODATA = 0x80044073,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_BUFFERTOOSMALL = 0x80044074,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_CANTCOMPRESS = 0x80044075,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_USERABORT = 0x800440C6,
		/// <summary>
		///
		/// </summary>
		// TODO : Put documentation
		AVIERR_ERROR = 0x800440C7,
		/// <summary>
		/// Operation successful
		/// </summary>
		AVIERR_OK = 0x0
	}
}
