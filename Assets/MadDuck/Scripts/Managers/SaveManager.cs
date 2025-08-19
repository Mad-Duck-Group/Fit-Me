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
        public static event Action OnLoadCompleted;

        private bool _saveReady = true;
        private bool _saveInQueue;
        private int _currentLoadRetryAttempts = 0;
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
                    _currentLoadRetryAttempts = 0;
                }
                else
                {
                    CurrentSaveFile.DeleteFile();
                    Load();
                    _currentLoadRetryAttempts++;
                }
            });
        }
        
        public void Save()
        {
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