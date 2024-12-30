/*
 * (c) 2024  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Net.Mime.MediaTypeNames;

namespace Lcl.EventLog.Utilities.Xml;

/// <summary>
/// Transforming a timestamp string into another format
/// via an <see cref="DateTimeOffset"/> intermediate
/// </summary>
public class TrxStamp: XmlFieldTransform
{
  /// <summary>
  /// Create a new TrxStamp
  /// </summary>
  private TrxStamp(string name, string format) : base(name)
  {
    Format=format;
  }

  /// <summary>
  /// A Compact format transformer ("yyyyMMdd-HHmmss-fffffff")
  /// </summary>
  public static TrxStamp Compact { get; } =
    new TrxStamp("time2stamp", "yyyyMMdd-HHmmss-fffffff");

  /// <summary>
  /// A Compact format transformer ("yyyyMMdd-HHmmss-fff")
  /// </summary>
  public static TrxStamp CompactMillis { get; } =
    new TrxStamp("time2millistamp", "yyyyMMdd-HHmmss-fff");

  /// <summary>
  /// A date format transformer ("yyyy-MM-dd")
  /// </summary>
  public static TrxStamp Date { get; } =
    new TrxStamp("time2date", "yyyy-MM-dd");

  /// <summary>
  /// The <see cref="DateTimeOffset"/> format string
  /// </summary>
  public string Format { get; set; }

  /// <inheritdoc/>
  public override string Transform(
    string value, XmlEventQuery caller)
  {
    if(String.IsNullOrEmpty(value))
    {
      return String.Empty;
    }
    var dto = DateTimeOffset.Parse(value, null, DateTimeStyles.RoundtripKind);
    return dto.ToString(Format);
  }

}
