/*
 * (c) 2024  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities;

/// <summary>
/// Utility class for reading and writing text blobs
/// to and from a stream. The underlying stream stores
/// the text as UTF-8 with low-ascii control characters
/// used as record boundaries.
/// </summary>
public static class TextBlobStore
{

  /// <summary>
  /// Return a function that writes blobs to the target stream.
  /// The caller of this method is responsible for closing the stream
  /// after the last blob has been written.
  /// </summary>
  /// <param name="target">
  /// The target stream to write to.
  /// </param>
  /// <param name="maxBlobSize">
  /// The maximum blob size to support, in UTF-8 bytes. By default, 0xFF00.
  /// </param>
  public static Action<string> CreateBlobWriter(
    Stream target, int maxBlobSize = 0xFF00)
  {
    var writer = new Writer(target, maxBlobSize);
    return writer.Write;
  }

  /// <summary>
  /// Read all blobs from the source stream.
  /// The caller of this method is responsible for closing the stream
  /// after all blobs have been read.
  /// </summary>
  /// <param name="source">
  /// The source stream to read from.
  /// </param>
  /// <param name="maxBlobSize">
  /// The maximum blob size to support, in UTF-8 bytes. By default, 0xFF00.
  /// </param>
  /// <returns></returns>
  public static IEnumerable<string> ReadAllBlobs(
    Stream source, int maxBlobSize = 0xFF00)
  {
    var reader = new Reader(source, maxBlobSize);
    return reader.ReadAll();
  }

  /// <summary>
  /// Marks the start of a record in the stream
  /// </summary>
  public const byte RecordStart = 0x02; // ASCII STX

  /// <summary>
  /// Marks the end of a record in the stream
  /// </summary>
  public const byte RecordEnd = 0x03; // ASCII ETX

  /// <summary>
  /// The record filler used between records (a line break, for easier human reading)
  /// </summary>
  public const byte RecordFiller = 0x0A; // ASCII LF

  /// <summary>
  /// A text blob stream writer.
  /// Normally created through <see cref="CreateBlobWriter(Stream, int)"/>.
  /// </summary>
  public class Writer
  {
    private readonly byte[] _buffer;

    /// <summary>
    /// Creata a new TextBlobStore.Writer
    /// </summary>
    /// <param name="target">
    /// The stream to write to. Must be writable.
    /// </param>
    /// <param name="maxBlobSize">
    /// The default maximum size of a blob in UTF-8 bytes. By default, 0xFF00,
    /// which is close to the maximum supported size 
    /// </param>
    public Writer(Stream target, int maxBlobSize = 0xFF00)
    {
      Target = target;
      if(!Target.CanWrite)
      {
        throw new ArgumentException("Stream must be writable");
      }
      if(maxBlobSize > 0xFFF0)
      {
        throw new ArgumentException("Blob size limit too large (must be 2^16-16 or below)");
      }
      MaxBlobSize = maxBlobSize;
      _buffer = new byte[maxBlobSize+8];
    }

    /// <summary>
    /// The target stream. This class does own it.
    /// </summary>
    public Stream Target { get; }

    /// <summary>
    /// The maximum size of a blob in UTF-8 bytes
    /// </summary>
    public int MaxBlobSize { get; }

    /// <summary>
    /// Writes a blob text to the stream at the current position.
    /// </summary>
    public void Write(string blob)
    {
      var byteCount = Encoding.UTF8.GetByteCount(blob);
      if(byteCount > MaxBlobSize)
      {
        throw new ArgumentException("Blob too large");
      }
      var terminatorIndex = blob.IndexOf((char)RecordEnd);
      if(terminatorIndex >= 0)
      {
        throw new ArgumentException("Blob contains an unsupported character (ASCII ETX)");
      }
      _buffer[0] = RecordStart;
      var byteCountWritten = Encoding.UTF8.GetBytes(blob, 0, blob.Length, _buffer, 1);
      _buffer[byteCountWritten+1] = RecordEnd;
      _buffer[byteCountWritten+2] = RecordFiller;
      Target.Write(_buffer, 0, byteCountWritten+3);
    }
  }

  /// <summary>
  /// A text blob stream reader
  /// </summary>
  public class Reader
  {
    private readonly byte[] _buffer;
    private int _tailPointer;
    private int _readPointer;

    /// <summary>
    /// Create a new TextBlobStore.Reader
    /// </summary>
    /// <param name="source">
    /// The source stream
    /// </param>
    /// <param name="maxBlobSize">
    /// The maximum supported blob size in UTF-8 bytes. By default, 0xFF00.
    /// </param>
    public Reader(Stream source, int maxBlobSize = 0xFF00)
    {
      _buffer = new byte[maxBlobSize+8];
      Source = source;
    }

    /// <summary>
    /// The source stream. This class does not own it, but does read eagerly from it
    /// (possibly overreading)
    /// </summary>
    public Stream Source { get; }

    /// <summary>
    /// Read all blobs from the stream.
    /// </summary>
    /// <returns>
    /// A sequence of blob texts
    /// </returns>
    public IEnumerable<string> ReadAll()
    {
      string? blob;
      while((blob = TryRead()) != null)
      {
        yield return blob;
      }
    }

    /// <summary>
    /// Try to read the next blob from the stream. If there is no more data
    /// null is returned.
    /// </summary>
    public string? TryRead()
    {
      if(!MoveToNextRecord())
      {
        return null;
      }
      var end = FindRecordEnd();
      var span = new ReadOnlySpan<byte>(_buffer, _readPointer+1, end-_readPointer-1);
      var blob = Encoding.UTF8.GetString(span);
      _readPointer = end+1;
      return blob;
    }

    /// <summary>
    /// Move to the start of the next record in the stream, if there is one.
    /// After this method returns, the read pointer is at the record start
    /// marker. The end of the record may or may not be in the buffer.
    /// This method may read from the source stream to fill the buffer.
    /// </summary>
    /// <returns>
    /// True if the next start was found, false if there are no more records
    /// in the stream.
    /// </returns>
    private bool MoveToNextRecord()
    {
      if(_tailPointer <= _readPointer) // Buffer is empty
      {
        if(!FillBuffer())
        {
          return false;
        }
      }
      var start = Array.IndexOf(_buffer, RecordStart, _readPointer, _tailPointer-_readPointer);
      if(start < 0)
      {
        // Only inter-record content in the buffer
        _readPointer = _tailPointer;
        if(!FillBuffer())
        {
          return false;
        }
      }
      _readPointer = start;
      return true;
    }

    private int FindRecordEnd()
    {
      if(_tailPointer <= _readPointer) // Buffer is empty
      {
        throw new InvalidOperationException(
          "Expecting record start to be at the read pointer, but the buffer is empty");
      }
      if(_buffer[_readPointer] != RecordStart)
      {
        throw new InvalidOperationException(
          "Expecting record start to be at the read pointer");
      }
      var end = Array.IndexOf(_buffer, RecordEnd, _readPointer, _tailPointer-_readPointer);
      if(end < 0)
      {
        // Record end not in the buffer. Try again after filling the buffer
        if(!FillBuffer())
        {
          throw new InvalidOperationException(
            "No record end found - premature EOF");
        }
        end = Array.IndexOf(_buffer, RecordEnd, _readPointer, _tailPointer-_readPointer);
        if(end < 0)
        {
          throw new InvalidOperationException(
            "No record end within the maximum buffer bounds (incompatible data)");
        }
      }
      return end;
    }

    /// <summary>
    /// Fills the buffer with data from the source stream after shifting 
    /// existing data to the start of the buffer.
    /// </summary>
    /// <returns>
    /// True if there is any data in the buffer after filling.
    /// False if there is no data left in the buffer nor the source stream.
    /// </returns>
    private bool FillBuffer()
    {
      if(_tailPointer > _readPointer)
      {
        // Move the remaining data to the start of the buffer
        Array.Copy(_buffer, _readPointer, _buffer, 0, _tailPointer-_readPointer);
        _tailPointer -= _readPointer;
        _readPointer = 0;
      }
      var readBytes = Source.Read(_buffer, _tailPointer, _buffer.Length-_tailPointer);
      _tailPointer += readBytes;
      return _tailPointer > 0;
    }

  }
}
