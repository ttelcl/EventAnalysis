/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities
{
  /// <summary>
  /// A dictionary-like structure storing objects by their key,
  /// optionally delegating to another instance
  /// </summary>
  public class BackedMap<TItem, TKey> : KeyedMap<TItem, TKey>
    where TItem : class, IHasKey<TKey>
    where TKey : IEquatable<TKey>
  {
    /// <summary>
    /// Create a new BackedMap
    /// </summary>
    public BackedMap(KeyedMap<TItem, TKey>? backing, IEnumerable<TItem>? items = null)
      : base(null) // ! avoid incorrect initialization order
    {
      Backing = backing;
      if(items != null)
      {
        PutRange(items);
      }
    }

    /// <summary>
    /// The map that is being shadowed by this map.
    /// </summary>
    public KeyedMap<TItem, TKey>? Backing { get; }

    /// <summary>
    /// Find an item by key in this map. If not found, delegate to the
    /// backing map instead.
    /// </summary>
    public override TItem? Find(TKey key)
    {
      return base.Find(key) ?? Backing?.Find(key);
    }

    /// <summary>
    /// Equivalent to Put(item, false). Unconditionally put the item in this store,
    /// shadowing the value in the backing store.
    /// </summary>
    public override void Put(TItem item)
    {
      Put(item, false);
    }

    /// <summary>
    /// Put the item in this map, optionally only if missing from the backing store
    /// </summary>
    /// <param name="item">
    /// The item to store
    /// </param>
    /// <param name="onlyIfMissing">
    /// If true and an item with the same key already appears in the backing store,
    /// the item is not stored. If false, the item is stored in this map unconditionally.
    /// </param>
    /// <returns>
    /// True if the item was stored, false if not.
    /// </returns>
    public bool Put(TItem item, bool onlyIfMissing)
    {
      if(onlyIfMissing)
      {
        var key = item.Key;
        var old = Backing?.Find(key);
        if(old == null)
        {
          base.Put(item);
          return true;
        }
        else
        {
          return false;
        }
      }
      else
      {
        base.Put(item);
        return true;
      }
    }

  }
}