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

    public interface IScheduleTiming
    {
        bool IsDue(DateTime utcNow);
        bool NeedsToBeCheckedEverySecond();
    }

    public class ConfigurableScheduledEvent<TInvocable> : IScheduledEvent
        where TInvocable : IInvocable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _preventOverlappingKey;
        public IScheduleTiming Entry { get; }
        private Func<Task<bool>> When { get; } = () => Task.FromResult(true);
        public Func<IInvocable, Task> WrapperAction { get; }

        internal ConfigurableScheduledEvent(IScheduleTiming entry, IServiceScopeFactory scopeFactory, string overlappKey = null, Func<Task<bool>> predicate = null, Func<IInvocable, Task> wrapperAction = null)
        {
            Entry = entry ?? throw new ArgumentNullException($"{nameof(entry)} was null");
            When = predicate ?? When;
            WrapperAction = wrapperAction;
            _preventOverlappingKey = overlappKey;
            this._scopeFactory = scopeFactory ?? throw new ArgumentNullException($"{nameof(scopeFactory)} was null");
        }

        public async Task InvokeScheduledEvent()
        {
            if (await When())
            {
                /// This allows us to scope the scheduled IInvocable object
                /// and allow DI to inject it's dependencies.
                using (var scope = this._scopeFactory.CreateScope())
                {
                    if (scope.ServiceProvider.GetRequiredService<TInvocable>() is IInvocable invocable)
                    {
                        if (WrapperAction != null)
                            await WrapperAction(invocable);
                        else
                            await invocable.Invoke();
                    }
                }
            }
        }

        public bool IsDue(DateTime utcNow) => Entry.IsDue(utcNow);

        public bool NeedsToBeCheckedEverySecond() => Entry.NeedsToBeCheckedEverySecond();

        public string OverlappingUniqueIdentifier() => _preventOverlappingKey ?? string.Empty;

        public bool ShouldPreventOverlapping() => _preventOverlappingKey != null;
    }

    public interface IScheduleBuilder
    {
        IScheduleBuilder UseCustomTiming(IScheduleTiming timing);
        IScheduleBuilder UseDefaultTiming(Action<IScheduleInterval> configuration);
        IScheduleBuilder ExecuteOnlyWhen(Func<Task<bool>> predicate);
        IScheduleBuilder PreventOverlapping(string key);
        IScheduledEvent Build(IServiceScopeFactory scopeFactory);
        IScheduleBuilder WrapUsing(Func<IInvocable, Task> action);
    }

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