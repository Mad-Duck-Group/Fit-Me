using MadDuck.Scripts.UIs.Panels;
using MessagePipe;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MadDuck.Scripts.LifetimeScopes
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField, HideDuplicateReferenceBox, HideLabel]
        private UIPanelController panelController = new();
        protected override void Configure(IContainerBuilder builder)
        {
            var options =
                builder.RegisterMessagePipe(pipeOptions => pipeOptions.InstanceLifetime = InstanceLifetime.Scoped);
            builder.RegisterComponent(gameManager).AsImplementedInterfaces();
            builder.RegisterInstance(panelController);
        }
    }
}