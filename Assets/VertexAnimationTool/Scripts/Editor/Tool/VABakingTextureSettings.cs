using UnityEngine;

namespace VertexAnimation
{
    [System.Serializable]
    public class VABakingTextureSettings
    {
        [Header("Texture Settings")]
        public Vector2Int calculatedTextureResolution = new Vector2Int(512, 512);
        public string outputName = "";
        public string outputFolder = "";
        
        // Calculated properties
        public float BytesPerPixel => 4f; // RGBA32
        public string PositionTextureExtension => ".png";
        public string NormalTextureExtension => ".png";
    }
} 