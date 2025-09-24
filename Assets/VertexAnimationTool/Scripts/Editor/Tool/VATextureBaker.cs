using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using System.Collections.Generic;
using VertexAnimation.Editor;

namespace VertexAnimation
{
    public class VATextureBaker
    {
        private readonly VAToolDataManager _dataManager;
        private VABakingTextureSettings _settings => _dataManager.TextureSettings;
        private VATextureLayout _layout => _dataManager.TextureLayout;
        private AnimatedObjectInfo _objectInfo => _dataManager.Info;
        private Preset _currentPreset;
        
        public VATextureBaker(VAToolDataManager dataManager, Preset currentPreset)
        {
            _dataManager = dataManager;
            _currentPreset = currentPreset;
        }
        
        public bool Bake(VAData vaData)
        {
            if (_dataManager.SelectedStateCount == 0)
            {
                Debug.LogError("Bake failed - No selected states");
                return false;
            }

            var vertexAnimations = new List<VAData.VertexAnimation>();

            // Calculate texture size using VATextureLayout
            vaData.textureSize = _layout.TextureSize;
                        
            // Create baking instance
            var bakingInstance = CreateBakingInstance();
            var bakedMesh = new Mesh();
            
            // Calculate Bounds (Low Quality mode only)
            Bounds bounds = CalculateBounds(bakingInstance, bakedMesh);
            vaData.boundsMin = bounds.min;
            vaData.boundsMax = bounds.max;

            // Baking position and normal colors
            var positionColors = new Color[_layout.TexturePixelCount];
            var normalColors = new Color[_layout.TexturePixelCount];

            foreach(var state in _dataManager.Info.StateInfos)
            {
                if (!state.isSelected)
                    continue;
                
                for (int i = 0; i < state.ClipInfos.Length && i < 2; i++)
                {
                    var clipInfo = state.ClipInfos[i];
                    BakeFrames(clipInfo, bakingInstance, bakedMesh, positionColors, normalColors, bounds);

                    // Create VertexAnimation
                    var va = new VAData.VertexAnimation();
					va.name = clipInfo.clip.name;
                    va.length = clipInfo.clip.length;
                    va.startRow = clipInfo.startRow;
                    va.bakeFrameCount = clipInfo.frameCount;
                    va.isLoop = clipInfo.clip.isLooping;
                    vertexAnimations.Add(va);
                }
            }

            // Create and apply textures
            var positionTexture = new Texture2D(_layout.Width, _layout.Height, TextureFormat.RGBA32, false);
            var normalTexture = new Texture2D(_layout.Width, _layout.Height, TextureFormat.RGBA32, false);

            positionTexture.SetPixels(positionColors);
            positionTexture.Apply();
            
            normalTexture.SetPixels(normalColors);
            normalTexture.Apply();
            
            // Clean up
            Object.DestroyImmediate(bakingInstance);
            
            // Save textures
            SaveTextures(vaData, positionTexture, normalTexture);
            
            // Update VAData
            vaData.vertexAnimations = vertexAnimations.ToArray();

            return true;
        }
        
        private GameObject CreateBakingInstance()
        {
            var animator = _objectInfo.Animator;
            if (animator == null)
            {
                Debug.LogError("Animator not found");
                return null;
            }

            var instance = Object.Instantiate(animator.gameObject);
            instance.hideFlags = HideFlags.HideAndDontSave;

            var smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            ResetTransforms(smr.transform);

            return instance;
        }

        private void ResetTransforms(Transform trn)
        {
            if (trn == null)
                return;

            trn.localPosition = Vector3.zero;
            trn.localRotation = Quaternion.identity;
            trn.localScale = Vector3.one;
            ResetTransforms(trn.parent);
        }
        
