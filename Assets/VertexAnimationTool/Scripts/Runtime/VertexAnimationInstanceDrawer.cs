using UnityEngine;
using System.Collections.Generic;

namespace VertexAnimation
{
    public class VertexAnimationInstanceDrawer : MonoBehaviour
    {
        // Singleton instance (valid only in scene)
        private static VertexAnimationInstanceDrawer _instance;
        public static VertexAnimationInstanceDrawer Instance
        {
            get
            {
                return _instance;
            }
        }

        // Instance drawer management
        private List<InternalInstanceDrawer> _instanceDrawers = new List<InternalInstanceDrawer>();
        private Dictionary<VAData, List<InternalInstanceDrawer>> _instanceDrawerMap = new Dictionary<VAData, List<InternalInstanceDrawer>>();

        public static InternalInstanceDrawer GetInstanceDrawer(int drawerIndex)
        {
            if (_instance == null)
            {
                // Debug.Log($"VertexAnimationInstanceDrawer is not initialized - GetInstanceDrawer {drawerIndex}");
                return null;
            }

            return _instance.GetInstanceDrawerByIndex(drawerIndex);
        }

        public int maxInstanceCount = 128;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            //Debug.Log($"VertexAnimationInstanceDrawer is initialized - {_instance.name}");
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var drawer in _instanceDrawers)
            {
                drawer.Update();
                drawer.Draw();
            }
        }

        public InternalInstanceDrawer GetInstanceDrawerByIndex(int drawerIndex)
        {
            if (drawerIndex < 0 || drawerIndex >= _instanceDrawers.Count)
            {
                Debug.Log($"VertexAnimationInstanceDrawer - GetInstanceDrawer - index out of range {drawerIndex}");
                return null;
            }

            return _instanceDrawers[drawerIndex];
        }

        private InternalInstanceDrawer GetAvailableInstanceDrawer(VertexAnimationInstance va)
        {
            _instanceDrawerMap.TryGetValue(va.m_VAData, out var drawerList);
            if (drawerList == null)
            {
                drawerList = new List<InternalInstanceDrawer>();
                _instanceDrawerMap[va.m_VAData] = drawerList;
                return CreateInstanceDrawer(va.m_VAData, ref drawerList);
            }

            //return drawerList.Find(drawer => drawer.instanceCount < drawer.maxInstanceCount);
            foreach (var drawer in drawerList)
            {
                if (drawer.instanceCount < drawer.maxInstanceCount)
                {
                    return drawer;
                }
            }

            return CreateInstanceDrawer(va.m_VAData, ref drawerList);
        }

        private InternalInstanceDrawer CreateInstanceDrawer(VAData vaData, ref List<InternalInstanceDrawer> drawerList)
        {
            var drawer = new InternalInstanceDrawer(vaData, maxInstanceCount, _instanceDrawers.Count);
            _instanceDrawers.Add(drawer);
            drawerList.Add(drawer);
            return drawer;
        }


#region VertexAnimationInstance management
        public static bool Register(VertexAnimationInstance va)
        {
            if (_instance == null)
            {
                Debug.Log($"VertexAnimationInstanceDrawer is not initialized - Register {va.name}");
                return false;
            }

            _instance.GetAvailableInstanceDrawer(va).Add(va);

            //Debug.Log($"Register {va.m_VAData.name} - drawer count: {_instance._instanceDrawers.Count}, instance count: {instanceDrawer.instanceCount}");
            return true;
        }

        public static bool Unregister(VertexAnimationInstance va)
        {
            if (_instance == null)
            {
                // Debug.Log($"VertexAnimationInstanceDrawer is not initialized - Unregister {va.name}");
                return false;
            }

            var instanceDrawer = GetInstanceDrawer(va.drawerIndex);

            if (instanceDrawer != null)
            {
                instanceDrawer.Remove(va);

                //Debug.Log($"Unregister {va.m_VAData.name} - drawer count: {_instance._instanceDrawers.Count}, instance count: {instanceDrawer.instanceCount}");
            }

            return true;
        }
#endregion
    }
}