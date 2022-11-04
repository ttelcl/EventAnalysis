/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lcl.EventLog.Utilities;


namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// An in-memory cache of the Providers table
  /// </summary>
  public class ProviderInfoCache: IReadOnlyCollection<ProviderInfoRow>
  {
    private readonly Dictionary<int, ProviderInfoRow> _byId;
    private readonly Dictionary<string, ProviderInfoRow> _byName;
    private readonly List<ProviderInfoRow> _newlyAdded;
    private int _nextId;

    /// <summary>
    /// Create a new ProvidersCache
    /// </summary>
    public ProviderInfoCache(IEnumerable<ProviderInfoRow> existing)
    {
      _byId = new Dictionary<int, ProviderInfoRow>();
      _byName = new Dictionary<string, ProviderInfoRow>();
      _newlyAdded = new List<ProviderInfoRow>();
      foreach(var row in existing)
      {
        Add(row);
      }
      _nextId =
        _byId.Count == 0
        ? 1
        : _byId.Values.Select(pir => pir.ProviderId).Max()+1; 
      // cannot use MaxBy() because of .net standard 2.0 compatibility
    }

    /// <summary>
    /// Add a row to this cache. Fails if the id or name are already in use
    /// </summary>
    internal void Add(ProviderInfoRow row)
    {
      if(_byId.ContainsKey(row.ProviderId))
      {
        throw new InvalidOperationException(
          $"Provider ID {row.ProviderId} is already in use");
      }
      if(_byName.ContainsKey(row.ProviderName))
      {
        throw new InvalidOperationException(
          $"Provider ID {row.ProviderId} is already registered");
      }
      _byId[row.ProviderId] = row;
      _byName[row.ProviderName] = row;
    }

    /// <summary>
    /// Observe the provider name and GUID, creating a new record in this
    /// cache if it wasn't observed before. Returns true if a new record
    /// was added, false if it already existed.
    /// </summary>
    public bool Observe(string prvName, string? prvGuid)
    {
      var existing = Find(prvName);
      if(existing != null && existing.ProviderGuid != prvGuid)
      {
        var eguid = existing.ProviderGuid ?? "NULL";
        var nguid = prvGuid ?? "NULL";
        throw new InvalidOperationException(
          $"Conflicting provider GUID declaration for '{prvName}': '{eguid}' vs '{nguid}'");
      }
      if(existing == null)
      {
        var pir = new ProviderInfoRow(_nextId++, prvName, prvGuid);
        Add(pir);
        _newlyAdded.Add(pir);
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Find a row by name, returning null if not found
    /// </summary>
    public ProviderInfoRow? Find(string name)
    {
      if(_byName.TryGetValue(name, out var row))
      {
        return row;
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Find a row by id, returning null if not found
    /// </summary>
    public ProviderInfoRow? Find(int id)
    {
      if(_byId.TryGetValue(id, out var row))
      {
        return row;
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Get a row by name, throwing a <see cref="KeyNotFoundException"/> if not found
    /// </summary>
    public ProviderInfoRow Get(string name)
    {
      if(_byName.TryGetValue(name, out var row))
      {
        return row;
      }
      else
      {
        throw new KeyNotFoundException(
          $"Unknown provider name '{name}'");
      }
    }

    /// <summary>
    /// Get a row by id, throwing a <see cref="KeyNotFoundException"/> if not found
    /// </summary>
    public ProviderInfoRow Get(int id)
    {
      if(_byId.TryGetValue(id, out var row))
      {
        return row;
      }
      else
      {
        throw new KeyNotFoundException(
          $"Unknown provider id {id}");
      }
    }

    /// <summary>
    /// The collection of newly added rows
    /// </summary>
    public IReadOnlyCollection<ProviderInfoRow> NewlyAdded => _newlyAdded;

    /// <summary>
    /// The total number of known provider rows
    /// </summary>
    public int Count => _byId.Count;

    /// <summary>
    /// Enumerate all rows in this cache (pre-existing and new)
    /// </summary>
    public IEnumerator<ProviderInfoRow> GetEnumerator()
    {
      return _byId.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
