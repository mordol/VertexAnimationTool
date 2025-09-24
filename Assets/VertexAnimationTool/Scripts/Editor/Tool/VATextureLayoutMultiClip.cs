using UnityEngine;

namespace VertexAnimation.Editor
{
    public class VATextureLayoutMultiClip : VATextureLayout
    {
        public VATextureLayoutMultiClip(int vertexCount, VAToolDataManager dataManager) : base(vertexCount, dataManager)
        {
            CalculateTextureSize();
        }

        public override void CalculateTextureSize()
        {
            // row - frame, column - vertex
            var totalFrameCount = 0;

            for(int i = 0; i < _states.Count; i++)
            {
                if(!_states[i].isSelected)
                    continue;

                foreach(var clip in _states[i].ClipInfos)
                {
                    totalFrameCount += clip.frameCount;
                }
            }

            _textureSize = new Vector2Int(_vertexCount, totalFrameCount);
            _totalPixelCount = UpdateStartRow(0);
        }

        private int UpdateStartRow(int size)
        {
            var requiredSize = 0;
            var startRow = 0;
            for(int i = 0; i < _states.Count; i++)
            {
                if(!_states[i].isSelected)
                    continue;

                foreach(var clip in _states[i].ClipInfos)
                {
                    clip.startRow = startRow;
                    var row = clip.frameCount;
                    startRow += row;
                    requiredSize += row * _vertexCount;
                }
            }

            return requiredSize;
        }

        // Return texture pixel array index
        public override int GetPixelIndex(AnimatedObjectInfo.ClipInfo clipInfo, int vertexIndex, int frameIndex)
        {
            var stateBase = clipInfo.startRow * Width;
            var frameBase = frameIndex * Width;
            return stateBase + frameBase + vertexIndex;
        }
    }
}