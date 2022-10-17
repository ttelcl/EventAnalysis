/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using Lcl.EventLog.Utilities;

namespace UnitTest.Lcl.EventLog
{
  public class TimeUtilTests
  {
    private readonly ITestOutputHelper _output;

    public TimeUtilTests(ITestOutputHelper output)
    {
      _output = output;
    }

    [Fact]
    public void EpochIsConsistent()
    {
      var dl = new DateTime(2022, 10, 17, 11, 54, 0, DateTimeKind.Local);
      var du = new DateTime(2022, 10, 17, 11, 54, 0, DateTimeKind.Utc);
      Assert.Equal(TimeUtil.TicksAtEpoch, TimeUtil.Epoch.Ticks);
      Assert.Equal(TimeUtil.TicksAtEpoch, TimeUtil.EpochDto.Ticks);
      Assert.Equal(TimeUtil.EpochDto.DateTime, TimeUtil.Epoch);
      Assert.Equal(TimeUtil.EpochDto.UtcDateTime, TimeUtil.Epoch);
      Assert.Equal(DateTimeKind.Local, dl.Kind);
      Assert.Equal(638015936400000000L-TimeUtil.TicksAtEpoch, dl.TicksSinceEpoch());
      Assert.Equal(638016044400000000L-TimeUtil.TicksAtEpoch, du.TicksSinceEpoch());
      var dlx = dl.TicksSinceEpoch().EpochDateTime();
      Assert.Equal(DateTimeKind.Utc, dlx.Kind);
      Assert.Equal(new DateTime(2022, 10, 17, 8, 54, 0, DateTimeKind.Utc), dlx);
    }

    [Fact]
    public void EpochTimeSerialization()
    {
      var dl1 = new DateTime(2022, 10, 17, 11, 54, 0, 42, DateTimeKind.Local);
      var dl2 = new DateTime(2022, 10, 17, 11, 54, 0, 999, DateTimeKind.Local);
      var du1 = new DateTime(2022, 10, 17, 11, 54, 0, 42, DateTimeKind.Utc);
      var du2 = new DateTime(2022, 10, 17, 11, 54, 0, 0, DateTimeKind.Utc);
      var dto1 = new DateTimeOffset(2022, 10, 17, 11, 54, 0, 42, TimeSpan.Zero);
      var dto2 = new DateTimeOffset(2022, 10, 17, 11, 54, 0, 42, TimeSpan.FromHours(1));

      // ToTimeString:
      Assert.Equal("20221017-115400-0420000", dl1.ToTimeString(true));
      Assert.Equal("20221017-115400", dl1.ToTimeString(false));
      Assert.Equal("20221017-115400-9990000", dl2.ToTimeString(true));
      // It truncates, not rounds:
      Assert.Equal("20221017-115400", dl2.ToTimeString(false)); 
      // DateTimekind is ignored:
      Assert.Equal("20221017-115400-0420000", du1.ToTimeString(true));
      Assert.Equal("20221017-115400", du1.ToTimeString(false));
      // Long form always includes the fractional part, even when 0
      Assert.Equal("20221017-115400-0000000", du2.ToTimeString(true));
      // With DateTimeOffset. Time zone is ignored, as documented.
      Assert.Equal("20221017-115400-0420000", dto1.ToTimeString(true));
      Assert.Equal("20221017-115400-0420000", dto2.ToTimeString(true));

      // ParseDateTime
      var dl3 = "20221017-115400-0420000".ParseDateTime(DateTimeKind.Local);
      Assert.Equal(dl1, dl3);
      Assert.Equal(DateTimeKind.Local, dl3.Kind);
      var du3 = "20221017-115400-0420000".ParseDateTime(DateTimeKind.Utc);
      Assert.Equal(DateTimeKind.Utc, du3.Kind);
      Assert.Equal(du1, du3);
    }

  }
}
