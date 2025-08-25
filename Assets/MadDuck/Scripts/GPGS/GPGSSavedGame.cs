using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using MadDuck.Scripts.Managers;
using MessagePipe;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.GPGS
{
    public struct LoadFromServiceEvent
    {
        public readonly byte[] data;

        public LoadFromServiceEvent(byte[] data)
        {
            this.data = data;
        }
    }

    [Serializable]
    public struct SaveUIConfig
    {
        public uint maxNumToDisplay;
        public bool allowCreateNew;
        public bool allowDelete;
    }
    
    [Serializable]
    public class GPGSSavedGame : IGPGSService
    {
        [SerializeField] private Sprite defaultSavedImage;
        [SerializeField] private bool allowSaveSelection;
        [SerializeField, ShowIf(nameof(allowSaveSelection))] private SaveUIConfig saveUIConfig;
        [SerializeField] private bool allowLoadSelection;
        [SerializeField, ShowIf(nameof(allowLoadSelection))] private SaveUIConfig loadUIConfig;
        
        private IPublisher<LoadFromServiceEvent> _loadFromServicePublisher;
        private IDisposable _saveToServiceSubscription;
        
        #region Life Cycle

        public void Initialize()
        {
            Subscribe();
        }

        public void Dispose()
        {
            Unsubscribe();
        }
        #endregion
        
        #region Events

        private void Subscribe()
        {
            _saveToServiceSubscription = GlobalMessagePipe.GetSubscriber<SaveToServiceEvent>()
                .Subscribe(x => SaveToService(x).Forget());
            _loadFromServicePublisher = GlobalMessagePipe.GetPublisher<LoadFromServiceEvent>();
            GPGSManager.OnFinishedAuthentication += OnFinishedAuthentication;
        }
        
        private void Unsubscribe()
        {
            _saveToServiceSubscription?.Dispose();
            GPGSManager.OnFinishedAuthentication -= OnFinishedAuthentication;
        }

        private void OnFinishedAuthentication(SignInStatus status)
        {
            if (status is not SignInStatus.Success) return;
            Debug.Log("GPGS Authenticated, ready to use saved games.");
            LoadFromService().Forget();
        }
        #endregion
        
        #region Helpers
        
        private async UniTask<Tuple<bool, ISavedGameMetadata>> ShowSaveSelectionUI(SaveUIConfig config) 
        {
            var tcs = new UniTaskCompletionSource<bool>();
            ISavedGameMetadata result = null;
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            savedGameClient.ShowSelectSavedGameUI("Select saved game",
                config.maxNumToDisplay, config.allowCreateNew, config.allowDelete, 
                (status, metadata) =>
                {
                    result = metadata;
                    OnSavedGameSelected(status, tcs);
                });
            await tcs.Task;
            return tcs.GetResult(0)
                ? new Tuple<bool, ISavedGameMetadata>(true, result) 
                : new Tuple<bool, ISavedGameMetadata>(false, null);
        }
        
        private void OnSavedGameSelected(SelectUIStatus status, UniTaskCompletionSource<bool> tcs)
        {
            if (status == SelectUIStatus.SavedGameSelected) 
            {
                tcs.TrySetResult(true);
            } 
            else 
            {
                Debug.LogWarning($"Failed to select saved game: {status}");
                tcs.TrySetResult(false);
            }
        }

        private async UniTask<Tuple<bool, IList<ISavedGameMetadata>>> TryFetchSaveGames()
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            // create Task from callback
            var tcs = new UniTaskCompletionSource<bool>();
            var result = new List<ISavedGameMetadata>();
            savedGameClient.FetchAllSavedGames(DataSource.ReadCacheOrNetwork, (status, games) =>
            {
                result = games;
                OnFetchedSavedGames(status, tcs);
            });
            await tcs.Task;
            return tcs.GetResult(0)
                ? new Tuple<bool, IList<ISavedGameMetadata>>(true, result) 
                : new Tuple<bool, IList<ISavedGameMetadata>>(false, null);
        }
        
        private void OnFetchedSavedGames(SavedGameRequestStatus status, UniTaskCompletionSource<bool> tcs)
        {
            if (status == SavedGameRequestStatus.Success)
            {
                tcs.TrySetResult(true);
            }
            else
            {
                Debug.LogWarning($"Failed to fetch saved games: {status}");
                tcs.TrySetResult(false);
            }
        }
        
        private async UniTask<Tuple<bool, ISavedGameMetadata>> TryOpenSavedGame(string filename, bool newSave = false) 
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            // create Task from callback
            var tcs = new UniTaskCompletionSource<bool>();
            ISavedGameMetadata result = null;
            if (newSave)
            {
                var saveGames = await TryFetchSaveGames();
                if (!saveGames.Item1) return new Tuple<bool, ISavedGameMetadata>(false, null);
                if (saveGames.Item2 != null)
                {
                    var count = saveGames.Item2.Count;
                    filename = $"save_{count + 1}";
                }
            }
            Debug.Log("Opening saved game: " + filename);
            savedGameClient.OpenWithAutomaticConflictResolution(filename, DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime, (status, game) =>
                {
                    result = game;
                    OnSavedGameOpened(status, tcs);
                });
            await tcs.Task;
            Debug.Log($"result is null: {result == null}, status: {tcs.GetResult(0)}");
            return tcs.GetResult(0)
                ? new Tuple<bool, ISavedGameMetadata>(true, result) 
                : new Tuple<bool, ISavedGameMetadata>(false, null);
        }
        
        private void OnSavedGameOpened(SavedGameRequestStatus status, UniTaskCompletionSource<bool> tcs) 
        {
            if (status == SavedGameRequestStatus.Success) 
            {
                Debug.Log("Successfully opened saved game.");
                tcs.TrySetResult(true);
            } 
            else 
            {
                Debug.LogWarning($"Failed to open saved game: {status}");
                tcs.TrySetResult(false);
            }
        }

        private async UniTask<Tuple<bool, ISavedGameMetadata>> TryGetUnopenedSavedGame(bool allowSaveSelection, SaveUIConfig? config = null)
        {
            ISavedGameMetadata unopenedSaveGame;
            if (allowSaveSelection && config != null)
            {
                var selectionResult = await ShowSaveSelectionUI(config.Value);
                if (!selectionResult.Item1) return new(false, null);
                unopenedSaveGame = selectionResult.Item2;
            }
            else
            {
                var allSaveGamesResult = await TryFetchSaveGames();
                if (!allSaveGamesResult.Item1) return new(false, null);
                var firstSave = allSaveGamesResult.Item2.Count > 0 ? allSaveGamesResult.Item2[0] : null;
                unopenedSaveGame = firstSave;
            }
            return new (true, unopenedSaveGame);
        }
        #endregion

        #region Save
        public async UniTaskVoid ManualSaveToService()
        {
            if (!PlayGamesPlatform.Instance.IsAuthenticated()) return;
            var data = await JsonSaveManager.Instance.ConvertToBytes();
            SaveToService(new SaveToServiceEvent(data), allowSaveSelection).Forget();
        }

        private async UniTaskVoid SaveToService(SaveToServiceEvent eventData, bool allowSaveSelection = false)
        {
            if (!PlayGamesPlatform.Instance.IsAuthenticated()) return;
            var unopenedSaveGameResult = await TryGetUnopenedSavedGame(allowSaveSelection, saveUIConfig);
            if (!unopenedSaveGameResult.Item1)
            {
                Debug.Log("No save game selected or available to save.");
                return;
            }
            var newSave = unopenedSaveGameResult.Item2 == null;
            var fileName = newSave ? "save_0" : unopenedSaveGameResult.Item2.Filename;
            var openResult = await TryOpenSavedGame(fileName, newSave);
            if (!openResult.Item1 || openResult.Item2 == null)
            {
                Debug.LogWarning("Failed to open the selected save game.");
                return;
            }
            var openedSavedGame = openResult.Item2;
            var data = eventData.data;
            var saveResult = await TrySaveToService(openedSavedGame, data, eventData.totalPlaytime, eventData.savedImage);
            if (!saveResult.Item1 || saveResult.Item2 == null) return;
            Debug.Log("Game saved to cloud successfully.");
        }

        private async UniTask<Tuple<bool, ISavedGameMetadata>> TrySaveToService(ISavedGameMetadata game, byte[] savedData, 
            TimeSpan? totalPlaytime = null, Texture2D savedImage = null)
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            builder = builder
                    .WithUpdatedDescription("Saved game at " + DateTime.Now);
            if (totalPlaytime != null)
                builder = builder.WithUpdatedPlayedTime(totalPlaytime.Value);
            if (savedImage) 
            {
                var pngData = savedImage.EncodeToPNG();
                builder = builder.WithUpdatedPngCoverImage(pngData);
            }
            else
            {
                try
                {
                    var defaultPngData = defaultSavedImage.texture.Decompress().EncodeToPNG();
                    builder = builder.WithUpdatedPngCoverImage(defaultPngData);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to encode default saved image to PNG: {e.Message}");
                }
            }
            SavedGameMetadataUpdate updatedMetadata = builder.Build();
            var tcs = new UniTaskCompletionSource<bool>();
            ISavedGameMetadata result = null;
            savedGameClient.CommitUpdate(game, updatedMetadata, savedData,
                (status, updatedGame) =>
                {
                    result = updatedGame;
                    OnSavedGameWritten(status, updatedGame, tcs);
                });
            await tcs.Task;
            return tcs.GetResult(0)
                ? new Tuple<bool, ISavedGameMetadata>(true, result) 
                : new Tuple<bool, ISavedGameMetadata>(false, null);
        }
        
        private void OnSavedGameWritten(SavedGameRequestStatus status, ISavedGameMetadata game, UniTaskCompletionSource<bool> tcs)
        {
            if (status == SavedGameRequestStatus.Success)
            {
                tcs.TrySetResult(true);
                Debug.Log("Successfully saved game: " + game.Filename);
            }
            else
            {
                tcs.TrySetResult(false);
                Debug.LogWarning($"Failed to save game: {status}");
            }
        }
        #endregion
        
        #region Load
        public void ManualLoadFromService()
        {
            if (!PlayGamesPlatform.Instance.IsAuthenticated()) return;
            LoadFromService(allowLoadSelection).Forget();
        }
        public async UniTaskVoid LoadFromService(bool allowLoadSelection = false)
        {
            if (!PlayGamesPlatform.Instance.IsAuthenticated()) return;
            var unopenedSaveGameResult = await TryGetUnopenedSavedGame(allowLoadSelection, loadUIConfig);
            if ((!unopenedSaveGameResult.Item1 || unopenedSaveGameResult.Item2 == null) && allowLoadSelection)
            {
                Debug.Log("No save game selected or available to load.");
                return;
            }
            var newSave = allowLoadSelection && unopenedSaveGameResult.Item2 == null;
            var fileName = unopenedSaveGameResult.Item2 == null ? "save_0" : unopenedSaveGameResult.Item2.Filename;
            var openResult = await TryOpenSavedGame(fileName, newSave);
            if (!openResult.Item1 || openResult.Item2 == null)
            {
                Debug.LogWarning("Failed to open the selected save game.");
                return;
            }
            var openedSavedGame = openResult.Item2;
            var loadResult = await TryLoadSavedGame(openedSavedGame);
            if (!loadResult.Item1 || loadResult.Item2 == null)
            {
                Debug.LogWarning("Failed to load the selected save game.");
                return;
            }
            Debug.Log("Game loaded from cloud successfully.");
            _loadFromServicePublisher.Publish(new LoadFromServiceEvent(loadResult.Item2));
        }

        public async UniTask<Tuple<bool, byte[]>> TryLoadSavedGame(ISavedGameMetadata savedGame)
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            var tcs = new UniTaskCompletionSource<bool>();
            byte[] result = null;
            savedGameClient.ReadBinaryData(savedGame, (status, data) =>
            {
                result = data;
                OnSavedGameDataRead(status, data, tcs);
            });
            await tcs.Task;
            return tcs.GetResult(0) 
                ? new Tuple<bool, byte[]>(true, result) 
                : new Tuple<bool, byte[]>(false, null);
        }
        
        private void OnSavedGameDataRead(SavedGameRequestStatus status, byte[] data, UniTaskCompletionSource<bool> tcs)
        {
            if (status == SavedGameRequestStatus.Success)
            {
                tcs.TrySetResult(true);
            }
            else
            {
                Debug.LogWarning($"Failed to read saved game data: {status}");
                tcs.TrySetResult(false);
            }
        }
        #endregion
    }
    
    public static class Texture2DUtils
    {
        public static Texture2D Decompress(this Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
    }
}