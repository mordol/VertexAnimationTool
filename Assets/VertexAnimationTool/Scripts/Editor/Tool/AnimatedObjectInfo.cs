using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using System;

namespace VertexAnimation
{
    public class AnimatedObjectInfo
    {
        public class ClipInfo
        {
            public AnimationClip clip;
            public float clipTotalFrames => clip.length * clip.frameRate;
            public int totalFrameCount => Mathf.CeilToInt(clipTotalFrames);

            // For baking options
            public int frameCount;
            public float frameStep => clip.length / (frameCount - 1);

            // For texture layout
            public int startRow;    // -1 means not valid state
            public int pixelCount;  // internal use
            public bool isUnderTotalFrameCount => frameCount < totalFrameCount;
        }

        public class StateMotionInfo
        {
            public string StateName;
            public bool Bakeable;
            public ClipInfo[] ClipInfos;
            public bool IsBlendtree;
            public string NonBakeableReason;

            // UI
            public bool isSelected;
        }

        public GameObject TargetObject { get; private set; }
        public SkinnedMeshRenderer SkinnedMeshRenderer { get; private set; }
        public Mesh SharedMesh { get; private set; }
        public int VertexCount { get; private set; }
        public Animator Animator { get; private set; }
        public RuntimeAnimatorController AnimatorController { get; private set; }
        public string ObjectName { get; private set; }
        public List<Material> Materials { get; private set; }
        public Bounds ObjectBounds { get; private set; }
        public List<StateMotionInfo> StateInfos { get; private set; }

        private AnimatedObjectInfo() { }

        public static AnimatedObjectInfo FromGameObject(GameObject obj)
        {
            if (obj == null) return null;
            var info = new AnimatedObjectInfo();
            info.TargetObject = obj;
            info.ObjectName = obj.name;

            info.SkinnedMeshRenderer = obj.GetComponentInChildren<SkinnedMeshRenderer>();
            if (info.SkinnedMeshRenderer != null)
            {
                info.SharedMesh = info.SkinnedMeshRenderer.sharedMesh;
                info.VertexCount = info.SharedMesh != null ? info.SharedMesh.vertexCount : 0;
            }

            info.Animator = obj.GetComponentInChildren<Animator>();
            info.StateInfos = new List<StateMotionInfo>();
            if (info.Animator != null)
            {
                info.AnimatorController = info.Animator.runtimeAnimatorController;
                var ac = info.AnimatorController as AnimatorController;
                if (ac != null)
                {
                    foreach (var layer in ac.layers)
                    {
                        // Add default state first, So info[0] is default state
                        var defaultState = layer.stateMachine.defaultState;
                        if (defaultState != null)
                        {
                            info.StateInfos.Add(AddStateInfo(defaultState, obj));
                        }

                        foreach (var state in layer.stateMachine.states)
                        {
                            if (state.state != defaultState)
                            {
                                info.StateInfos.Add(AddStateInfo(state.state, obj));
                            }
                        }
                    }
                }
            }

            // Collect materials from SMR
            info.Materials = new List<Material>();
            if (info.SkinnedMeshRenderer.sharedMaterials != null)
                info.Materials.AddRange(info.SkinnedMeshRenderer.sharedMaterials);

            // Calculate Bounds
            if (info.SkinnedMeshRenderer != null)
            {
                info.ObjectBounds = info.SkinnedMeshRenderer.bounds;
            }
            else
            {
                info.ObjectBounds = new Bounds(obj.transform.position, Vector3.zero);
            }

            return info;
        }

        private static StateMotionInfo AddStateInfo(AnimatorState state, GameObject obj)
        {
            var stateInfo = new StateMotionInfo();
            stateInfo.StateName = state.name;
            stateInfo.IsBlendtree = false;
            
            var motion = state.motion;
            if (motion is BlendTree blendTree)
            {
                // 1D (2 motion) blend tree only
                if (blendTree.blendType == BlendTreeType.Simple1D &&
                    blendTree.children.Length == 2 &&
                    blendTree.children[0].motion is AnimationClip c0 &&
                    blendTree.children[1].motion is AnimationClip c1)
                {
                    stateInfo.Bakeable = true;

                    if (c0 != c1)
                    {
                        stateInfo.ClipInfos = new ClipInfo[] { new ClipInfo() { clip = c0 }, new ClipInfo() { clip = c1 } };
                    }
                    else
                    {
                        stateInfo.ClipInfos = new ClipInfo[] { new ClipInfo() { clip = c0 } };                        
                    }

                    stateInfo.IsBlendtree = true;
                }
                else
                {
                    // Possible single clip processing
                    stateInfo.ClipInfos = null;
                    foreach (var child in blendTree.children)
                    {
                        if (child.motion is AnimationClip clip)
                        {
                            stateInfo.ClipInfos = new ClipInfo[] { new ClipInfo() { clip = clip } };
                            break;
                        }
                    }

                    if (stateInfo.ClipInfos != null)
                    {
                        stateInfo.Bakeable = true;
                    }
                    else
                    {
                        stateInfo.Bakeable = false;
                        stateInfo.NonBakeableReason = "Can not find any clip";
                    }
                }
            }
            else if (motion is AnimationClip clip)
            {
                stateInfo.Bakeable = true;
                stateInfo.ClipInfos = new ClipInfo[] { new ClipInfo() { clip = clip } };
            }
            else
            {
                stateInfo.Bakeable = false;
                stateInfo.NonBakeableReason = "Not bakeable -No BlendTree or AnimationClip";

                Debug.Log($"No BlendTree or AnimationClip - {state.name} state- {obj.name}");
            }

            return stateInfo;
        }
    }
} 