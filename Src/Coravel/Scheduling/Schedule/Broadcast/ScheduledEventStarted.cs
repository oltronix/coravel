using System;
using Coravel.Events.Interfaces;
using Coravel.Scheduling.Schedule.Event;

namespace Coravel.Scheduling.Schedule.Broadcast
{
    public class ScheduledEventStarted : IEvent
    {
        public IScheduledEvent StartedEvent { get; private set; }
        public DateTime StartedAtUtc { get; private set; }

        public ScheduledEventStarted(IScheduledEvent startedEvent)
        {
            this.StartedEvent = startedEvent;
            this.StartedAtUtc = DateTime.UtcNow;
        }
    }
}