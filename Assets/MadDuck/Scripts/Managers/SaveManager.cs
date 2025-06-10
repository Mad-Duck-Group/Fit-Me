using System;
using Esper.ESave;
using Esper.ESave.Threading;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    [RequireComponent(typeof(SaveFileSetup))]
    public class SaveManager : PersistentMonoSingleton<SaveManager>
    {
        public SaveFileSetup SaveFileSetup { get; private set; }
        public SaveFile CurrentSaveFile => SaveFileSetup.GetSaveFile();
        public static event Action OnSaveCompleted;
        public static event Action OnLoadCompleted;

        protected override void Awake()
        {
            base.Awake();
            SaveFileSetup = GetComponent<SaveFileSetup>();
        }

        private void Start()
        {
            Load();
        }

        public void Load()
        {
            var operation = CurrentSaveFile.Load();
            operation.onOperationEnded.AddListener(() =>
            {
                if (operation.state == SaveFileOperation.OperationState.Completed)
                {
                    OnLoadCompleted?.Invoke();
                }
                else
                {
                    Debug.LogError($"Failed to load save file");
                }
            });
        }
        
        public void Save()
        {
            var operation = CurrentSaveFile.Save();
            operation.onOperationEnded.AddListener(() =>
            {
                if (operation.state == SaveFileOperation.OperationState.Completed)
                {
                    OnSaveCompleted?.Invoke();
                }
                else
                {
                    Debug.LogError($"Failed to save file");
                }
            });
        }
    }
}