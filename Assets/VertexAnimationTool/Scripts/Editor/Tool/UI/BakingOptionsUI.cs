using UnityEngine;
using UnityEditor;

namespace VertexAnimation.Editor
{
    public class BakingOptionsUI
    {
        private readonly VAToolDataManager _dataManager;
        private readonly EditorPrefsManager _prefsManager;
        private readonly PresetManager _presetManager;
        private readonly VAPrefabService _prefabService;
        
        public BakingOptionsUI(VAToolDataManager dataManager, EditorPrefsManager prefsManager, PresetManager presetManager, VAPrefabService prefabService)
        {
            _dataManager = dataManager;
            _prefsManager = prefsManager;
            _presetManager = presetManager;
            _prefabService = prefabService;
        }
        
        public void DrawGlobalOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Baking Options", EditorStyles.boldLabel);

            DrawOutputFolder();
            DrawPrefabOptions();
            DrawShaderSelection();
        }
        
        public void DrawStateBakingOptions()
        {
            if (_dataManager.TargetObject == null)
            {
                return;
            }

            if (_dataManager.Info == null || _dataManager.Info.StateInfos.Count == 0)
            {
                EditorGUILayout.HelpBox("No states available.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview VA Texture Info", EditorStyles.boldLabel);
            DrawPreviewTextureInfo();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"State list({_dataManager.SelectedStateCount}/{_dataManager.Info.StateInfos.Count}) :", EditorStyles.boldLabel);

            DrawStateSelectionButtons();
            EditorGUILayout.Space();

            DrawStateList();
            DrawBakeButton();
        }
        
        private void DrawOutputFolder()
        {
            // Output Folder (global)
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Folder", GUILayout.Width(100));
            _dataManager.OutputFolder = EditorGUILayout.TextField(_dataManager.OutputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", _dataManager.OutputFolder, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert absolute path to project-relative path if possible
                    string projectPath = System.IO.Path.GetFullPath(Application.dataPath);
                    if (selectedPath.StartsWith(projectPath))
                    {
                        _dataManager.OutputFolder = "Assets" + selectedPath.Substring(projectPath.Length);
                    }
                    else
                    {
                        _dataManager.OutputFolder = selectedPath;
                    }
                    
                    // Save to EditorPrefs for persistence
                    _prefsManager.SetLastOutputFolder(_dataManager.OutputFolder);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                _dataManager.UpdateOutputFolder();
            }
        }
        
        private void DrawPrefabOptions()
        {
            // Add 'Create VA prefab' checkbox below the output folder input
            if (_dataManager.ModifyOriginalPrefab)
                _dataManager.CreateVAPrefab = false;

            _dataManager.CreateVAPrefab = EditorGUILayout.Toggle("Create VA prefab", _dataManager.CreateVAPrefab);

            if (_dataManager.CreateVAPrefab)
                _dataManager.ModifyOriginalPrefab = false;

            _dataManager.ModifyOriginalPrefab = EditorGUILayout.Toggle("Modify orginal prefab", _dataManager.ModifyOriginalPrefab);

            if (_dataManager.ModifyOriginalPrefab)
            {
                _dataManager.BackupOriginalPrefab = EditorGUILayout.Toggle("  - Backup orginal prefab", _dataManager.BackupOriginalPrefab);
            }
        }
        
