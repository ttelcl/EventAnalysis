/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities.Xml
{
  /// <summary>
  /// The registry of XmlFieldTransform instances, making them accessible by name
  /// </summary>
  public class TransformRegistry
  {
    private readonly Dictionary<string, XmlFieldTransform> _registry;

    /// <summary>
    /// Create a new empty TransformRegistry. Consider using the <see cref="Default"/>
    /// singleton instead.
    /// </summary>
    public TransformRegistry()
    {
      _registry = new Dictionary<string, XmlFieldTransform>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A singleton TransformRegistry with transforms "notempty" and "unsigned" preregistered.
    /// </summary>
    public static TransformRegistry Default = 
      new TransformRegistry()
      .Register<TrxNotEmpty>()
      .Register<TrxUnsigned>();

    /// <summary>
    /// Register a previously created XmlFieldTransform instance
    /// </summary>
    public TransformRegistry Register(XmlFieldTransform trx)
    {
      _registry[trx.Name] = trx;
      return this;
    }

    /// <summary>
    /// Create a new XmlFieldTransform subclass instance and register it
    /// </summary>
    public TransformRegistry Register<Trx>()
      where Trx : XmlFieldTransform, new()
    {
      return Register(new Trx());
    }

    /// <summary>
    /// Lookup a tranform by name, returning null if not found
    /// </summary>
    public XmlFieldTransform? Find(string transformName)
    {
      if(_registry.TryGetValue(transformName, out var transform) && transform!=null)
      {
        return transform;
      }
      else
      {
        return null;
      }
    }

  }
}
