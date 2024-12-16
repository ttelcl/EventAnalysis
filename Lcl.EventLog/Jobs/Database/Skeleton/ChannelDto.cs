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
/// Description of ChannelDto
/// </summary>
public class ChannelDto
{
  /// <summary>
  /// Create a new ChannelDto
  /// </summary>
  public ChannelDto(
    string job,
    string channel,
    string machine,
    IEnumerable<ProviderDto> providers)
  {
    Job = job;
    Channel = channel;
    Machine = machine;
    Providers = providers.ToList();
  }

  /// <summary>
  /// The job name chosen for this channel
  /// </summary>
  [JsonProperty("job")]
  public string Job { get; }

  /// <summary>
  /// The event channel name
  /// </summary>
  [JsonProperty("channel")]
  public string Channel { get; }

  /// <summary>
  /// The name of the computer whose event log is being read
  /// </summary>
  [JsonProperty("machine")]
  public string Machine { get; }

  /// <summary>
  /// The collection of providers that are associated with this channel
  /// (and nested in those: the task and operation descriptions)
  /// </summary>
  [JsonProperty("providers")]
  public List<ProviderDto> Providers { get; }

  /// <summary>
  /// Create a ChannelDto instance from an EventJob, loading
  /// it from the job's inner database.
  /// </summary>
  public static ChannelDto FromJob(
    EventJob job)
  {
    if(!job.HasDbV2)
    {
      return new ChannelDto(
        job.Configuration.Name,
        job.Configuration.Channel,
        job.Zone.Machine,
        new List<ProviderDto>());
    }
    using var odb = job.OpenInnerDatabase2(false);
    var opScaffolds =
      odb.AllOperationInfoRows()
      .Select(opRow => new OperationDto.Scaffold(opRow))
      .ToDictionary(scaffold => scaffold.Row.Key);
    var taskScaffolds =
      odb.AllTaskInfoRows()
      .Select(row => new TaskDto.Scaffold(row))
      .ToDictionary(scaffold => scaffold.Row.Key);
    var providerScaffolds =
      odb.AllProviderInfoRows()
      .Select(row => new ProviderDto.Scaffold(row))
      .ToDictionary(scaffold => scaffold.Row.ProviderId);
    foreach(var opScaffold in opScaffolds.Values)
    {
      var taskScaffold = taskScaffolds[opScaffold.Row.TaskKey];
      taskScaffold.Dto.Operations.Add(opScaffold.Dto);
    }
    foreach(var taskScaffold in taskScaffolds.Values)
    {
      var providerScaffold = providerScaffolds[taskScaffold.Row.ProviderId];
      providerScaffold.Dto.Tasks.Add(taskScaffold.Dto);
    }
    return new ChannelDto(
      job.Configuration.Name,
      job.Configuration.Channel,
      job.Zone.Machine,
      providerScaffolds.Values.Select(scaffold => scaffold.Dto));
  }

}
