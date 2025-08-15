using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MadDuck.Scripts.LifetimeScopes
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameManager gameManager;
        protected override void Configure(IContainerBuilder builder)
        {
            var options =
                builder.RegisterMessagePipe(pipeOptions => pipeOptions.InstanceLifetime = InstanceLifetime.Scoped);
            builder.RegisterComponent(gameManager).AsImplementedInterfaces();
        }
    }
}