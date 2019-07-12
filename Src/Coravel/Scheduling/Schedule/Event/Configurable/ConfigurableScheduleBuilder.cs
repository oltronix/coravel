using Coravel.Invocable;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Coravel.Scheduling.Schedule.Event.Configurable
{
    public class ConfigurableScheduleBuilder<TInvocable> : IScheduleBuilder
        where TInvocable : IInvocable
    {
        private IScheduleTiming _scheduleTiming;
        private Action<IScheduleInterval> _defaultScheduleAction;
        private string _overlappingKey;
        private Func<Task<bool>> _predicate;
        private Func<IInvocable, Task> _wrapperAction;

        public IScheduleBuilder UseCustomTiming(IScheduleTiming timing)
        {
            _scheduleTiming = timing;
            return this;
        }

        public IScheduleBuilder UseDefaultTiming(Action<IScheduleInterval> configuration)
        {
            _defaultScheduleAction = configuration;
            return this;
        }

        public IScheduleBuilder PreventOverlapping(string key)
        {
            _overlappingKey = key;
            return this;
        }

        public IScheduleBuilder ExecuteOnlyWhen(Func<Task<bool>> predicate)
        {
            _predicate = predicate;
            return this;
        }

        public IScheduleBuilder WrapUsing(Func<IInvocable, Task> action)
        {
            _wrapperAction = action;
            return this;
        }

        public IScheduledEvent Build(IServiceScopeFactory scopeFactory)
        {
            if (_scheduleTiming == null)
            {
                var standardEvent = ScheduledEvent.WithInvocable<TInvocable>(scopeFactory);
                if (_overlappingKey != null)
                    standardEvent.PreventOverlapping(_overlappingKey);
                if (_predicate != null)
                    standardEvent.When(_predicate);
                _defaultScheduleAction?.Invoke(standardEvent);
                return standardEvent;
            }
            return new ConfigurableScheduledEvent<TInvocable>(_scheduleTiming, scopeFactory, _overlappingKey, _predicate, _wrapperAction);
        }
    }
}