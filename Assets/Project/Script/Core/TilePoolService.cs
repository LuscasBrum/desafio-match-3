using System.Collections.Generic;
using UnityEngine;
using Gazeus.DesafioMatch3.ScriptableObjects;


namespace Gazeus.DesafioMatch3.Controllers
{
    public class TilePoolService
    {
        private readonly TilePrefabRepository _repo;
        private readonly Transform _poolRoot;
        private readonly Dictionary<int, Stack<GameObject>> _pool = new();
        private readonly Dictionary<GameObject, int> _reverseType = new();

        public TilePoolService(TilePrefabRepository repo, Transform poolRoot)
        {
            _repo = repo;
            _poolRoot = poolRoot;

            for (int i = 0; i < _repo.TileTypePrefabList.Length; i++)
                _pool[i] = new Stack<GameObject>(64);
        }

        public void Prewarm(int[] types, int countPerType)
        {
            for (int t = 0; t < types.Length; t++)
            {
                int type = types[t];
                for (int i = 0; i < countPerType; i++)
                {
                    var go = Object.Instantiate(_repo.TileTypePrefabList[type], _poolRoot);
                    Prepare(go, type);
                    go.SetActive(false);
                    _pool[type].Push(go);
                }
            }
        }

        public GameObject Get(int type)
        {
            GameObject go = _pool[type].Count > 0
                ? _pool[type].Pop()
                : Object.Instantiate(_repo.TileTypePrefabList[type], _poolRoot);

            if (!go.TryGetComponent(out PooledTile pt))
                pt = go.AddComponent<PooledTile>();
            pt.TypeId = type;

            _reverseType[go] = type;
            go.transform.SetParent(_poolRoot, false);
            go.SetActive(true);
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;

            int type = 0;
            if (!_reverseType.TryGetValue(go, out type))
            {
                if (go.TryGetComponent(out PooledTile pt)) type = pt.TypeId;
            }

            go.transform.SetParent(_poolRoot, false);
            go.SetActive(false);

            if (!_pool.ContainsKey(type))
                _pool[type] = new Stack<GameObject>();

            _pool[type].Push(go);
            _reverseType[go] = type;
        }

        private static void Prepare(GameObject go, int type)
        {
            if (!go.TryGetComponent(out PooledTile pt))
                pt = go.AddComponent<PooledTile>();
            pt.TypeId = type;
        }
    }
}