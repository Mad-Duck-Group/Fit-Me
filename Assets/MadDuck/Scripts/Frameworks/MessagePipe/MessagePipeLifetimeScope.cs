using System;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace MadDuck.Scripts.Frameworks.MessagePipe
{
    public class MessagePipeLifetimeScope : LifetimeScope
    {
        public static event Action OnGlobalMessagePipeSet;
        protected override void Configure(IContainerBuilder builder)
        {
            // RegisterMessagePipe returns options.
            builder.RegisterMessagePipe();

            // Setup GlobalMessagePipe to enable diagnostics window and global function
            builder.RegisterBuildCallback(c =>
            {
                GlobalMessagePipe.SetProvider(c.AsServiceProvider());
                OnGlobalMessagePipeSet?.Invoke();
            });

            // RegisterMessageHandlerFilter: Register for filter, also exists RegisterAsyncMessageHandlerFilter, Register(Async)RequestHandlerFilter
            //builder.RegisterMessageHandlerFilter<MyFilter<int>>();
            //builder.RegisterEntryPoint<MessagePipeDemo>(Lifetime.Singleton);
        }
    }
}
