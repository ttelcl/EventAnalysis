using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

using Xunit;
using Xunit.Abstractions;

using Lcl.EventLog.Jobs.Database;
using Lcl.EventLog.Utilities;
using Lcl.EventLog.Utilities.Xml;

namespace UnitTest.Lcl.EventLog
{
  public class XmlTests
  {
    private readonly ITestOutputHelper _output;

    public XmlTests(ITestOutputHelper output)
    {
      _output = output;
    }

    private const string __sample1 = @"
<Event xmlns=""http://schemas.microsoft.com/win/2004/08/events/event"">
  <System>
    <Provider Name=""Microsoft-Windows-Security-Auditing"" Guid=""{54849625-5478-4994-a5ba-3e3b0328c30d}"" />
    <EventID>4689</EventID>
    <Version>0</Version>
    <Level>0</Level>
    <Task>13313</Task>
    <Opcode>0</Opcode>
    <Keywords>0x8020000000000000</Keywords>
    <TimeCreated SystemTime=""2022-10-27T17:40:34.3224373Z"" />
    <EventRecordID>6761488</EventRecordID>
    <Correlation />
    <Execution ProcessID=""4"" ThreadID=""36308"" />
    <Channel>Security</Channel>
    <Computer>FOOBAR2018</Computer>
    <Security />
  </System>
  <EventData>
    <Data Name=""SubjectUserSid"">S-1-5-FOO-BAR</Data>
    <Data Name=""SubjectUserName"">foobar</Data>
    <Data Name=""SubjectDomainName"">FOOBAR2018</Data>
    <Data Name=""SubjectLogonId"">0xd994e</Data>
    <Data Name=""Status"">0x0</Data>
    <Data Name=""ProcessId"">0xa3fc</Data>
    <Data Name=""ProcessName"">C:\Program Files (x86)\Google\Chrome\Application\chrome.exe</Data>
  </EventData>
</Event>";

    [Fact]
    public void XmlDissectorTest1()
    {
      var dissector = new XmlDissector(__sample1);
      Assert.Equal("4689", dissector.Eval("/Event/System/EventID"));
      Assert.Equal("4689", dissector.EvalSystem("EventID"));
      Assert.Equal("4689", dissector.Eval(":sys:EventID"));
      Assert.Equal("", dissector.Eval("/Event/System/DoesNotExist"));
      Assert.Equal("", dissector.EvalSystem("DoesNotExist"));
      Assert.Equal("", dissector.Eval(":sys:DoesNotExist"));
      Assert.Equal("Microsoft-Windows-Security-Auditing", dissector.Eval("/Event/System/Provider/@Name"));
      Assert.Equal("Microsoft-Windows-Security-Auditing", dissector.EvalSystem("Provider/@Name"));
      Assert.Equal("Microsoft-Windows-Security-Auditing", dissector.Eval(":sys:Provider/@Name"));
      Assert.Equal("Microsoft-Windows-Security-Auditing", dissector.EvalSystem("Provider", "Name"));
      Assert.Equal("{54849625-5478-4994-a5ba-3e3b0328c30d}", dissector.Eval("/Event/System/Provider/@Guid"));
      Assert.Equal("{54849625-5478-4994-a5ba-3e3b0328c30d}", dissector.EvalSystem("Provider", "Guid"));
      Assert.Equal("0xa3fc", dissector.Eval("/Event/EventData/Data[@Name='ProcessId']"));
      Assert.Equal("0xa3fc", dissector.EvalData("ProcessId"));
      Assert.Equal("0xa3fc", dissector.Eval(":data:ProcessId"));
      Assert.Equal(41980L, dissector.Eval("/Event/EventData/Data[@Name='ProcessId']").AsInteger());
      Assert.Equal(41980L, dissector.EvalData("ProcessId").AsInteger());
      Assert.Equal(0x8020000000000000UL, dissector.Eval("/Event/System/Keywords").AsUnsigned());
      Assert.Equal(0x8020000000000000UL, dissector.EvalSystem("Keywords").AsUnsigned());
    }

