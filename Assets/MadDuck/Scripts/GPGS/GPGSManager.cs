using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using MadDuck.Scripts.Managers;
using MessagePipe;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.GPGS
{
    public interface IGPGSService : IDisposable
    {
        public void Initialize();
    }
    
    public struct GPGSServiceRequest
    {
        public readonly Type serviceType;
        
        private GPGSServiceRequest(Type serviceType)
        {
            this.serviceType = serviceType;
        }
        
        public static GPGSServiceRequest Create<T>() where T : IGPGSService
        {
            return new GPGSServiceRequest(typeof(T));
        }
    }

    public struct GPGSServiceResponse<T> where T : IGPGSService
    {
        public readonly T service;

        public GPGSServiceResponse(T service)
        {
            this.service = service;
        }
    }
    
    public class TestService : IGPGSService
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }
    }
    
    [ShowOdinSerializedPropertiesInInspector]
    public class GPGSManager : PersistentMonoSingleton<GPGSManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization, 
        IRequestHandler<GPGSServiceRequest, GPGSServiceResponse<IGPGSService>>
    {
        [Title("Services")]
        [OdinSerialize] private List<IGPGSService> gpgsSavedGame = new();
        
        #if UNITY_ANDROID
        public static event Action<SignInStatus> OnFinishedAuthentication;
        
        #region Initialization
        private void Start()
        {
            PlayGamesPlatform.Activate();
            Authenticate();
        }
        #endregion
        
        #region Events
        private void OnEnable()
        {
            gpgsSavedGame.ForEach(x => x.Initialize());
        }
        
        private void OnDisable()
        {
            gpgsSavedGame.ForEach(x => x.Dispose());
        }
        #endregion

        #region Authentication
        private void Authenticate()
        {
            PlayGamesPlatform.Instance.Authenticate(AuthenticateCallback);
        }
        
        public void ManualAuthenticate()
        {
            PlayGamesPlatform.Instance.ManuallyAuthenticate(AuthenticateCallback);
        }
        
        private void AuthenticateCallback(SignInStatus status)
        {
            Debug.Log($"Authentication finished with status: {status}");
            OnFinishedAuthentication?.Invoke(status);
        }
        #endregion
        #endif
        
        #region Serialization
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }

        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        public SerializationData SerializationData 
        { 
            get => serializationData;
            set => serializationData = value;
        }
        #endregion

        public GPGSServiceResponse<IGPGSService> Invoke(GPGSServiceRequest request)
        {
            var service = gpgsSavedGame.Find(x => x.GetType() == request.serviceType);
            if (service != null)
            {
                return new GPGSServiceResponse<IGPGSService>(service);
            }
            throw new Exception($"Service of type {request.serviceType} not found.");
        }
    }
}