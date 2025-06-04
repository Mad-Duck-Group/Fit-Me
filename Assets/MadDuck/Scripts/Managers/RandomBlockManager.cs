using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
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
        [SerializeField] private Block _currentBlock;
        public Block CurrentBlock
        {
            get => _currentBlock;
            set => _currentBlock = value;
        }
    }

    [SerializeField] private float objectScale = 0.5f;
    [SerializeField] private GameObject[] topten;
    [SerializeField] private GameObject[] jelly;
    [SerializeField] private GameObject[] pan;
    [SerializeField] private GameObject[] sankaya;
    [FormerlySerializedAs("spawnPositions")] [SerializeField] private SpawnPoint[] spawnPoints;
    private List<GameObject> _randomObjects;
    private Tween _scaleTween;
    public SpawnPoint[] SpawnPoints => spawnPoints;

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
        GameObject toptenObj = topten[Random.Range(0, topten.Length)];
        GameObject jellyObj = jelly[Random.Range(0, jelly.Length)];
        GameObject panObj = pan[Random.Range(0, pan.Length)];
        GameObject sankayaObj = sankaya[Random.Range(0, sankaya.Length)];
        
        _randomObjects = new List<GameObject>() {toptenObj, jellyObj, panObj, sankayaObj};
        
        
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
            int randomIndex = Random.Range(0, _randomObjects.Count);
            Block spawn = Instantiate(_randomObjects[randomIndex], spawnTransform.position, Quaternion.identity).GetComponent<Block>();
            _randomObjects.RemoveAt(randomIndex);
            spawn.SpawnIndex = i;
            spawn.transform.localScale = Vector3.zero;
            Vector3 scale = new Vector3(objectScale, objectScale, 1f);
            //int randomRotation = Random.Range(0, 4) * 90;
            //spawn.transform.eulerAngles = new Vector3(0, 0, randomRotation);
            _scaleTween = Tween.Scale(spawn.transform, scale, 0.2f);
            spawn.OriginalPosition = spawn.transform.position;
            spawn.OriginalRotation = spawn.transform.eulerAngles;
            spawn.OriginalScale = scale;
            spawnPoints[i].IsFree = false;
            spawnPoints[i].CurrentBlock = spawn;
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
