using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using PrimeTween;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
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

    public class LoadSceneManager : PersistentMonoSingleton<LoadSceneManager>
    {
        [Title("Scenes")]
        [SerializeField] private SerializableDictionary<SceneType, SceneReference> scenes;

        [Title("Fade")] 
        [SerializeField] private Image background;
        [SerializeField] private float fadeOutTime = 1.5f;
        [SerializeField] private Ease fadeOutEase = Ease.OutQuint;
        [SerializeField] private Ease fadeInEase = Ease.InQuint;
        
        [Title("Debug")]
        [SerializeField] private SceneType debugSceneType;
        [Button("Debug Load Scene")]
        private void DebugLoadScene()
        {
            LoadScene(debugSceneType, LoadSceneMode.Single, false);
        }
    
        public delegate void StartFadeOut();
        public static event StartFadeOut OnStartFadeOut;
        public delegate void FinishFadeOut();
        public static event FinishFadeOut OnFinishFadeOut;
        public delegate void StartFadeIn();
        public static event StartFadeIn OnStartFadeIn;
        public delegate void FinishFadeIn();
        public static event FinishFadeIn OnFinishFadeIn;
    
        private IDisposable _loadSceneEventListener;
        private Tween _fadeTween;
        private AsyncOperation _asyncOperation;
        private CancellationTokenSource _loadSceneCts;
        public string NextScene { get; private set; }
        public LoadSceneMode LoadSceneMode { get; private set; }
        public static bool FirstSceneLoaded { get; private set; }
        
        private void OnEnable()
        {
            _loadSceneEventListener = GlobalMessagePipe.GetSubscriber<LoadSceneEvent>()
                .Subscribe(OnLoadSceneEvent);
            background.color = new Color(0, 0, 0, 0);
            background.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _loadSceneEventListener?.Dispose();
        }

        private void Start()
        {
            if (!FirstSceneLoaded) OnFinishFadeIn?.Invoke();
        }

        private void OnLoadSceneEvent(LoadSceneEvent loadSceneEvent)
        {
            LoadScene(loadSceneEvent.sceneType, loadSceneEvent.loadSceneMode, loadSceneEvent.useLoadingScene);
        }

        public void LoadScene(SceneType sceneType, LoadSceneMode loadSceneMode, bool useLoadingScene)
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
            background.gameObject.SetActive(true);
            _fadeTween = Tween.Alpha(background, 0f, 1f, fadeOutTime, fadeOutEase, useUnscaledTime: true)
                .OnComplete(() =>
                {
                    OnFadeOutComplete(useLoadingScene);
                });
        }

        private void OnFadeOutComplete(bool useLoadingScene)
        {
            OnFinishFadeOut?.Invoke();
            if (useLoadingScene)
            {
                background.color = new Color(0, 0, 0, 0);
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
            await UniTask.WaitWhile(() => _asyncOperation.progress < 0.9f, cancellationToken: cancellationToken);
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

        private void UnloadScene(Scene lastScene, Scene current)
        {
            Debug.Log("Unloading " + lastScene.name);
            if (LoadSceneMode == LoadSceneMode.Additive)
            {
                SceneManager.UnloadSceneAsync(lastScene);
            }
            OnStartFadeIn?.Invoke();
            _fadeTween = Tween.Alpha(background, 1f, 0f, fadeOutTime, fadeInEase, useUnscaledTime: true)
                .OnComplete(() =>
                {
                    background.gameObject.SetActive(false);
                    OnFinishFadeIn?.Invoke();
                });
            SceneManager.activeSceneChanged -= UnloadScene;
        }
    
    }
}