using Coravel.Invocable;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Coravel.Scheduling.Schedule.Event.Configurable
{
    public interface IScheduleBuilder
    {
        IScheduleBuilder UseCustomTiming(IScheduleTiming timing);
        IScheduleBuilder UseDefaultTiming(Action<IScheduleInterval> configuration);
        IScheduleBuilder ExecuteOnlyWhen(Func<Task<bool>> predicate);
        IScheduleBuilder PreventOverlapping(string key);
        IScheduledEvent Build(IServiceScopeFactory scopeFactory);
        IScheduleBuilder WrapUsing(Func<IInvocable, Task> action);
    }
}