    private const string __sample2 = @"
<Event xmlns=""http://schemas.microsoft.com/win/2004/08/events/event"">
  <System>
    <Provider Name=""Microsoft-Windows-Servicing"" Guid=""{bd12f3b8-fc40-4a61-a307-b7a013a069c1}"" />
    <EventID>1</EventID>
    <Version>0</Version>
    <Level>0</Level>
    <Task>1</Task>
    <Opcode>0</Opcode>
    <Keywords>0x8000000000000000</Keywords>
    <TimeCreated SystemTime=""2022-10-18T07:19:20.9000277Z"" />
    <EventRecordID>419</EventRecordID>
    <Correlation />
    <Execution ProcessID=""34276"" ThreadID=""7440"" />
    <Channel>Setup</Channel>
    <Computer>FOOBAR2018</Computer>
    <Security UserID=""S-1-5-18"" />
  </System>
  <UserData>
    <CbsPackageInitiateChanges xmlns=""http://manifests.microsoft.com/win/2004/08/windows/setup_provider"">
      <PackageIdentifier>KB5016616</PackageIdentifier>
      <InitialPackageState>5080</InitialPackageState>
      <InitialPackageStateTextized>Superseded</InitialPackageStateTextized>
      <IntendedPackageState>5000</IntendedPackageState>
      <IntendedPackageStateTextized>Absent</IntendedPackageStateTextized>
      <Client>CbsTask</Client>
    </CbsPackageInitiateChanges>
  </UserData>
</Event>";

    [Fact]
    public void XmlDissectorTest2()
    {
      var dissector = new XmlDissector(__sample2);
      Assert.Equal("KB5016616", dissector.Eval("/Event/UserData/*/PackageIdentifier"));
      Assert.Equal("KB5016616", dissector.EvalUserData("PackageIdentifier"));
      Assert.Equal("KB5016616", dissector.Eval(":udata:PackageIdentifier"));
    }

    [Fact]
    public void XmlQueryTest()
    {
      var transforms = TransformRegistry.Default;
      var dissector = new XmlDissector(__sample1);
      var queryUserId = new XmlEventQuery(
        "userid",
        ":data:SubjectUserSid");
      var userid = queryUserId.Evaluate(dissector, transforms);
      Assert.Equal("S-1-5-FOO-BAR", userid);
      var queryPidRaw = new XmlEventQuery(
        "pid-raw",
        ":data:ProcessId",
        "");
      var pidRaw = queryPidRaw.Evaluate(dissector, transforms);
      Assert.Equal("0xa3fc", pidRaw);
      var queryPidDecimal = new XmlEventQuery(
        "pid-dec",
        ":data:ProcessId",
        "unsigned,notempty");
      var pidDecimal = queryPidDecimal.Evaluate(dissector, transforms);
      Assert.Equal("41980", pidDecimal);
    }

    [Fact]
    public void XmlQueryTestJson()
    {
      var transforms = TransformRegistry.Default;
      var dissector = new XmlDissector(__sample1);
      var queryUserId = XmlEventQuery.FromJson(@"{
  ""label"": ""userid"", 
  ""expression"": "":data:SubjectUserSid""
}");
      var userid = queryUserId.Evaluate(dissector, transforms);
      Assert.Equal("S-1-5-FOO-BAR", userid);
      var queryPidRaw = XmlEventQuery.FromJson(@"{
  ""label"": ""pid-raw"", 
  ""expression"": "":data:ProcessId"",
  ""transforms"": """"
}");
      var pidRaw = queryPidRaw.Evaluate(dissector, transforms);
      Assert.Equal("0xa3fc", pidRaw);
      var queryPidDecimal = XmlEventQuery.FromJson(@"{
  ""label"": ""pid-raw"", 
  ""expression"": "":data:ProcessId"",
  ""transforms"": ""unsigned,notempty""
}");
      var pidDecimal = queryPidDecimal.Evaluate(dissector, transforms);
      Assert.Equal("41980", pidDecimal);
    }

  }
}

