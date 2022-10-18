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
  /// A dictionary-like structure storing objects by their associated key.
  /// </summary>
  public class KeyedMap<TItem, TKey> 
    where TItem : class, IHasKey<TKey>
    where TKey : IEquatable<TKey>
  {
    private readonly Dictionary<TKey, TItem> _items;

    /// <summary>
    /// Create a new KeyedMap
    /// </summary>
    public KeyedMap(IEnumerable<TItem>? items = null)
    {
      _items = new Dictionary<TKey, TItem>();
      if(items != null)
      {
        PutRange(items);
      }
    }

    /// <summary>
    /// Find an item from this map, returning null if not found
    /// </summary>
    public TItem? this[TKey key] {
      get => Find(key);
    }

    /// <summary>
    /// Find an item from this map with the same key as the <paramref name="keyItem"/>,
    /// returning null if not found
    /// </summary>
    public TItem? this[TItem keyItem] {
      get => Find(keyItem.Key);
    }

    /// <summary>
    /// Find an item from this map, returning null if not found
    /// </summary>
    public virtual TItem? Find(TKey key)
    {
      if(_items.TryGetValue(key, out TItem? item))
      {
        return item;
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Put an item in the map (overwriting items if they already existed)
    /// </summary>
    public virtual void Put(TItem item)
    {
      _items[item.Key] = item;
    }

    /// <summary>
    /// Remove an item from this map if it existed.
    /// </summary>
    public virtual void Remove(TKey key)
    {
      _items.Remove(key);
    }

    /// <summary>
    /// Put zero or more items into this map
    /// </summary>
    public void PutRange(IEnumerable<TItem> items)
    {
      foreach(var item in items)
      {
        Put(item);
      }
    }

    /// <summary>
    /// Get the items stored in this map
    /// </summary>
    public virtual IEnumerable<TItem> Items => _items.Values;
  }
}
