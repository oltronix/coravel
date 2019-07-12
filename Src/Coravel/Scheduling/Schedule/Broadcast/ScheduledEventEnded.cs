using System;
using Coravel.Events.Interfaces;
using Coravel.Scheduling.Schedule.Event;

namespace Coravel.Scheduling.Schedule.Broadcast
{
    public class ScheduledEventEnded : IEvent
    {
        public IScheduledEvent EndedEvent { get; private set; }
        public DateTime EndedAtUtc { get; private set; }

        public ScheduledEventEnded(IScheduledEvent endedEvent)
        {
            this.EndedEvent = endedEvent;
            this.EndedAtUtc = DateTime.UtcNow;
        }
    }
}