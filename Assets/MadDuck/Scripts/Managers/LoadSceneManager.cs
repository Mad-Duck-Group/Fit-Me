using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels.MainMenu;
using MadDuck.Scripts.UIs.Panels.Transition;
using MessagePipe;
using PrimeTween;
using R3;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MadDuck.Scripts.Managers
{

    #region Events
    public struct LoadSceneEvent
    {
        public readonly SceneType sceneType;
        public readonly LoadSceneMode loadSceneMode;
        public readonly bool useLoadingScene;
        
        public LoadSceneEvent(SceneType sceneType, LoadSceneMode loadSceneMode, bool useLoadingScene)
        {
            this.sceneType = sceneType;
            this.loadSceneMode = loadSceneMode;
            this.useLoadingScene = useLoadingScene;
        }
    }
    #endregion

    public enum SceneType
    {
        MainMenu,
        Loading,
        Tutorial,
        Gameplay
    }

    public enum TransitionScreenType
    {
        BlockCascade,
        BlockPopUp
    }

    [ShowOdinSerializedPropertiesInInspector]
    public class LoadSceneManager : PersistentMonoSingleton<LoadSceneManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspectors
        [Title("Scenes")]
        [SerializeField] private SerializableDictionary<SceneType, SceneReference> scenes;

        [field: Title("Transition")] 
        [field: OdinSerialize] public SerializableDictionary<TransitionScreenType, ITransitionScreen> TransitionScreens 
        { get; private set; } = new();
        [SerializeField] private bool minimumLoadingScreenDuration = true;
        [SerializeField, ShowIf(nameof(minimumLoadingScreenDuration))] 
        private float loadingScreenDuration = 1f;

        [Title("Debug")]
        [SerializeField] private SceneType debugSceneType;
        [Button("Debug Load Scene")]
        private void DebugLoadScene()
        {
            LoadScene(debugSceneType, LoadSceneMode.Single, false);
        }
        #endregion
        
        #region Fields and Properties
        public static event Action OnStartFadeOut;
        public static event Action OnFinishFadeOut;
        public static event Action OnStartFadeIn;
        public static event Action OnFinishFadeIn;
    
        private ITransitionScreen _currentTransitionScreen;
        private IDisposable _loadSceneEventListener;
        private Tween _fadeTween;
        private AsyncOperation _asyncOperation;
        private CancellationTokenSource _loadSceneCts;
        public string NextScene { get; private set; }
        public LoadSceneMode LoadSceneMode { get; private set; }
        public static bool FirstSceneLoaded { get; private set; }
        #endregion
        
        #region Initialization
        private void Start()
        {
            TransitionScreens.Values.ForEach(screen =>
            {
                screen.Initialize();
            });
            if (!FirstSceneLoaded) OnFinishFadeIn?.Invoke();
        }
        #endregion
        
        #region Events
        private void OnEnable()
        {
            _loadSceneEventListener = GlobalMessagePipe.GetSubscriber<LoadSceneEvent>()
                .Subscribe(OnLoadSceneEvent);
        }

        private void OnDisable()
        {
            _loadSceneEventListener?.Dispose();
        }
        
        private void OnLoadSceneEvent(LoadSceneEvent loadSceneEvent)
        {
            LoadScene(loadSceneEvent.sceneType, loadSceneEvent.loadSceneMode, loadSceneEvent.useLoadingScene);
        }
        #endregion
        
        #region Scene Loading
        
        public void ReloadScene(LoadSceneMode loadSceneMode, bool useLoadingScene)
        {
            var currentSceneName = SceneManager.GetActiveScene().path;
            var sceneType = scenes.FirstOrDefault(x => x.Value.Path == currentSceneName).Key;
            if (sceneType == default)
            {
                Debug.LogError($"Current scene '{currentSceneName}' not found in the dictionary.");
                return;
            }
            LoadScene(sceneType, loadSceneMode, useLoadingScene);
        }
        
        public async void LoadScene(SceneType sceneType, LoadSceneMode loadSceneMode, bool useLoadingScene)
        {
            if (_asyncOperation is { isDone: false } || _fadeTween.isAlive) return;
            string sceneName;
            if (scenes.TryGetValue(sceneType, out SceneReference sceneReference))
            {
                sceneName = sceneReference.Path;
            }
            else
            {
                Debug.LogError($"Scene {sceneType} not found in the dictionary.");
                return;
            }
            NextScene = sceneName;
            LoadSceneMode = loadSceneMode;
            OnStartFadeOut?.Invoke();
            _currentTransitionScreen = TransitionScreens.Values.GetRandomElement();
            _currentTransitionScreen.Show();
            await _currentTransitionScreen.TransitionIn().ToUniTask();
            await _currentTransitionScreen.TransitionBefore().ToUniTask();
            OnFadeOutComplete(useLoadingScene);
        }

        private void OnFadeOutComplete(bool useLoadingScene)
        {
            OnFinishFadeOut?.Invoke();
            if (useLoadingScene)
            {
                string loadingScene;
                if (scenes.TryGetValue(SceneType.Loading, out SceneReference loadingSceneReference))
                {
                    loadingScene = loadingSceneReference.Path;
                }
                else
                {
                    Debug.LogError("Loading scene not found in the dictionary.");
                    return;
                }
                SceneManager.LoadScene(loadingScene);
            }
            else
            {
                _loadSceneCts = new CancellationTokenSource();
                LoadSceneAsync(_loadSceneCts.Token).Forget();
            }
        }

        private async UniTask LoadSceneAsync(CancellationToken cancellationToken = default)
        {
            Scene thisScene = SceneManager.GetActiveScene();
            SceneManager.activeSceneChanged += UnloadScene;
            _asyncOperation = SceneManager.LoadSceneAsync(NextScene, LoadSceneMode);
            _asyncOperation.allowSceneActivation = false;
            var progressCts = new CancellationTokenSource();
            Observable.EveryValueChanged(_asyncOperation, f => f.progress, cancellationToken: progressCts.Token)
                .Subscribe(progress => _currentTransitionScreen.Progress = progress)
                .AddTo(this);
            if (!minimumLoadingScreenDuration)
                await UniTask.WaitWhile(() => _asyncOperation.progress < 0.9f, cancellationToken: cancellationToken);
            else
            {
                await UniTask.WhenAll(UniTask.WaitWhile(() => _asyncOperation.progress < 0.9f, cancellationToken: cancellationToken),
                    UniTask.WaitForSeconds(loadingScreenDuration, ignoreTimeScale: true, cancellationToken: cancellationToken));
            }
            progressCts.Cancel();
            _currentTransitionScreen.Progress = 1f;
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            _asyncOperation.allowSceneActivation = true;
            SceneManager.sceneLoaded += (scene, mode) => SceneManager.SetActiveScene(scene);
            FirstSceneLoaded = true;
            Time.timeScale = 1f;
            _asyncOperation = null;
        }

        public void CancelLoadScene()
        {
            _loadSceneCts?.Cancel();
        }

        private async void UnloadScene(Scene lastScene, Scene current)
        {
            Debug.Log("Unloading " + lastScene.name);
            if (LoadSceneMode == LoadSceneMode.Additive)
            {
                SceneManager.UnloadSceneAsync(lastScene);
            }
            OnStartFadeIn?.Invoke();
            await _currentTransitionScreen.TransitionAfter().ToUniTask();
            await _currentTransitionScreen.TransitionOut().ToUniTask();
            _currentTransitionScreen.Hide();
            _currentTransitionScreen.Progress = 0f;
            _currentTransitionScreen = null;
            OnFinishFadeIn?.Invoke();
            SceneManager.activeSceneChanged -= UnloadScene;
        }
        #endregion

        #region Serialization
        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        SerializationData ISupportsPrefabSerialization.SerializationData 
        {
            get => serializationData;
            set => serializationData = value;
        }
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }
        #endregion
    }
}