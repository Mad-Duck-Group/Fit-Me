using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MadDuck.Scripts.Managers;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class RandomBlockManager : MonoSingleton<RandomBlockManager>
{
    [Serializable]
    public struct SpawnPoint
    {
        [SerializeField] private Transform transform;
        public Transform Transform => transform;
        private bool _isFree;
        public bool IsFree { get => _isFree; set => _isFree = value; }
        [SerializeField, ReadOnly] private Block currentBlock;
        public Block CurrentBlock
        {
            get => currentBlock;
            set => currentBlock = value;
        }
    }

    [SerializeField] private float objectScale = 0.5f;
    [SerializeField] private Block[] topten;
    [SerializeField] private Block[] jelly;
    [SerializeField] private Block[] pan;
    [SerializeField] private Block[] sankaya;
    [SerializeField] private SpawnPoint[] spawnPoints;
    private List<Block> _randomBlocks;
    private Tween _scaleTween;

    // Start is called before the first frame update
    public void SpawnAtStart()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            FreeSpawnPoint(i);
        }
        SpawnRandomBlock();
    }
    
    public void FreeSpawnPoint(int index)
    {
        spawnPoints[index].IsFree = true;
        spawnPoints[index].CurrentBlock = null;
    }

    public void RandomType()
    {
        var toptenObj = topten[Random.Range(0, topten.Length)];
        var jellyObj = jelly[Random.Range(0, jelly.Length)];
        var panObj = pan[Random.Range(0, pan.Length)];
        var sankayaObj = sankaya[Random.Range(0, sankaya.Length)];
        
        _randomBlocks = new List<Block> {toptenObj, jellyObj, panObj, sankayaObj};
    }

    public void SpawnRandomBlock()
    {
        RandomType();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnPoints[i].IsFree)
            {
                continue;
            }
            Transform spawnTransform = spawnPoints[i].Transform;
            int randomIndex = Random.Range(0, _randomBlocks.Count);
            Block block = Instantiate(_randomBlocks[randomIndex], spawnTransform.position, Quaternion.identity);
            _randomBlocks.RemoveAt(randomIndex);
            block.SpawnIndex = i;
            block.transform.localScale = Vector3.zero;
            Vector3 scale = new Vector3(objectScale, objectScale, 1f);
            //int randomRotation = Random.Range(0, 4) * 90;
            //spawn.transform.eulerAngles = new Vector3(0, 0, randomRotation);
            _scaleTween = Tween.Scale(block.transform, scale, 0.2f);
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
                //spawnPoints[i].CurrentBlock.transform.DOKill();
                Destroy(spawnPoints[i].CurrentBlock.gameObject);
                FreeSpawnPoint(i);
            }
            else if (!spawnPoints[i].IsFree)
            {
                //spawnPoints[i].CurrentBlock.transform.DOKill();
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
    

    public void GameOverCheck()
    {
        StartCoroutine(GameOverCheckCoroutine());
    }

    private IEnumerator GameOverCheckCoroutine()
    {
        if (_scaleTween.isAlive)
            yield return _scaleTween.GetAwaiter();
        List<Block> blockToCheck = spawnPoints.Select(spawnPoint => spawnPoint.CurrentBlock).ToList();
        if (!GridManager.Instance.CheckAvailableBlock(blockToCheck, out _))
        {
            GameManager.Instance.GameOver(true);
        }
    }

    private void OnDestroy()
    {
        _scaleTween.Stop();
    }
}
