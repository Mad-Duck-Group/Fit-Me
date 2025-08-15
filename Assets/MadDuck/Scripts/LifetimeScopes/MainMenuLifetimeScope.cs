using MadDuck.Scripts.Managers;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MadDuck.Scripts.LifetimeScopes
{
    public class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private BlockManager blockManager;
        
        protected override void Configure(IContainerBuilder builder)
        {
            var options =
                builder.RegisterMessagePipe(pipeOptions => pipeOptions.InstanceLifetime = InstanceLifetime.Scoped);
            builder.RegisterComponent(blockManager).AsImplementedInterfaces();
        }
        
    }
}