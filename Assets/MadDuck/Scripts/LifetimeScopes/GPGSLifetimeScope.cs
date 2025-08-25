using MadDuck.Scripts.GPGS;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MadDuck.Scripts.LifetimeScopes
{
    [RequireComponent(typeof(GPGSManager))]
    public class GPGSLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // var options =
            //     builder.RegisterMessagePipe();
            var manager = GetComponent<GPGSManager>();
            builder.RegisterComponent(manager).AsImplementedInterfaces().AsSelf();
            builder.RegisterBuildCallback(x => GlobalMessagePipe.SetProvider(x.AsServiceProvider()));
        }
    }
}