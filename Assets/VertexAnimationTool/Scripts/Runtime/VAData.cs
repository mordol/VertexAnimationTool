using UnityEngine;
using System.Collections.Generic;

namespace VertexAnimation
{
    [CreateAssetMenu(fileName = "VAData", menuName = "VAT/VA Data")]
    public class VAData : ScriptableObject
    {
        public Mesh mesh;
        public Material material;
        public int supportShaderVACount;    // support for multiple VA shader
        public int availableVACount => Mathf.Min(supportShaderVACount, vertexAnimations.Length);
        public int vertexCount => mesh.vertexCount;

        [Header("Textures")]
        public Texture2D positionTexture;
        public Texture2D normalTexture;
        public Vector2Int textureSize;

        [Header("Bounds")]
        public Vector3 boundsMin;
        public Vector3 boundsMax;

        [System.Serializable]
        public class VertexAnimation
        {
            public string name;
            public int startRow;
            public float length;
            public int bakeFrameCount;
            public bool isLoop; // Motion.isLooping
        }
        
        public VertexAnimation[] vertexAnimations;

        private Dictionary<int, int> _vaNameHashMap;

        public void OnEnable()
        {
            if (_vaNameHashMap != null)
                return;

            _vaNameHashMap = new Dictionary<int, int>();

            if (vertexAnimations == null)
                return;

            for (int i = 0; i < vertexAnimations.Length; i++)
            {
                int hash = Animator.StringToHash(vertexAnimations[i].name);
                _vaNameHashMap[hash] = i;
            }
        }

        public int GetVertexAnimationIndex(int vaNameHash)
        {
            return _vaNameHashMap.TryGetValue(vaNameHash, out int index) ? index : -1;
        }

        public VertexAnimation GetVertexAnimation(int vaNameHash)
        {
            int index = GetVertexAnimationIndex(vaNameHash);
            return index >= 0 ? vertexAnimations[index] : null;
        }
    }
} 