        private void DrawShaderSelection()
        {
            // Show shader selection if Create VA prefab is enabled
            if (_dataManager.CreateVAPrefab || _dataManager.ModifyOriginalPrefab)
            {
                // Load shader from EditorPrefs if not set
                if (_dataManager.VAShaders == null)
                {
                    _dataManager.VAShaders = _prefsManager.GetLastVAShaders();
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("VA Shaders for multiple clips", EditorStyles.boldLabel);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    _dataManager.ModifyVAShaders(1);
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    _dataManager.ModifyVAShaders(-1);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                for (int i = 0; _dataManager.VAShaders != null && i < _dataManager.VAShaders.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (i > 0 && i == _dataManager.VAShaders.Length - 1)
                    {
                        EditorGUILayout.LabelField($"for {i+1}~ clips", GUILayout.Width(100));
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"for {i+1} clips", GUILayout.Width(100));
                    }
                    
                    Shader newShader = (Shader)EditorGUILayout.ObjectField(_dataManager.VAShaders[i], typeof(Shader), false);
                    if (newShader != _dataManager.VAShaders[i])
                    {
                        _dataManager.VAShaders[i] = newShader;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _prefsManager.SetLastVAShaders(_dataManager.VAShaders);
                }
            }
        }
        
        private void DrawStateSelectionButtons()
        {
            // 모두 선택/해제 버튼 (bakeable states만 대상)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                _dataManager.SelectAllBakeableStates();
            }
            if (GUILayout.Button("Select None"))
            {
                _dataManager.ClearSelectedStates();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStateList()
        {
            EditorGUI.BeginChangeCheck();
            _dataManager.SelectedStateCount = 0;
            foreach (var state in _dataManager.Info.StateInfos)
            {
                EditorGUILayout.BeginHorizontal();

                if (state.Bakeable)
                {
                    state.isSelected = EditorGUILayout.ToggleLeft(state.StateName, state.isSelected, GUILayout.Width(180));
                    _dataManager.SelectedStateCount += state.isSelected ? 1 : 0;
                }
                else
                {
                    EditorGUILayout.LabelField(state.StateName, GUILayout.Width(180));
                    EditorGUILayout.LabelField(state.NonBakeableReason);
                }
                
                EditorGUILayout.EndHorizontal();

                DrawStateBakingSettings(state);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _dataManager.TextureLayout.CalculateTextureSize();
            }
        }
        
        private void DrawStateBakingSettings(AnimatedObjectInfo.StateMotionInfo state)
        {
            EditorGUILayout.BeginVertical("box");

            foreach(var info in state.ClipInfos)
            {
                var clip = info.clip;
                EditorGUILayout.LabelField($"Clip: {clip.name} ({clip.frameRate}fps, {clip.length:F3}s, {info.totalFrameCount} frames)");

                info.frameCount = EditorGUILayout.IntSlider("Bake Frame Count", info.frameCount, 2, info.totalFrameCount);
                EditorGUILayout.LabelField($"Frame Step: {info.frameStep:F3}s");
            }
          
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewTextureInfo()
        {
            // Draw preview texture info for combined selected states
            if (_dataManager.SelectedStateCount == 0)
            {
                EditorGUILayout.HelpBox("No states selected.", MessageType.Warning);
                return;
            }

            _dataManager.TextureSettings.outputName = EditorGUILayout.TextField("Output Filename", _dataManager.TextureSettings.outputName);

            // Draw Calculate Texture Size
            var layout = _dataManager.TextureLayout;
            var texturePixelCount = layout.TexturePixelCount;
            _dataManager.TextureSettings.calculatedTextureResolution = layout.TextureSize;

            float bytesPerPixel = _dataManager.TextureSettings.BytesPerPixel;
            float memoryUsageMB = (texturePixelCount * bytesPerPixel) / (1024f * 1024f);
            int similarSize = Mathf.RoundToInt(Mathf.Sqrt(texturePixelCount));
            EditorGUILayout.LabelField($"Calculated Texture: {layout.Width}x{layout.Height} (≈{memoryUsageMB:F2} MB, similar {similarSize}x{similarSize})");

            // Draw Calculate Pixel Usage
            float pixelRatio = (float)layout.TotalPixelCount / texturePixelCount * 100f;
            EditorGUILayout.LabelField($"Pixel Usage: {pixelRatio:F1}% ({layout.TotalPixelCount:N0}/{texturePixelCount:N0})");
            EditorGUILayout.LabelField($"Pixels per Frame: {_dataManager.Info.VertexCount}");
        }
        
        private void DrawBakeButton()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("선택된 State Create VA Datas"))
            {
                var vaData = VACreateService.Create(_dataManager, _presetManager, _prefabService);
                if (vaData != null)
                {
                    EditorUtility.DisplayDialog("Bake Result", $"Successfully created : {vaData.name}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Bake Result", "Failed to create VA Data", "OK");
                }
            }
        }
    }
}
