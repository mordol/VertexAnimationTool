using UnityEngine;
using UnityEditor;
using VertexAnimation;

[CustomEditor(typeof(VertexAnimationInstance))]
public class VertexAnimationInstanceEditor : Editor
{
    private string[] _animationNames;

    private void OnEnable()
    {
        if (_animationNames != null)
            return;

        VertexAnimationInstance animator = (VertexAnimationInstance)target;
        _animationNames = new string[animator.m_VAData.availableVACount];
        for (int i = 0; i < animator.m_VAData.availableVACount; i++)
        {
            _animationNames[i] = animator.m_VAData.vertexAnimations[i].name;
        }
    }

    public override void OnInspectorGUI()
    {
        // Show default fields
        //DrawDefaultInspector();

        // Get VertexAnimationInstance
        VertexAnimationInstance vaInstance = (VertexAnimationInstance)target;

        EditorGUILayout.ObjectField("VA Data", vaInstance.m_VAData, typeof(VertexAnimation.VAData), false);

        // Show button if m_VADatas is not null and has elements
        if (vaInstance.m_VAData == null)
        {
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Animation");

        // Show dropdown for animation
        var animationIndex = vaInstance.m_VAIndex;
        animationIndex = EditorGUILayout.Popup(animationIndex, _animationNames);
        if (animationIndex != vaInstance.m_VAIndex)
        {
            vaInstance.Play(_animationNames[animationIndex]);
        }

        EditorGUILayout.EndHorizontal();

        var speed = EditorGUILayout.Slider(vaInstance.speed, 0, 3);
        if (!Mathf.Approximately(speed, vaInstance.speed))
        {
            vaInstance.speed = speed;
        }

        if (GUILayout.Button("Reset animation"))
        {
            vaInstance.Reset();
        }
    }
}
