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
/// Description of TlobReader
/// </summary>
public class TlobReader: IDisposable
{
  private bool _disposed;
  private readonly byte[] _headerBuffer;
  private byte[] _blobBuffer;

  /// <summary>
  /// Create a new TlobReader, reading from a decompressed stream.
  /// ()
  /// </summary>
  /// <param name="baseStream">
  /// The raw uncompressed stream to read from.
  /// </param>
  public TlobReader(Stream baseStream)
  {
    BaseStream = baseStream;
    _headerBuffer = new byte[8];
    _blobBuffer = new byte[0x8000];
  }

  /// <summary>
  /// Create a new TlobReader for reading from the given stream.
  /// </summary>
  /// <param name="stream">
  /// The stream to read from.
  /// </param>
  /// <param name="decompress">
  /// If true, the input stream is assumed to be compressed using GZip,
  /// and a decompression stage is inserted.
  /// </param>
  /// <returns></returns>
  public static TlobReader FromStream(Stream stream, bool decompress)
  {
    if(decompress)
    {
      stream = new GZipStream(stream, CompressionMode.Decompress, false);
    }
    return new TlobReader(stream);
  }

  /// <summary>
  /// Create a new TlobReader for reading from the given file.
  /// </summary>
  /// <param name="path">
  /// The name of the file to read from.
  /// </param>
  /// <param name="decompress">
  /// If true, the input stream is assumed to be compressed using GZip,
  /// and a decompression stage is inserted.
  /// </param>
  /// <returns></returns>
  public static TlobReader FromFile(string path, bool decompress)
  {
    var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    return FromStream(stream, decompress);
  }

  /// <summary>
  /// The raw uncompressed stream to read from.
  /// </summary>
  public Stream BaseStream { get; }

  /// <summary>
  /// Try reading the next record from the stream, returning null
  /// at the end of the stream.
  /// </summary>
  public string? TryRead()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    var nHdr = BaseStream.Read(_headerBuffer, 0, 8);
    if(nHdr == 0)
    {
      return null;
    }
    if(nHdr != 8)
    {
      throw new IOException("Unexpected end of stream");
    }
    for(var i=0; i<6; i++)
    {
      var ch = (char)_headerBuffer[i];
      if(ch<'0' || ch>'9')
      {
        if(_headerBuffer[0] == 0x1f && _headerBuffer[1] == 0x8b)
        {
          throw new IOException(
            "Expecting a decompressed stream, but this looks like a GZip stream. "+
            "Did you forget to insert a decompressor?");
        }
        throw new IOException(
          "Invalid record header (expecting only digits in the first 6 bytes)");
      }
    }
    if(_headerBuffer[6] != (byte)'\r' || _headerBuffer[7] != (byte)'\n')
    {
      throw new IOException(
        "Invalid record header (expecting CR-LF in the last 2 bytes)");
    }
    var sizeText = Encoding.UTF8.GetString(_headerBuffer, 0, 6);
    var size = Int32.Parse(sizeText);
    if(size > _blobBuffer.Length)
    {
      // reallocate buffer, with some spare space
      _blobBuffer = new byte[size+4096];
    }
    var nBlob = BaseStream.Read(_blobBuffer, 0, size);
    if(nBlob != size)
    {
      throw new IOException("Unexpected end of stream");
    }
    return Encoding.UTF8.GetString(_blobBuffer, 0, size);
  }

  /// <summary>
  /// Enumerate all remaining records in the stream.
  /// </summary>
  public IEnumerable<string> ReadAll()
  {
    string? record;
    while((record = TryRead()) != null)
    {
      yield return record;
    }
  }

  /// <summary>
  /// Clean up resources.
  /// </summary>
  public void Dispose()
  {
    if(!_disposed)
    {
      BaseStream.Dispose();
      _disposed = true;
    }
  }

}
