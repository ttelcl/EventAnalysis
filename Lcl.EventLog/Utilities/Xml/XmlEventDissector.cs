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
  /// Extension of XmlDissector exposing properties for the common event log headers
  /// </summary>
  public class XmlEventDissector: XmlDissector
  {
    /// <summary>
    /// Create a new XmlEventDissector
    /// </summary>
    public XmlEventDissector(string xml)
      : base(xml)
    {
    }

    /// <summary>
    /// The record ID
    /// </summary>
    public long RecordId => EvalNotEmpty(":sys:EventRecordID").AsInteger()!.Value;

    /// <summary>
    /// The provider name
    /// </summary>
    public string Provider => EvalNotEmpty(":sys:Provider/@Name");

    /// <summary>
    /// The provider GUID (may be null)
    /// </summary>
    public Guid? ProviderGuid => Eval(":sys:Provider/@Guid").AsGuid();

    /// <summary>
    /// The event ID
    /// </summary>
    public int EventId => (int)EvalNotEmpty(":sys:EventID").AsInteger()!.Value;

    /// <summary>
    /// The event version
    /// </summary>
    public int Version => (int)EvalNotEmpty(":sys:Version").AsInteger()!.Value;

    /// <summary>
    /// The event log level
    /// </summary>
    public int Level => (int)EvalNotEmpty(":sys:Level").AsInteger()!.Value;

    /// <summary>
    /// The task ID
    /// </summary>
    public int TaskId => (int)EvalNotEmpty(":sys:Task").AsInteger()!.Value;

    /// <summary>
    /// The opcode
    /// </summary>
    public int Opcode => (int)EvalNotEmpty(":sys:Opcode").AsInteger()!.Value;

    /// <summary>
    /// The keywords bitmask
    /// </summary>
    public ulong? Keywords => Eval(":sys:Keywords").AsUnsigned();

    /// <summary>
    /// The event timestamp
    /// </summary>
    public DateTimeOffset Stamp => EvalNotEmpty(":sys:TimeCreated/@SystemTime").AsStamp()!.Value;

    /// <summary>
    /// The activity correlation ID (may be null)
    /// </summary>
    public Guid? ActivityId => Eval(":sys:Correlation/@ActivityID").AsGuid();

    /// <summary>
    /// The channel name - which should match the name of the channel this event was published to
    /// </summary>
    public string Channel => EvalNotEmpty(":sys:Channel");

    /// <summary>
    /// The computer name - which should match the data zone
    /// </summary>
    public string Machine => EvalNotEmpty(":sys:Computer");

    /// <summary>
    /// The security user SID (may be null)
    /// </summary>
    public string? UserSid => Eval(":sys:Security/@UserID").EmptyNull();
  }
}