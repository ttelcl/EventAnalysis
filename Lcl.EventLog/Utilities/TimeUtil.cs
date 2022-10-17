﻿/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities
{
  /// <summary>
  /// Utilities for converting time stamps
  /// </summary>
  public static class TimeUtil
  {
    /// <summary>
    /// The unix epoch (1970-01-01 00:00:00Z) as DateTimeOffset
    /// </summary>
    public static readonly DateTimeOffset EpochDto =
      new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The unix epoch (1970-01-01 00:00:00Z) as UTC DateTime
    /// </summary>
    public static readonly DateTime Epoch =
      new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// The tick count at the unix epoch (1970-01-01 00:00:00Z)
    /// </summary>
    public const long TicksAtEpoch = 621355968000000000L;

    /// <summary>
    /// The number of ticks (0.0000001 seconds; 100 nanoseconds) elapsed since
    /// the unix epoch (1970-01-01 00:00:00Z) at the given time.
    /// </summary>
    public static long TicksSinceEpoch(this DateTimeOffset dto)
    {
      return dto.UtcTicks - TicksAtEpoch;
    }

    /// <summary>
    /// The number of ticks (0.0000001 seconds; 100 nanoseconds) elapsed since
    /// the unix epoch (1970-01-01 00:00:00Z) at the given time. The argument
    /// must have a kind of Utc or Local. Local timestamps are converted to UTC
    /// first.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the argument's Kind is Unspecified.
    /// </exception>
    public static long TicksSinceEpoch(this DateTime dt)
    {
      if(dt.Kind == DateTimeKind.Unspecified)
      {
        throw new ArgumentException(
          $"Expecting a timestamp in 'UTC' or 'local' time, not 'unspecified'");
      }
      return dt.ToUniversalTime().Ticks - TicksAtEpoch;
    }

    /// <summary>
    /// The UTC DateTimeOffset <paramref name="epochTicks"/> ticks after
    /// the unix epoch (1970-01-01 00:00:00Z)
    /// </summary>
    public static DateTimeOffset EpochDateTimeOffset(this long epochTicks)
    {
      return EpochDto.AddTicks(epochTicks);
    }

    /// <summary>
    /// The UTC DateTime <paramref name="epochTicks"/> ticks after
    /// the unix epoch (1970-01-01 00:00:00Z)
    /// </summary>
    public static DateTime EpochDateTime(this long epochTicks)
    {
      return Epoch.AddTicks(epochTicks);
    }

    /// <summary>
    /// Convert a timestamp to a string in "yyyyMMdd-HHmmss-fffffff"
    /// or "yyyyMMdd-HHmmss" form
    /// </summary>
    /// <param name="dt">
    /// The time stamp. The conversion uses whatever timezone is implied in
    /// the argument's Kind.
    /// </param>
    /// <param name="full">
    /// use the longer format
    /// </param>
    public static string ToTimeString(this DateTime dt, bool full = true)
    {
      return dt.ToString(
        full ? "yyyyMMdd-HHmmss-fffffff" : "yyyyMMdd-HHmmss",
        CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parse a datetime string in "yyyyMMdd-HHmmss-fffffff"
    /// or "yyyyMMdd-HHmmss" form. The Kind has to be specified explicitly. 
    /// </summary>
    public static DateTime FromTimeString(this string txt, DateTimeKind kind)
    {
      var rgx = @"(\d{8}-\d{6})(-\d{7})?";
      var m = Regex.Match(txt, rgx);
      if(!m.Success)
      {
        throw new ArgumentException(
          $"Unsupported time string format");
      }
      var dt = DateTime.ParseExact(
        txt, 
        new[] { "yyyyMMdd-HHmmss-fffffff", "yyyyMMdd-HHmmss" }, 
        CultureInfo.InvariantCulture,
        DateTimeStyles.None);
      // Supporting .net standard 2.0 means we don't have SpecifyKind()
      return new DateTime(dt.Ticks, kind);
    }

  }
}
