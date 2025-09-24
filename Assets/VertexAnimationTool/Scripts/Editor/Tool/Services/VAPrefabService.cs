using UnityEngine;
using UnityEditor;
using System.IO;

namespace VertexAnimation.Editor
{
    public class VAPrefabService
    {
        private readonly VAToolDataManager _dataManager;
        
        public VAPrefabService(VAToolDataManager dataManager)
        {
            _dataManager = dataManager;
        }
        
        public GameObject CreateVAPrefab(VAData vaData)
        {
            try
            {
                Validate(vaData);
            }
            catch (System.ArgumentException ex)
            {
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                return null;
            }

            GameObject copyC = Object.Instantiate(_dataManager.TargetObject);
            copyC.name = _dataManager.TargetObject.name + "_VA_Copy";
            
            // Clean up hide flags on the entire hierarchy to ensure prefab creation works
            CleanupHideFlags(copyC);

            if (!ChangeTargetObject(copyC, vaData))
            {
                Object.DestroyImmediate(copyC);
                EditorUtility.DisplayDialog("Error", $"Failed to change target object. {_dataManager.TargetObject.name}", "OK");
                return null;
            }

            var path = _dataManager.TextureSettings.outputFolder;
            string prefabPath = Path.Combine(path, $"{_dataManager.TargetObject.name}-VA.prefab");
            string prefabAssetPath = prefabPath.Replace('\\', '/');

            // Create the prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copyC, prefabAssetPath);
            
            // Clean up the temporary copy
            Object.DestroyImmediate(copyC);
            
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Failed to create prefab at {prefabAssetPath}");
            }

            Debug.Log($"VA Prefab created successfully: {prefabAssetPath}");
            return prefab;
        }

