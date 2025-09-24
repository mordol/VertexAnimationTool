using UnityEditor;
using UnityEngine;

namespace VertexAnimation.Editor
{
    public class VertexAnimationToolWindow : EditorWindow
    {
        // Managers
        private VAToolDataManager _dataManager;
        private PresetManager _presetManager;
        private EditorPrefsManager _prefsManager;
        
        // Services
        private VAPrefabService _prefabService;
        
        // UI Components
        private BakingOptionsUI _bakingOptionsUI;
        private ObjectInfoUI _objectInfoUI;

        [MenuItem("Tools/Vertex Animation Tool", priority = 2000)]
        public static void ShowWindow()
        {
            var window = GetWindow<VertexAnimationToolWindow>("Vertex Animation Tool");
            window.Initialize();
        }

        private void Initialize()
        {
            // Initialize managers
            _dataManager = new VAToolDataManager();
            _presetManager = new PresetManager();
            _prefsManager = new EditorPrefsManager();
            
            // Initialize services
            _prefabService = new VAPrefabService(_dataManager);
            
            // Initialize UI components
            _bakingOptionsUI = new BakingOptionsUI(_dataManager, _prefsManager, _presetManager, _prefabService);
            _objectInfoUI = new ObjectInfoUI(_dataManager);
            
            // Restore settings
            RestoreLastSettings();
        }

        private void RestoreLastSettings()
        {
            _dataManager.OutputFolder = _prefsManager.GetLastOutputFolder();
            _dataManager.VAShaders = _prefsManager.GetLastVAShaders();
        }

        private void OnGUI()
        {
            if (_dataManager == null)
            {
                Initialize();
            }

            _dataManager.ScrollPos = EditorGUILayout.BeginScrollView(_dataManager.ScrollPos);

            // Draw UI sections
            _bakingOptionsUI.DrawGlobalOptions();
            GUILayout.Space(10);
            _objectInfoUI.Draw();

            _bakingOptionsUI.DrawStateBakingOptions();

            EditorGUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            // Save current settings
            if (_prefsManager != null)
            {
                _prefsManager.SetLastOutputFolder(_dataManager.OutputFolder);
                _prefsManager.SetLastVAShaders(_dataManager.VAShaders);
            }
        }
    }
} 