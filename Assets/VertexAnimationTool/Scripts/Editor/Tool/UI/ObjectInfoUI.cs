using UnityEngine;
using UnityEditor;

namespace VertexAnimation.Editor
{
    public class ObjectInfoUI
    {
        private readonly VAToolDataManager _dataManager;
        
        public ObjectInfoUI(VAToolDataManager dataManager)
        {
            _dataManager = dataManager;
        }
        
        public void Draw()
        {
            EditorGUILayout.LabelField("Target GameObject", EditorStyles.boldLabel);
            var newTarget = (GameObject)EditorGUILayout.ObjectField(_dataManager.TargetObject, typeof(GameObject), true);
            if (newTarget != _dataManager.TargetObject)
            {
                _dataManager.UpdateTargetObject(newTarget);
            }

            EditorGUILayout.Space();

            if (_dataManager.Info == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject to display its information.", MessageType.Info);
                return;
            }

            DrawSkinnedMeshInfo();
            DrawAnimatorInfo();
            DrawAnimatorControllerInfo();
            DrawStateCountInfo();
            DrawMaterialInfo();
            DrawMeshVertexCountInfo();
            DrawBoundsInfo();
        }
        
        private void DrawSkinnedMeshInfo()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SkinnedMeshRenderer:", GUILayout.Width(150));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(_dataManager.Info.SkinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawAnimatorInfo()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Animator:", GUILayout.Width(150));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(_dataManager.Info.Animator, typeof(Animator), true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawAnimatorControllerInfo()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Animator Controller:", GUILayout.Width(150));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(_dataManager.Info.AnimatorController, typeof(RuntimeAnimatorController), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStateCountInfo()
        {
            EditorGUILayout.BeginHorizontal();
            int totalStates = _dataManager.Info.StateInfos != null ? _dataManager.Info.StateInfos.Count : 0;
            int bakeableStatesCount = _dataManager.BakeableStateCount;
            EditorGUILayout.LabelField($"State Count ({bakeableStatesCount}):", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{totalStates} total, {bakeableStatesCount} bakeable");
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawMaterialInfo()
        {
            EditorGUILayout.BeginHorizontal();
            int materialCount = _dataManager.Info.Materials != null ? _dataManager.Info.Materials.Count : 0;
            EditorGUILayout.LabelField($"Materials ({materialCount}):", GUILayout.Width(150));
            EditorGUI.BeginDisabledGroup(true);
            if (_dataManager.Info.Materials != null && _dataManager.Info.Materials.Count > 0)
            {
                EditorGUILayout.ObjectField(_dataManager.Info.Materials[0], typeof(Material), false);
                if (_dataManager.Info.Materials.Count > 1)
                {
                    EditorGUILayout.LabelField($"+{_dataManager.Info.Materials.Count - 1} more");
                }
            }
            else
            {
                EditorGUILayout.LabelField("None");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawMeshVertexCountInfo()
        {
            // Mesh vertex count
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mesh Vertex Count:", GUILayout.Width(150));
            EditorGUILayout.LabelField(_dataManager.Info.VertexCount.ToString());
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawBoundsInfo()
        {
            // Bounds
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bounds Center:", GUILayout.Width(150));
            EditorGUILayout.LabelField(_dataManager.Info.ObjectBounds.center.ToString("F4"));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bounds Size:", GUILayout.Width(150));
            EditorGUILayout.LabelField(_dataManager.Info.ObjectBounds.size.ToString("F4"));
            EditorGUILayout.EndHorizontal();
        }
    }
}
