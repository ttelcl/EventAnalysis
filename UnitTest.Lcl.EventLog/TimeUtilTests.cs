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
      Assert.Equal(TimeUtil.TicksAtEpoch, TimeUtil.Epoch.Ticks);
      Assert.Equal(TimeUtil.TicksAtEpoch, TimeUtil.EpochDto.Ticks);
      Assert.Equal(TimeUtil.EpochDto.DateTime, TimeUtil.Epoch);
      Assert.Equal(TimeUtil.EpochDto.UtcDateTime, TimeUtil.Epoch);
    }

  }
}
