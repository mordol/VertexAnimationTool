using UnityEditor;
using VertexAnimation;
using System.Reflection;

[CustomEditor(typeof(VertexAnimationInstanceDrawer))]
public class VertexAnimationInstanceDrawerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Show default fields
        DrawDefaultInspector();

        VertexAnimationInstanceDrawer instance = (VertexAnimationInstanceDrawer)target;

        // get private field _instanceDrawers using reflection
        var instanceDrawers = instance.GetType().GetField("_instanceDrawers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) as System.Collections.Generic.List<InternalInstanceDrawer>;
        if (instanceDrawers == null || instanceDrawers.Count == 0)
        {
            EditorGUILayout.LabelField($"No instance drawers");
            return;
        }

        var totalInstanceCount = 0;
        foreach (var instanceDrawer in instanceDrawers)
        {
            totalInstanceCount += instanceDrawer.instanceCount;
        }

        EditorGUILayout.LabelField($"Instance drawers: {instanceDrawers.Count}, Total instances: {totalInstanceCount}", EditorStyles.boldLabel);

        // list all instance drawers
        foreach (var instanceDrawer in instanceDrawers)
        {
            EditorGUILayout.LabelField($"{instanceDrawer.vaData.name} - {instanceDrawer.instanceCount}/{instanceDrawer.maxInstanceCount}");
        }
    }
}