using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils;
using PrimeTween;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D.Animation;
using Random = UnityEngine.Random;

namespace MadDuck.Scripts.Managers
{
    public class RandomBlockManager : MonoSingleton<RandomBlockManager>
    {
        #region Data Structures
        [Serializable]
        public record SpawnPoint
        {
            [field: SerializeField] public Transform Transform { get; private set; }

            [field: SerializeField, DisplayAsString] public bool IsFree { get; set; } = true;
            [field: SerializeField, Sirenix.OdinInspector.ReadOnly] public Block CurrentBlock { get; set; }
        }
        #endregion

        #region Inspectors

        [Title("Random References")] 
        [field: SerializeField] public SerializableDictionary<BlockTypes, SpriteLibraryAsset> SpriteLibraryAssets { get; private set; } = new();
        [field: SerializeField] public SerializableDictionary<BlockTypes, Color> AtomColorDictionary { get; private set; } = new();
        [SerializeField] private Block blockPrefab;
        [SerializeField] [DictionaryDrawerSettings(KeyLabel = "Block Face", ValueLabel = "Block Preset")] 
        private SerializableDictionary<string, BlockPreset> blockPresetDictionary = new();
        [SerializeField] private SpawnPoint[] spawnPoints;

        [Title("Random Settings")]
        [SerializeField] private int maxRandomAmount = 3;
        [SerializeField] private float objectScale = 0.5f;
        #endregion
        
        #region Fields
        private Tween _scaleTween;
        private readonly Dictionary<string, Block> _blockPool = new();
        #endregion
        
        #region Initialization
        public void SpawnAtStart()
        {
            spawnPoints.ForEach(FreeSpawnPoint);
            SpawnRandomBlock();
        }
        #endregion
        
        #region Events
        private void OnEnable()
        {
            GameManager.OnSceneActivated += OnSceneActivated;
        }

        private void OnDisable()
        {
            GameManager.OnSceneActivated -= OnSceneActivated;
        }
        
        private void OnSceneActivated()
        {
           foreach (var pair in blockPresetDictionary)
           {
               if (_blockPool.ContainsKey(pair.Key)) continue;
               var block = Instantiate(blockPrefab, transform);
               block.name = pair.Key;
               block.GenerateAtom(pair.Key, pair.Value);
               block.gameObject.SetActive(false);
               _blockPool.Add(pair.Key, block);
           }
        }
        #endregion
        
        #region Spawning
        /// <summary>
        /// Spawns random blocks at spawn points.
        /// </summary>
        public void SpawnRandomBlock()
        {
            //if (spawnPoints.Any(x => !x.IsFree)) return;
            var blockTypes = Enum.GetValues(typeof(BlockTypes)).Cast<BlockTypes>().ToList();
            var allSchemas = _blockPool
                .SelectMany(x => x.Value.BlockPreset.BlockSchemas
                    .Select(schema => (BlockFace: x.Key, BlockSchema: schema)));
            var shuffledSchemas = allSchemas.Shuffled().ToList();
            /*GridManager.Instance.CreateVacantSchema(out var vacantSchema);
            var firstThreeSchemas = shuffledSchemas
                .Where(s => ArrayHelper.CanBFitInA(vacantSchema, s.BlockSchema.schema, 
                    out vacantSchema, true))
                .Take(maxRandomAmount)
                .ToList();
            var remainingAmount = maxRandomAmount - firstThreeSchemas.Count;
            if (remainingAmount > 0) 
                firstThreeSchemas.AddRange(shuffledSchemas.GetRandomElements(remainingAmount));
            var randomSchemas = firstThreeSchemas.Shuffled().ToList();*/
            //NOTE: Disable smart random for now to test the pure random spawning
            var randomSchemas = shuffledSchemas
                .Take(maxRandomAmount)
                .ToList();
            for (int i = 0; i < randomSchemas.Count; i++)
            {
                if (!spawnPoints[i].IsFree)
                {
                    continue;
                }
                Transform spawnTransform = spawnPoints[i].Transform;
                var randomBlock = randomSchemas[i];
                var blockType = blockTypes.GetRandomElement();
                var blockFace = randomBlock.BlockFace;
                var index = randomBlock.BlockSchema.Index;
                if (!_blockPool.ContainsKey(blockFace))
                {
                    Debug.LogError($"Block face {blockFace} not found in block pool.");
                    continue;
                }
                var blockToSpawn = _blockPool[blockFace];
                Block block = Instantiate(blockToSpawn, spawnTransform.position, Quaternion.identity, transform);
                block.gameObject.SetActive(true);
                block.ChangeType(blockType, false);
                block.SpawnIndex = i;
                block.transform.localScale = Vector3.zero;
                Vector3 scale = new Vector3(objectScale, objectScale, 1f);
                int randomRotation = index * 90;
                block.transform.eulerAngles = new Vector3(0, 0, randomRotation);
                _scaleTween = Tween.Scale(block.transform, scale, 0.2f).OnComplete(() => block.Initialize());
                spawnPoints[i].IsFree = false;
                spawnPoints[i].CurrentBlock = block;
            }
        }
        #endregion
        
        #region Utils
        public void FreeSpawnPoint(int index)
        {
            spawnPoints[index].IsFree = true;
            spawnPoints[index].CurrentBlock = null;
        }

        public void FreeSpawnPoint(SpawnPoint spawnPoint)
        {
            spawnPoint.IsFree = true;
            spawnPoint.CurrentBlock = null;
        }
        
        public void ResetSpawnPoint()
        {
            foreach (var spawnPoint in spawnPoints)
            {
                spawnPoint.IsFree = true;
                if (spawnPoint.CurrentBlock)
                    Destroy(spawnPoint.CurrentBlock.gameObject);
                spawnPoint.CurrentBlock = null;
            }
        }

        // public void ReRoll()
        // {
        //     DestroyBlock(true);
        //     SpawnRandomBlock();
        //     if (GameManager.Instance.CurrentReRoll <= 0)
        //     {
        //         GameOverCheck();
        //     }
        // }

        public async UniTask GameOverCheck()
        {
            if (_scaleTween.isAlive)
            {
                await UniTask.WaitUntil(() => _scaleTween.GetAwaiter().IsCompleted);
            }
            List<Block> blockToCheck = spawnPoints.Where(x => !x.IsFree).Select(spawnPoint => spawnPoint.CurrentBlock).ToList();
            if (!GridManager.Instance.CheckAvailableBlock(blockToCheck, out _))
            {
                GameManager.Instance.GameOver(true);  
            }
        }
        #endregion

    }
}
