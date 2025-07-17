using System;
using System.Collections.Generic;
using Esper.ESave;
using Esper.ESave.Threading;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    [RequireComponent(typeof(SaveFileSetup))]
    public class SaveManager : PersistentMonoSingleton<SaveManager>
    {
        #region Fields and Properties
        public SaveFileSetup SaveFileSetup { get; private set; }
        public SaveFile CurrentSaveFile => SaveFileSetup.GetSaveFile();
        public static event Action OnSaveCompleted;
        public static event Action OnLoadCompleted;

        private bool _saveReady = true;
        private bool _saveInQueue;
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();
            SaveFileSetup = GetComponent<SaveFileSetup>();
        }

        private void Start()
        {
            Load();
        }
        #endregion

        #region Save/Load
        public void Load()
        {
            if (!SaveFileSetup) SaveFileSetup = GetComponent<SaveFileSetup>();
            var operation = CurrentSaveFile.Load(true);
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
            if (!SaveFileSetup) SaveFileSetup = GetComponent<SaveFileSetup>();
            if (!_saveReady)
            {
                Debug.LogWarning("Save operation is already in progress.");
                _saveInQueue = true;
                return;
            }
            var operation = CurrentSaveFile.Save();
            _saveReady = false;
            operation.onOperationEnded.AddListener(() =>
            {
                if (operation.state == SaveFileOperation.OperationState.Completed)
                {
                    OnSaveCompleted?.Invoke();
                    _saveReady = true;
                    if (!_saveInQueue) return;
                    _saveInQueue = false;
                    Save(); // Retry saving if there was a save in queue
                }
                else
                {
                    Debug.LogError($"Failed to save file");
                }
            });
        }
        #endregion
    }
}