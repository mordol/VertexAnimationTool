using UnityEngine;
using System.IO;

namespace VertexAnimation.Editor
{
    public class VAToolDataManager
    {
        // Target Object
        public GameObject TargetObject { get; private set; }
        public AnimatedObjectInfo Info { get; private set; }

        public VABakingTextureSettings TextureSettings { get; private set; }
        public VATextureLayout TextureLayout => _texturelayout;
        public int BakeableStateCount { get; private set; }
        
        // UI
        public Vector2 ScrollPos { get; set; }
        public int SelectedStateCount { get; set; }
        
        // Baking options
        public string OutputFolder { get; set; }
        public string OutputName { get; set; }
        public bool CreateVAPrefab { get; set; }
        public Shader[] VAShaders { get; set; }
        public bool ModifyOriginalPrefab { get; set; }
        public bool BackupOriginalPrefab { get; set; }
        public bool BatchingAssets { get; set; }
        
        private VATextureLayout _texturelayout;
        
        public VAToolDataManager()
        {
            TargetObject = null;
            TextureSettings = new VABakingTextureSettings();
            
            // Default values
            CreateVAPrefab = true;
            ModifyOriginalPrefab = false;
            BackupOriginalPrefab = false;
            BatchingAssets = false;
        }
        
        public void UpdateTargetObject(GameObject newTarget)
        {
            TargetObject = newTarget;
            Info = newTarget != null ? AnimatedObjectInfo.FromGameObject(newTarget) : null;

            BakeableStateCount = 0;
            SelectedStateCount = 0;
            
            if (Info == null || Info.StateInfos == null || Info.StateInfos.Count <= 0)
                return;

            foreach(var state in Info.StateInfos)
            {
                state.isSelected = state.Bakeable;    // default setting

                foreach(var clip in state.ClipInfos)
                {
                    clip.frameCount = Mathf.CeilToInt(clip.clipTotalFrames * 0.5f);    // Set initial frame count
                }
                BakeableStateCount += state.Bakeable ? 1 : 0;
            }

            SelectedStateCount = BakeableStateCount;
            TextureSettings.outputName = TargetObject.name;
            UpdateOutputFolder();
            ResetTextureLayout();
        }

        public void ResetTextureLayout()
        {
            if (Info == null)
                return;

            _texturelayout = VATextureLayout.Create(Info.VertexCount, this);
        }
        
        public void ClearSelectedStates()
        {
            Info.StateInfos.ForEach(state => state.isSelected = false);
        }
        
        public void SelectAllBakeableStates()
        {
            Info.StateInfos.ForEach(state => state.isSelected = state.Bakeable);
        }

        public void UpdateOutputFolder()
        {
            TextureSettings.outputFolder = Path.Combine(OutputFolder, TextureSettings.outputName);
        }

        public void ModifyVAShaders(int count)
        {
            var newCount = count;
            if (VAShaders != null)
            {
                newCount += VAShaders.Length;
            }

            newCount = Mathf.Max(0, newCount);
            if (newCount == 0)
            {
                VAShaders = null;
                return;
            }

            var newVAShaders = new Shader[newCount];
            for (int i = 0; VAShaders != null && i < VAShaders.Length && i < newCount; i++)
            {
                newVAShaders[i] = VAShaders[i];
            }

            VAShaders = newVAShaders;
        }

        public static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
}
