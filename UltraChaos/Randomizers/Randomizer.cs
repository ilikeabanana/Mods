using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    public abstract class Randomizer<T> where T : class
    {
        public List<T> Pool { get; } = new List<T>();

        private readonly Dictionary<int, T> _map = new Dictionary<int, T>();
        private readonly HashSet<int> _usedIds = new HashSet<int>();

        protected virtual int NextIndex(int count) => Random.Range(0, count);

        protected abstract int GetInstanceID(T item);

        protected abstract RandomConfigValue GetConfigValue();

        protected virtual void Log(string message) { }

        public void ResetMappings()
        {
            _map.Clear();
            _usedIds.Clear();
        }

        public void AddToPool(T item)
        {
            if (item == null) return;
            int id = GetInstanceID(item);
            if (Pool.All(p => GetInstanceID(p) != id))
                Pool.Add(item);
        }

        public virtual void Initialize() { }

        public void AddRangeToPool(IEnumerable<T> items)
        {
            foreach (var item in items)
                AddToPool(item);
        }

        public T GetRandom(T original = null, List<T> CPool = null)
        {
            if (CPool == null) CPool = Pool;
            if (CPool.Count == 0)
                return original;

            switch (GetConfigValue())
            {
                case RandomConfigValue.Disabled:
                    return original;

                case RandomConfigValue.AlwaysUnique:
                    return CPool[NextIndex(CPool.Count)];

                case RandomConfigValue.UniquePerKindWithDuplicates:
                    {
                        int key = original != null ? GetInstanceID(original) : 0;
                        if (!_map.TryGetValue(key, out T mapped))
                        {
                            mapped = CPool[NextIndex(CPool.Count)];
                            _map[key] = mapped;
                        }
                        return mapped;
                    }

                case RandomConfigValue.UniquePerKind:
                    {
                        int key = original != null ? GetInstanceID(original) : 0;
                        if (!_map.TryGetValue(key, out T mapped))
                        {
                            List<T> available = CPool
                                .Where(item => !_usedIds.Contains(GetInstanceID(item)) && GetInstanceID(item) != key)
                                .ToList();

                            Log($"[UniquePerKind] key={key} | pool={CPool.Count} | usedIds=[{string.Join(",", _usedIds)}] | available={available.Count}");

                            if (available.Count == 0)
                            {
                                available = CPool.Where(item => GetInstanceID(item) != key).ToList();
                                Log($"[UniquePerKind] key={key} | pool exhausted, falling back to {available.Count} candidates");
                            }

                            if (available.Count == 0)
                                return original;

                            mapped = available[NextIndex(available.Count)];
                            _usedIds.Add(GetInstanceID(mapped));
                            _map[key] = mapped;

                            Log($"[UniquePerKind] key={key} -> assigned id={GetInstanceID(mapped)} | usedIds now=[{string.Join(",", _usedIds)}]");
                        }
                        else
                        {
                            Log($"[UniquePerKind] key={key} -> already mapped to id={GetInstanceID(mapped)} (cache hit)");
                        }
                        return mapped;
                    }

                default:
                    return original;
            }
        }
    }
}