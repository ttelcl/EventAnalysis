/*
 * (c) 2024  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.EventLog.Jobs.Database.Skeleton;

/// <summary>
/// Operation info as it appears in metadata dumps
/// </summary>
public class OperationDto
{
  /// <summary>
  /// Create a new OperationDto
  /// </summary>
  public OperationDto(
    int id,
    string? description)
  {
    OperationId = id;
    OperationDescription = description;
  }

  /// <summary>
  /// The operation ID code
  /// </summary>
  [JsonProperty("id")]
  public int OperationId { get; }

  /// <summary>
  /// The operation description
  /// </summary>
  [JsonProperty("description")]
  public string? OperationDescription { get; }

  internal class Scaffold
  {
    public Scaffold(OperationInfoRow row)
    {
      Row = row;
      Dto = new OperationDto(
        row.OperationId,
        row.OperationDescription);
    }

    public OperationDto Dto { get; }

    public OperationInfoRow Row { get; }
  }
}
