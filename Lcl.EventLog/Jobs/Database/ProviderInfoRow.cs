/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Row in the Providers table (v2 model)
  /// </summary>
  public class ProviderInfoRow
  {
    /// <summary>
    /// Create a new ProvidersRow
    /// </summary>
    public ProviderInfoRow(
      int prvid,
      string prvname,
      string? prvguid)
    {
      ProviderId = prvid;
      ProviderName = prvname;
      ProviderGuid = prvguid;
    }

    /// <summary>
    /// The DB-internal ID for the provider
    /// </summary>
    [JsonProperty("prvid")]
    public int ProviderId { get; }

    /// <summary>
    /// The provider name
    /// </summary>
    [JsonProperty("prvname")]
    public string ProviderName { get; }

    /// <summary>
    /// The provider GUID as string (possibly null)
    /// </summary>
    [JsonProperty("prvguid")]
    public string? ProviderGuid { get; }

  }
}