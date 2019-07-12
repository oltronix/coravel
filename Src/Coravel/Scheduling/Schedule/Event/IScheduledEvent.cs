using Coravel.Invocable;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Coravel.Scheduling.Schedule.Event
{
    public interface IScheduledEvent
    {
        bool IsDue(DateTime utcNow);
        Task InvokeScheduledEvent();
        bool ShouldPreventOverlapping();
        string OverlappingUniqueIdentifier();
        bool NeedsToBeCheckedEverySecond();
    }    
}