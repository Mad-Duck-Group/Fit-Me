using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Units;
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
        [SerializeField] private float objectScale = 0.5f;
        #endregion
        
        #region Fields
        private Tween _scaleTween;
        #endregion
        
        public void SpawnAtStart()
        {
            spawnPoints.ForEach(FreeSpawnPoint);
            SpawnRandomBlock();
        }
    
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

        public void SpawnRandomBlock()
        {
            var blockTypes = Enum.GetValues(typeof(BlockTypes)).Cast<BlockTypes>().ToList();
            var blockFaces = Enum.GetValues(typeof(BlockFaces)).Cast<BlockFaces>().ToList();
            var randomBlocks = new List<KeyValuePair<BlockTypes, BlockFaces>>();
            foreach (var type in blockTypes)
            {
                var randomFace = blockFaces.GetRandomElement();
                randomBlocks.Add(new KeyValuePair<BlockTypes, BlockFaces>(type, randomFace));
            }
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (!spawnPoints[i].IsFree)
                {
                    continue;
                }
                Transform spawnTransform = spawnPoints[i].Transform;
                var randomBlock = randomBlocks.GetRandomElement();
                var blockType = randomBlock.Key;
                var blockFace = randomBlock.Value;
                var blockPrefab = blockPrefabDictionary[blockFace];
                Block block = Instantiate(blockPrefab, spawnTransform.position, Quaternion.identity, transform);
                block.ChangeColor(blockType, false);
                randomBlocks.Remove(randomBlock);
                block.SpawnIndex = i;
                block.transform.localScale = Vector3.zero;
                Vector3 scale = new Vector3(objectScale, objectScale, 1f);
                block.GenerateSchema();
                int randomRotation = Random.Range(0, 4) * 90;
                block.transform.eulerAngles = new Vector3(0, 0, randomRotation);
                _scaleTween = Tween.Scale(block.transform, scale, 0.2f).OnComplete(() => block.Initialize());
                spawnPoints[i].IsFree = false;
                spawnPoints[i].CurrentBlock = block;
            }
        }
    
        public void DestroyBlock(bool destroyAll = false)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (destroyAll)
                {
                    Destroy(spawnPoints[i].CurrentBlock.gameObject);
                    FreeSpawnPoint(i);
                }
                else if (!spawnPoints[i].IsFree)
                {
                    Destroy(spawnPoints[i].CurrentBlock.gameObject);
                    FreeSpawnPoint(i);
                }
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
            List<Block> blockToCheck = spawnPoints.Select(spawnPoint => spawnPoint.CurrentBlock).ToList();
            if (!GridManager.Instance.CheckAvailableBlock(blockToCheck, out _))
            {
                GameManager.Instance.GameOver(true);  
            }
        }
    }
}
