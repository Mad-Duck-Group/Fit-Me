using System.Collections.Generic;
using System.Linq;
using MadDuck.Scripts.Utils.Inspectors;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MadDuck.Scripts.Utils
{
    [CreateAssetMenu(fileName = "GameBootstrapper", menuName = "MadDuck/GameBootstrapper", order = 1)]
    public class GameBootstrapper : ScriptableObject
    {
        [InfoBox("Make sure that this asset is preloaded in Player Settings > Preloaded Assets.")]
        [HideLabel, ShowInInspector]
        private InspectorVoid _infoBox;
        
        [SerializeField] private List<GameObject> persistentMonoSingletons;
        
        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var preloadAsset = UnityEditor.PlayerSettings.GetPreloadedAssets().FirstOrDefault(x => x is GameBootstrapper);
            if (preloadAsset is GameBootstrapper instance)
            {
                instance.OnEnable();
            }
        }
        #endif
        
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                OnFirstSceneLoaded(activeScene, default);
            }
            else
            {
                SceneManager.sceneLoaded -= OnFirstSceneLoaded;
                SceneManager.sceneLoaded += OnFirstSceneLoaded;
            }
            
        }
        
        private void OnFirstSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("Instantiating persistent objects...");
            foreach (var persistentObject in persistentMonoSingletons)
            {
                if (persistentObject)
                {
                    Instantiate(persistentObject);
                }
                else
                {
                    Debug.LogWarning("A persistent object is null and will not be instantiated.");
                }
            }
        }
    }
}