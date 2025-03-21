using System;
using UnityEngine;

namespace Drydock.Tools
{
    public interface IPoolable
    {
        void OnGetFromPool(Vector3 position, Quaternion rotation);

        void OnReturnToPool();
    }

    public class PoolHandle : MonoBehaviour
    {
        [Header("POOL SETTINGS:")]
        public int initialPoolSize = 20;

        public bool growOnExhaustion = true;

        [HideInInspector]
        public Pool pool;

        public event Action<PoolHandle> OnAwakened;

        public event Action<PoolHandle> OnDisabled;

        public IPoolable[] poolables;

        public void CachePoolables() => poolables = GetComponentsInChildren<IPoolable>();

        private void OnEnable() => OnAwakened?.Invoke(this);

        private void OnDisable()
        {
            OnDisabled?.Invoke(this);
            if(pool != null && pool.isActiveAndEnabled)
                pool.Reparent(this);
        }
    }
}
