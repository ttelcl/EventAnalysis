/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Interace to version 2 raw event databse files
  /// </summary>
  public class RawEventDbV2
  {
    /// <summary>
    /// Create a new RawEventDbV2
    /// </summary>
    public RawEventDbV2(string fileName, bool allowWrite, bool allowCreate = false)
    {
      FileName = Path.GetFullPath(fileName);
      AllowWrite = allowWrite;
      AllowCreate = allowCreate;
      if(!AllowCreate && !File.Exists(FileName))
      {
        throw new FileNotFoundException(
          $"Attempt to open a nonexisting DB in no-create mode");
      }
    }

    /// <summary>
    /// The database filename
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The directory in which the database file exists
    /// </summary>
    public string DbDirectory {
      get {
        var dir = Path.GetDirectoryName(FileName);
        if(String.IsNullOrEmpty(dir))
        {
          throw new InvalidOperationException("Bad db name");
        }
        return dir;
      }
    }

    /// <summary>
    /// Allow opening the DB in writable mode
    /// </summary>
    public bool AllowWrite { get; }

    /// <summary>
    /// Allow opening the DB in DDL mode
    /// </summary>
    public bool AllowCreate { get; }

    /// <summary>
    /// Open a new database connection. This may create the database file if it didn't
    /// exist yet.
    /// </summary>
    /// <param name="writable">
    /// Enable writing to the db (i.e. not readonly)
    /// </param>
    /// <param name="create">
    /// Enable creating the database file if it didn't exist yet
    /// </param>
    /// <returns>
    /// The database connection, already opened
    /// </returns>
    public OpenDbV2 Open(bool writable, bool create = false)
    {
      if(!writable && create)
      {
        throw new ArgumentException(
          $"Invalid argument combination. create=true should imply writable=true");
      }
      if(writable && !AllowWrite)
      {
        throw new InvalidOperationException(
          $"Attempt to open a read-only database for writing");
      }
      if(create && !AllowCreate)
      {
        throw new InvalidOperationException(
          $"Attempt to open a no-DDL database in create mode");
      }
      SqliteOpenMode mode;
      if(create)
      {
        mode = SqliteOpenMode.ReadWriteCreate;
      }
      else if(writable)
      {
        mode = SqliteOpenMode.ReadWrite;
      }
      else
      {
        mode = SqliteOpenMode.ReadOnly;
      }
      if(!File.Exists(FileName))
      {
        if(create)
        {
          var dir = DbDirectory;
          if(!Directory.Exists(dir))
          {
            Directory.CreateDirectory(dir);
          }
        }
        else
        {
          throw new InvalidOperationException(
            $"The database does not exist and creation is disabled: {FileName}");
        }
      }
      var builder = new SqliteConnectionStringBuilder {
        DataSource = FileName,
        Mode = mode,
        ForeignKeys = true,
      };
      var conn = new SqliteConnection(builder.ConnectionString);
      return new OpenDbV2(this, conn, writable, create);
    }
  }

}
