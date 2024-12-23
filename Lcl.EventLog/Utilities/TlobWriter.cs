/*
 * (c) 2024  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities;

/// <summary>
/// Writes "Textual large objects" to a stream ("evarc" format)
/// </summary>
public class TlobWriter: IDisposable
{
  private readonly byte[] _headerBuffer;
  private byte[] _blobBuffer;
  private bool _disposed;

  /// <summary>
  /// Create a new TextBlobWriter
  /// </summary>
  public TlobWriter(
    Stream baseStream)
  {
    BaseStream = baseStream;
    _headerBuffer = new byte[8];
    _blobBuffer = new byte[0x8000];
  }

  /// <summary>
  /// Create a new TlobWriter for writing to the given stream,
  /// optionally compressing the output.
  /// </summary>
  /// <param name="stream">
  /// The writable stream to write to.
  /// </param>
  /// <param name="compress">
  /// If true, the output will be compressed using GZip.
  /// </param>
  public static TlobWriter FromStream(Stream stream, bool compress)
  {
    if(compress)
    {
      stream = new GZipStream(stream, CompressionLevel.SmallestSize, false);
    }
    return new TlobWriter(stream);
  }

  /// <summary>
  /// Create a new TlobWriter for writing the given file name,
  /// optionally compressing the output.
  /// </summary>
  /// <param name="path">
  /// The name of the file to write to (if <paramref name="compress"/> is true,
  /// this name should end in ".gz")
  /// </param>
  /// <param name="compress">
  /// Whether or not to compress the output (using GZip)
  /// </param>
  /// <returns></returns>
  public static TlobWriter FromFile(string path, bool compress)
  {
    var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
    return FromStream(stream, compress);
  }

  /// <summary>
  /// The underlying stream
  /// </summary>
  public Stream BaseStream { get; }

  /// <summary>
  /// Write a "tlob" to the stream
  /// </summary>
  public void WriteTlob(string tlob)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    var blobByteCount = Encoding.UTF8.GetByteCount(tlob);
    if(blobByteCount > 999999)
    {
      throw new ArgumentException(
        "Blob too large, exceeding the maximum size (999999 bytes)",
        nameof(tlob));
    }
    if(blobByteCount > _blobBuffer.Length)
    {
      // reallocate buffer
      _blobBuffer = new byte[blobByteCount+4096];
    }
    var blobByteCountStr = blobByteCount.ToString("D6");
    Encoding.UTF8.GetBytes(blobByteCountStr, 0, 6, _headerBuffer, 0);
    _headerBuffer[6] = (byte)'\r';
    _headerBuffer[7] = (byte)'\n';
    Encoding.UTF8.GetBytes(tlob, 0, tlob.Length, _blobBuffer, 0);
    BaseStream.Write(_headerBuffer, 0, 8);
    BaseStream.Write(_blobBuffer, 0, blobByteCount);
  }

  /// <summary>
  /// Clean up, closing the underlying stream
  /// </summary>
  public void Dispose()
  {
    if(!_disposed)
    {
      BaseStream.Flush();
      BaseStream.Dispose();
      _disposed = true;
    }
  }
}
