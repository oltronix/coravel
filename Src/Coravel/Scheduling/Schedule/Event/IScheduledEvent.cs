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

    public class AsyncFuncInvocable : IInvocable
    {
        public AsyncFuncInvocable(Func<Task> func)
        {
            Func = func;
        }

        public Func<Task> Func { get; }

        public Task Invoke() => Func();
    }

    public class ConfigurableScheduledEvent<TInvocable> : IScheduledEvent
        where TInvocable : IInvocable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _preventOverlappingKey;
        public IScheduleTiming Entry { get; }
        private Func<Task<bool>> When { get; } = () => Task.FromResult(true);

        internal ConfigurableScheduledEvent(IScheduleTiming entry, IServiceScopeFactory scopeFactory, string overlappKey = null, Func<Task<bool>> predicate = null)
        {
            Entry = entry ?? throw new ArgumentNullException($"{nameof(entry)} was null");
            When = predicate ?? When;
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
        IScheduleBuilder PreventOverlapping(string key);
        IScheduledEvent Build(IServiceScopeFactory scopeFactory);
    }

    public class ConfigurableScheduleBuilder<TInvocable> : IScheduleBuilder
        where TInvocable : IInvocable
    {
        private IScheduleTiming _scheduleTiming;
        private Action<IScheduleInterval> _defaultScheduleAction;
        private string _overlappingKey;
        private Func<Task<bool>> _predicate;

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

        public IScheduledEvent Build(IServiceScopeFactory scopeFactory)
        {
            if (_scheduleTiming == null)
            {
                var standardEvent = ScheduledEvent.WithInvocable<TInvocable>(scopeFactory);
                _defaultScheduleAction?.Invoke(standardEvent);
                return standardEvent;
            }
            return new ConfigurableScheduledEvent<TInvocable>(_scheduleTiming, scopeFactory, _overlappingKey, _predicate);
        }
    }
}