using Coravel.Invocable;
using Coravel.Scheduling.Schedule.Event.Configurable;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Coravel.Scheduling.Schedule.Event
{
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
}