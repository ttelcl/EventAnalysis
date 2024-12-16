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
/// Task info as it appears in metadata dumps
/// </summary>
public class TaskDto
{
  /// <summary>
  /// Create a new TaskDto
  /// </summary>
  public TaskDto(
    [JsonProperty("event")] int eventId,
    int version,
    int task,
    string? description,
    IEnumerable<OperationDto> operations)
  {
    EventId = eventId;
    EventVersion = version;
    TaskId = task;
    TaskDescription = description;
    Operations = operations.ToList();
  }

  /// <summary>
  /// The event ID
  /// </summary>
  [JsonProperty("event")]
  public int EventId { get; }

  /// <summary>
  /// The event version
  /// </summary>
  [JsonProperty("version")]
  public int EventVersion { get; }

  /// <summary>
  /// The task ID
  /// </summary>
  [JsonProperty("task")]
  public int TaskId { get; }

  /// <summary>
  /// The task description, if available
  /// </summary>
  [JsonProperty("description")]
  public string? TaskDescription { get; }

  /// <summary>
  /// The collections of operations that use this task
  /// </summary>
  [JsonProperty("operations")]
  public List<OperationDto> Operations { get; }

  internal class Scaffold
  {
    public Scaffold(TaskInfoRow row)
    {
      Dto = new TaskDto(
        row.EventId,
        row.EventVersion,
        row.TaskId,
        row.TaskDescription,
        new List<OperationDto>());
      Row = row;
    }

    public TaskDto Dto { get; }

    public TaskInfoRow Row { get; }
  }
}
