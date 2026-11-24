using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class FlexibleShiftTests
{
 static DateTime D=new(2026,11,24);
 static Shift S()=>new(){Flexible=true,Start=new(8,0,0),LatestStart=new(10,0,0),End=new(18,0,0),RequiredMinutes=480,GraceMinutes=0,MaxLateMinutes=60};
 [Theory][InlineData(8,16)][InlineData(9,17)][InlineData(10,18)]public void InsideWindowHasNoLatenessAndOwnExpectedEnd(int arrival,int departure){var r=AttendanceCalculator.Calculate(S(),D,D.AddHours(arrival),D.AddHours(departure));Assert.Equal(0,r.LateMinutes);Assert.Equal(0,r.EarlyLeaveMinutes);Assert.Equal(AttendanceStatus.Present,r.Status);Assert.Equal(480,r.WorkedMinutes);}
 [Fact]public void LateIsMeasuredFromLatestArrivalAndDurationStillRequired(){var r=AttendanceCalculator.Calculate(S(),D,D.AddHours(10).AddMinutes(30),D.AddHours(18));Assert.Equal(30,r.LateMinutes);Assert.Equal(30,r.EarlyLeaveMinutes);Assert.Equal(AttendanceStatus.Late,r.Status);}
 [Fact]public void EarlyArrivalDoesNotMoveRequiredEndEarlier(){var r=AttendanceCalculator.Calculate(S(),D,D.AddHours(7),D.AddHours(15));Assert.Equal(60,r.EarlyLeaveMinutes);Assert.Equal(0,r.OvertimeMinutes);}
 [Fact]public void OvertimeUsesPersonalEndWithExistingThreshold(){var r=AttendanceCalculator.Calculate(S(),D,D.AddHours(9),D.AddHours(17).AddMinutes(45));Assert.Equal(45,r.OvertimeMinutes);Assert.Equal(0,r.LateMinutes);}
 [Fact]public void InvalidAndOvernightFlexDefinitionIsRejected(){var s=S();s.RequiredMinutes=1000;Assert.Throws<DomainException>(()=>AttendanceCalculator.Calculate(s,D,D.AddHours(9),D.AddHours(17)));s=S();s.End=new(6,0,0);Assert.Throws<DomainException>(()=>AttendanceCalculator.Calculate(s,D,D.AddHours(9),D.AddHours(17)));}
}
