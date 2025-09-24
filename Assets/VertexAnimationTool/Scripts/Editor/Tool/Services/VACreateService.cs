using UnityEditor;
using UnityEngine;
using System.IO;

namespace VertexAnimation.Editor
{
    public class VACreateService
    {
        public static VAData Create(VAToolDataManager dataManager, PresetManager presetManager, VAPrefabService prefabService)
        {
            var vaData = ScriptableObject.CreateInstance<VAData>();
            vaData.mesh = dataManager.Info.SkinnedMeshRenderer.sharedMesh;
            
            var preset = presetManager.GetPreset();

            // Create directory
            var path = dataManager.TextureSettings.outputFolder;
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string safeFileName = VAToolDataManager.SanitizeFileName(dataManager.Info.ObjectName);

            var baker = new VATextureBaker(dataManager, preset);
            baker.Bake(vaData);

            // Save ScriptableObject
            string vaDataPath = Path.Combine(path, $"{safeFileName}_VA.asset");
            AssetDatabase.CreateAsset(vaData, vaDataPath);

            // Create VA Prefab if enabled and we have successful bakes
            if (dataManager.CreateVAPrefab)
            {
                prefabService.CreateVAPrefab(vaData);
            }

            // not create VA prefab - backup original and modify original
            if (dataManager.ModifyOriginalPrefab && vaData.vertexAnimations.Length > 0)
            {
                var assetPath = AssetDatabase.GetAssetPath(dataManager.TargetObject);

                // backup original prefab
                if (dataManager.BackupOriginalPrefab)
                {
                    string backupPath = Path.Combine(Path.GetDirectoryName(assetPath), $"{Path.GetFileNameWithoutExtension(assetPath)}_backup.prefab");
                    prefabService.BackupOriginalPrefab(backupPath);
                }

                // modify original prefab
                try
                {
                    prefabService.ModifyOriginalPrefab(vaData);
                }
                catch (System.ArgumentException ex)
                {
                    EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                }
            }

            EditorUtility.SetDirty(vaData);
            //AssetDatabase.SaveAssetIfDirty(vaData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return vaData;
        }
    }
}