        public void ModifyOriginalPrefab(VAData vaData)
        {
            try
            {
                Validate(vaData);
            }
            catch (System.ArgumentException ex)
            {
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                return;
            }

            var prefabPath = AssetDatabase.GetAssetPath(_dataManager.TargetObject);
            var targetObject = PrefabUtility.LoadPrefabContents(prefabPath);

            if (!ChangeTargetObject(targetObject, vaData))
            {
                EditorUtility.DisplayDialog("Error", $"Failed to change target object. {_dataManager.TargetObject.name}", "OK");
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(targetObject, prefabPath);
            PrefabUtility.UnloadPrefabContents(targetObject);
        }
        
        private bool ChangeTargetObject(GameObject targetObject, VAData vaData)
        {
            var animator = targetObject.GetComponentInChildren<Animator>();
            GameObject gameObjectA = animator?.gameObject;
            if (gameObjectA == null)
            {
                Debug.LogError($"No GameObject with Animator found in hierarchy. {targetObject.name}");
                return false;
            }

            RemoveRootGameObject(gameObjectA);

            animator = targetObject.GetComponentInChildren<Animator>();
            gameObjectA = animator?.gameObject;
            if (gameObjectA == null)
            {
                Debug.LogError($"No GameObject with Animator found in hierarchy. {targetObject.name}");
                return false;
            }

            var rmAnimator = gameObjectA.GetComponent<Animator>();
            if (rmAnimator != null)
            {
                Object.DestroyImmediate(rmAnimator, true);
                rmAnimator = null;
            }

            var smr = gameObjectA.GetComponentInChildren<SkinnedMeshRenderer>();
            GameObject gameObjectB = smr.gameObject;
            if (gameObjectB == null)
            {
                Debug.LogError($"No GameObject with SkinnedMeshRenderer found in hierarchy. {targetObject.name}");
                return false;
            }

            CreateVAMaterial(smr, vaData);
            
            var vertexAnimationInstance = gameObjectA.GetComponent<VertexAnimationInstance>();
            if (vertexAnimationInstance == null)
            {
                vertexAnimationInstance = gameObjectA.AddComponent<VertexAnimationInstance>();
            }
            vertexAnimationInstance.m_VAData = vaData;

            smr = null;
            Object.DestroyImmediate(gameObjectB, true);

            return true;
        }
        
        private void Validate(VAData vaData)
        {
            if (_dataManager.TargetObject == null)
            {
                throw new System.ArgumentException("Target object is null.");
            }
            
            if (vaData == null || vaData.vertexAnimations == null || vaData.vertexAnimations.Length == 0)
            {
                throw new System.ArgumentException("VADatas list is null or empty.");
            }
            
            if (string.IsNullOrEmpty(_dataManager.TextureSettings.outputFolder))
            {
                throw new System.ArgumentException("Output folder is not set.");
            }
            
            if (_dataManager.VAShaders == null)
            {
                throw new System.ArgumentException("VA Shader is not assigned. Please assign a shader in the tool window.");
            }
        }
        
        private void CreateVAMaterial(SkinnedMeshRenderer skinnedMeshRenderer, VAData vaData)
        {
            if (_dataManager.VAShaders == null)
            {
                throw new System.InvalidOperationException("VA Shader is not assigned.");
            }

            var vaCount = vaData.vertexAnimations.Length;

            // Get the original materials
            Material[] originalMaterials = skinnedMeshRenderer.sharedMaterials;
            if (originalMaterials == null || originalMaterials.Length == 0 || originalMaterials[0] == null)
            {
                throw new System.InvalidOperationException("Original materials are null or empty.");
            }

            if (originalMaterials.Length > 1)
            {
                Debug.LogWarning($"Multiple materials found ({originalMaterials.Length}). Only the first material({originalMaterials[0].name}) will be used for the VA prefab.");
            }

            var vaMaterial = new Material(originalMaterials[0]);
            // Copy material
            var index = Mathf.Clamp(vaCount - 1, 0, _dataManager.VAShaders.Length - 1);
            vaMaterial.shader = _dataManager.VAShaders[index];
            vaMaterial.name = $"{_dataManager.TargetObject.name}-VA";
            vaMaterial.enableInstancing = true;
            index = vaMaterial.shader.FindPropertyIndex("_MaxVaCount");
            vaData.supportShaderVACount = vaMaterial.shader.GetPropertyDefaultIntValue(index);

            // Set vaMaterial
            vaMaterial.SetTexture(InternalInstanceDrawer.s_VaPositionTexId, vaData.positionTexture);
            vaMaterial.SetTexture(InternalInstanceDrawer.s_VaNormalTexId, vaData.normalTexture);
            vaMaterial.SetFloat(InternalInstanceDrawer.s_VaTextureWidthId, vaData.textureSize.x);
            vaMaterial.SetFloat(InternalInstanceDrawer.s_VaTextureHeightId, vaData.textureSize.y);
            vaMaterial.SetFloat(InternalInstanceDrawer.s_VaVertexCountId, vaData.vertexCount);
            vaMaterial.SetVector(InternalInstanceDrawer.s_VaBoundMinId, vaData.boundsMin);
            vaMaterial.SetVector(InternalInstanceDrawer.s_VaBoundMaxId, vaData.boundsMax);

            // Save the material to the output folder
            var path = _dataManager.TextureSettings.outputFolder;
            string materialPath = Path.Combine(path, $"{vaMaterial.name}.mat");
            string materialAssetPath = materialPath.Replace('\\', '/');
            AssetDatabase.CreateAsset(vaMaterial, materialAssetPath);
            AssetDatabase.SaveAssets();

            vaData.material = vaMaterial;
        }
        
        private void RemoveRootGameObject(GameObject root)
        {
            var smr = root.GetComponentInChildren<SkinnedMeshRenderer>();
            Transform rootBone = smr?.rootBone;

            // Find root
            while (rootBone != null)
            {
                var parent = rootBone.parent;
                if (parent == null)
                {
                    break;
                }

                var components = parent.GetComponents<Component>();
                //Debug.Log($"{parent.name}: {components.Length}");
                if (components.Length > 1)
                {
                    break;
                }
                
                rootBone = parent;
            }

            if (rootBone != null)
            {
                CloneAndSetParentEffectiveNode(rootBone.gameObject, root);
                Object.DestroyImmediate(rootBone.gameObject, true);
            }
            
            // Find 'Root' GameObject (case insensitive)
            rootBone = null;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name.Equals("Root", System.StringComparison.OrdinalIgnoreCase))
                {
                    rootBone = child;
                    break;
                }
            }
            
            if (rootBone != null)
            {
                CloneAndSetParentEffectiveNode(rootBone.gameObject, root);
                Object.DestroyImmediate(rootBone.gameObject, true);
            }
        }

        private void CloneAndSetParentEffectiveNode(GameObject boneRoot, GameObject parent)
        {
            var targetObjects = new System.Collections.Generic.HashSet<GameObject>();

            // Find non-Transform component's GameObject
            var components = boneRoot.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component is Transform)
                {
                    continue;
                }
                targetObjects.Add(component.gameObject);
            }

            // Copy and set parent
            foreach (var targetObject in targetObjects)
            {                
                var clone = Object.Instantiate(targetObject, parent.transform, true);
                clone.name = targetObject.name;

                // Remove all children of clone
                foreach (Transform child in clone.transform)
                {
                    Object.DestroyImmediate(child.gameObject, true);
                }
            }
        }
                
        private void CleanupHideFlags(GameObject gameObject)
        {
            if (gameObject == null) return;
            
            // Clear hide flags on this GameObject
            gameObject.hideFlags = HideFlags.None;
            
            // Recursively clean up all children
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                Transform child = gameObject.transform.GetChild(i);
                CleanupHideFlags(child.gameObject);
            }
        }

        public void BackupOriginalPrefab(string backupPath)
        {
            if (string.IsNullOrEmpty(backupPath))
            {
                Debug.LogError("Backup path is null or empty.");
                return;
            }
            
            PrefabUtility.SaveAsPrefabAsset(_dataManager.TargetObject, backupPath);
            Debug.Log($"Original prefab backed up to: {backupPath}");
        }
    }
}
