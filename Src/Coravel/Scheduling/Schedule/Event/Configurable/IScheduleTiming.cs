using System;

namespace Coravel.Scheduling.Schedule.Event.Configurable
{
    public interface IScheduleTiming
    {
        bool IsDue(DateTime utcNow);
        bool NeedsToBeCheckedEverySecond();
    }
}