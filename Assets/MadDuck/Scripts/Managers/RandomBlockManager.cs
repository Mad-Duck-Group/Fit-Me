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
        [SerializeField] private SerializableDictionary<BlockFaces, Block> blockPrefabDictionary = new();
        [SerializeField] private SpawnPoint[] spawnPoints;

        [Title("Random Settings")]
        [SerializeField] private int maxRandomAmount = 3;
        [SerializeField] private float objectScale = 0.5f;
        #endregion
        
        #region Fields
        private Tween _scaleTween;
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
            foreach (var block in blockPrefabDictionary.Values.ToList())
            {
                block.GenerateSchema();
            }
        }
        #endregion
        
        #region Spawning
        /// <summary>
        /// Spawns random blocks at spawn points.
        /// </summary>
        public void SpawnRandomBlock()
        {
           
            // var randomBlocks = new List<KeyValuePair<BlockTypes, BlockFaces>>();
            // foreach (var type in blockTypes)
            // {
            //     var randomFace = blockFaces.GetRandomElement();
            //     randomBlocks.Add(new KeyValuePair<BlockTypes, BlockFaces>(type, randomFace));
            // }
            if (spawnPoints.Any(x => !x.IsFree)) return;
            var blockTypes = Enum.GetValues(typeof(BlockTypes)).Cast<BlockTypes>().ToList();
            //var blockFaces = Enum.GetValues(typeof(BlockFaces)).Cast<BlockFaces>().ToList();
            var allSchemas = blockPrefabDictionary.Values
                .SelectMany(x => x.BlockSchemas.Select(schema => (x.BlockFace, BlockSchema: schema)));
            var shuffledSchemas = allSchemas.Shuffled().ToList();
            GridManager.Instance.CreateVacantSchema(out var vacantSchema);
            var firstThreeSchemas = shuffledSchemas
                .Where(s => ArrayHelper.CanBFitInA(vacantSchema, s.BlockSchema.schema, 
                    out vacantSchema, true))
                .Take(maxRandomAmount)
                .ToList();
            var remainingAmount = maxRandomAmount - firstThreeSchemas.Count;
            if (remainingAmount > 0) 
                firstThreeSchemas.AddRange(shuffledSchemas.GetRandomElements(remainingAmount));
            firstThreeSchemas = firstThreeSchemas.Shuffled().ToList();
            for (int i = 0; i < firstThreeSchemas.Count; i++)
            {
                if (!spawnPoints[i].IsFree)
                {
                    continue;
                }
                Transform spawnTransform = spawnPoints[i].Transform;
                var randomBlock = firstThreeSchemas[i];
                var blockType = blockTypes.GetRandomElement();
                var blockFace = randomBlock.BlockFace;
                var index = randomBlock.BlockSchema.index;
                var blockPrefab = blockPrefabDictionary[blockFace];
                Block block = Instantiate(blockPrefab, spawnTransform.position, Quaternion.identity, transform);
                block.ChangeColor(blockType, false);
                block.SpawnIndex = i;
                block.transform.localScale = Vector3.zero;
                Vector3 scale = new Vector3(objectScale, objectScale, 1f);
                block.GenerateSchema();
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
