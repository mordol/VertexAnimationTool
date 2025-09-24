using System.Collections.Generic;
using UnityEngine;

namespace VertexAnimation.Editor
{
    public abstract class VATextureLayout
    {
        public Vector2Int TextureSize => _textureSize;
        public int Width => _textureSize.x;
        public int Height => _textureSize.y;
        public int VertexCount => _vertexCount;
        public int TexturePixelCount => _textureSize.x * _textureSize.y;
        public int TotalPixelCount => _totalPixelCount;

        protected int _vertexCount;
        protected Vector2Int _textureSize;
        protected int _totalPixelCount;
        protected VAToolDataManager _dataManager;

        protected VABakingTextureSettings _settings => _dataManager?.TextureSettings;
        protected List<AnimatedObjectInfo.StateMotionInfo> _states => _dataManager?.Info.StateInfos;

        protected VATextureLayout(int vertexCount, VAToolDataManager dataManager)
        {
            _vertexCount = vertexCount;
            _dataManager = dataManager;
        }

        public static VATextureLayout Create(int vertexCount, VAToolDataManager dataManager)
        {
            return new VATextureLayoutMultiClip(vertexCount, dataManager);
        }

        public abstract void CalculateTextureSize();

        public abstract int GetPixelIndex(AnimatedObjectInfo.ClipInfo clipInfo, int vertexIndex, int frameIndex);
    }
}