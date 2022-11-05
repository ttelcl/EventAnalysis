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
  /// Base class for implementing XML value string transforms
  /// </summary>
  public abstract class XmlFieldTransform
  {
    /// <summary>
    /// Create a new XmlFieldTransform
    /// </summary>
    protected XmlFieldTransform(
      string name)
    {
      Name = name;
    }

    /// <summary>
    /// The name identifying this transform
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Transform the value as extracted from XML into its destination form
    /// </summary>
    public abstract string Transform(string value, XmlEventQuery caller);

  }
}
