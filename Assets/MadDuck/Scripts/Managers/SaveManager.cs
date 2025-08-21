using System;
using System.Collections.Generic;
using Esper.ESave;
using Esper.ESave.Threading;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    public class SaveManager : PersistentMonoSingleton<SaveManager>
    {
        [field: SerializeField] private SaveFileSetup debugSaveFileSetup;
        [field: SerializeField] private SaveFileSetup releaseSaveFileSetup;
        [field: SerializeField] private bool testRelease;
        [field: SerializeField, Range(1, 10)] private int retryAttempts = 3;
        
        #region Fields and Properties

        public SaveFile CurrentSaveFile
        {
            get
            {
                #if UNITY_EDITOR
                return testRelease ? releaseSaveFileSetup.GetSaveFile() : debugSaveFileSetup.GetSaveFile();
                #else
                return releaseSaveFileSetup.GetSaveFile();
                #endif
            }
        }

        public static event Action OnSaveCompleted;
        public static event Action OnSaveReady;
        public static event Action OnLoadCompleted;

        public bool Saving { get; private set; }
        private bool _saveInQueue;
        private int _currentLoadRetryAttempts = 0;
        private int _currentSaveRetryAttempts = 0;
        #endregion

        #region Initialization

        private void Start()
        {
            Load();
        }
        #endregion

        #region Save/Load
        public void Load()
        {
            if (_currentLoadRetryAttempts >= retryAttempts)
            {
                Debug.LogError($"Failed to load save file after {retryAttempts} attempts.");
                _currentLoadRetryAttempts = 0;
                return;
            }
            var operation = CurrentSaveFile.Load(true);
            operation.onOperationEnded.AddListener(() =>
            {
                if (operation.state == SaveFileOperation.OperationState.Completed)
                {
                    OnLoadCompleted?.Invoke();
                    OnSaveReady?.Invoke();
                    _currentLoadRetryAttempts = 0;
                }
                else
                {
                    CurrentSaveFile.DeleteFile();
                    _currentLoadRetryAttempts++;
                    Load();
                }
            });
        }
        
        public void Save()
        {
            if (_currentSaveRetryAttempts >= retryAttempts)
            {
                Debug.LogError($"Failed to save file after {retryAttempts} attempts.");
                _currentSaveRetryAttempts = 0;
                OnSaveReady?.Invoke();
                return;
            }
            if (Saving)
            {
                Debug.LogWarning("Save operation is already in progress.");
                _saveInQueue = true;
                return;
            }
            var operation = CurrentSaveFile.Save(true);
            Saving = true;
            operation.onOperationEnded.AddListener(() =>
            {
                if (operation.state == SaveFileOperation.OperationState.Completed)
                {
                    _currentSaveRetryAttempts = 0;
                    OnSaveCompleted?.Invoke();
                    Saving = false;
                    OnSaveReady?.Invoke();
                    if (!_saveInQueue) return;
                    _saveInQueue = false;
                    Save(); // Retry saving if there was a save in queue
                }
                else
                {
                    _currentSaveRetryAttempts++;
                    Saving = false;
                    _saveInQueue = false;
                    Save();
                }
            });
        }
        #endregion
    }
}