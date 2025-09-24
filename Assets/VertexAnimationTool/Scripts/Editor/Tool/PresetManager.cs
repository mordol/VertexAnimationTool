using UnityEngine;
using UnityEditor.Presets;
using UnityEditor;

namespace VertexAnimation.Editor
{
    public class PresetManager
    {
        private Preset _preset;
        
        public PresetManager()
        {
            LoadPresets();
        }
        
        public void LoadPresets()
        {
            // Find preset for and RGBA32
            string[]guids = AssetDatabase.FindAssets("Preset-RGBA32");
            if (guids.Length > 0)
            {
                string presetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            }
            else
            {
                Debug.LogWarning("No preset found with name: Preset-RGBA32");
            }
        }
        
        public Preset GetPreset()
        {
            if (_preset != null)
            {
                return _preset;
            }
            
            Debug.LogWarning($"No preset found for format: RGBA32");
            return null;
        }
    }
}