        private Bounds CalculateBounds(GameObject bakingInstance, Mesh bakedMesh)
        {
            var smr = bakingInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            var minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var state in _dataManager.Info.StateInfos)
            {
                if (!state.isSelected)
                    continue;

                foreach(var info in state.ClipInfos)
                {
                    var frameStep = info.frameStep;
                    for (int frameIndex = 0; frameIndex < info.frameCount; frameIndex++)
                    {
                        float time = frameIndex * frameStep;
                        info.clip.SampleAnimation(bakingInstance, time);
                        smr.BakeMesh(bakedMesh);
                        
                        var vertices = bakedMesh.vertices;
                        for (int v = 0; v < _objectInfo.VertexCount; v++)
                        {
                            var vertex = vertices[v];
                            minPos = Vector3.Min(minPos, vertex);
                            maxPos = Vector3.Max(maxPos, vertex);
                        }
                    }
                }
            }
            
            return new Bounds((minPos + maxPos) * 0.5f, maxPos - minPos);
        }
        
        private void BakeFrames(AnimatedObjectInfo.ClipInfo clipInfo, GameObject bakingInstance, Mesh bakedMesh, Color[] positionColors, Color[] normalColors, Bounds bounds)
        {
            var smr = bakingInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            var vertexCount = _objectInfo.VertexCount;
            
            for (int frameIndex = 0; frameIndex < clipInfo.frameCount; frameIndex++)
            {
                float time = frameIndex * clipInfo.frameStep;
                clipInfo.clip.SampleAnimation(bakingInstance, time);
                smr.BakeMesh(bakedMesh);
                
                var vertices = bakedMesh.vertices;
                var normals = bakedMesh.normals;
                
                // Vertex data baking
                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    // Calculate pixel index using VATextureLayout
                    int pixelIndex = _layout.GetPixelIndex(clipInfo, vertexIndex, frameIndex);
                    
                    // Skip if pixel index is out of texture range
                    if (pixelIndex >= positionColors.Length)
                    {
                        Debug.LogWarning($"Pixel index {pixelIndex} is out of range for texture size {positionColors.Length}");
                        continue;
                    }
                    
                    var vertex = vertices[vertexIndex];
                    var normal = (normals != null && normals.Length > 0) ? normals[vertexIndex] : Vector3.up;
                    
                    // Position data
                    float px = Mathf.InverseLerp(bounds.min.x, bounds.max.x, vertex.x);
                    float py = Mathf.InverseLerp(bounds.min.y, bounds.max.y, vertex.y);
                    float pz = Mathf.InverseLerp(bounds.min.z, bounds.max.z, vertex.z);
                    positionColors[pixelIndex] = new Color(px, py, pz, 1.0f);

                    // Normal data
                    float nx = normal.x * 0.5f + 0.5f;
                    float ny = normal.y * 0.5f + 0.5f;
                    float nz = normal.z * 0.5f + 0.5f;
                    normalColors[pixelIndex] = new Color(nx, ny, nz, 1.0f);
                }
            }
        }

        private void SaveTextures(VAData vaData, Texture2D positionTexture, Texture2D normalTexture)
        {
            string safeFileName = VAToolDataManager.SanitizeFileName(_settings.outputName);
            string safePath = System.IO.Path.Combine(_settings.outputFolder, safeFileName);
            
            string positionPath = $"{safePath}_Positions{_settings.PositionTextureExtension}";
            SaveTexture(positionTexture, positionPath);
            vaData.positionTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(positionPath);

            string normalPath = $"{safePath}_Normals{_settings.NormalTextureExtension}";
            SaveTexture(normalTexture, normalPath);
            vaData.normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        }
        
        private void SaveTexture(Texture2D texture, string path)
        {            
            byte[] bytes = texture.EncodeToPNG();
            
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            
            // Apply preset
            ApplyPresetToTexture(path);
        }
        
        private void ApplyPresetToTexture(string texturePath)
        {
            if (_currentPreset != null)
            {
                // Reload saved texture
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    // Apply preset to texture
                    _currentPreset.ApplyTo(importer);
                    importer.SaveAndReimport();
                    //Debug.Log($"Applied preset '{_currentPreset.name}' to texture: {texturePath}");
                }
                else
                {
                    Debug.LogWarning($"Importer not found: {texturePath}");
                }
            }
            else
            {
                Debug.LogWarning("No preset available to apply to texture");
            }
        }
    }
} 