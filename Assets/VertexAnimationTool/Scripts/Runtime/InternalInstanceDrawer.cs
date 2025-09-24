using UnityEngine;
using System.Collections.Generic;

namespace VertexAnimation
{
    public class InternalInstanceDrawer
    {
        public VAData vaData { get { return _vaData; } }
        private VAData _vaData;
        private bool _isMultipleVA;
        private Mesh _mesh;
        private RenderParams _renderParams;
        private MaterialPropertyBlock _propertyBlock;

        private int _maxInstanceCount;
        private int _instanceCount;
        private int _drawerIndex;

        public int instanceCount {
            get { return _instanceCount; }
        }

        public int maxInstanceCount {
            get { return _maxInstanceCount; }
        }

        private struct InstanceData
        {
            public Matrix4x4 objectToWorld;
        }

        private InstanceData[] _instanceData;

        private float[] _beginArray;
        private float[] _offsetArray;
        private float[] _speedArray;
        private float[] _vaIndexArray;
        private bool _isDirty_Begin;
        private bool _isDirty_Offset;
        private bool _isDirty_Speed;
        private bool _isDirty_VAIndex;

        private VertexAnimationInstance[] _vaArray;
        private HashSet<VertexAnimationInstance> _vaSet; // for check distinct

        // Shader properties
        public static int s_VaPositionTexId = Shader.PropertyToID("_VaPositionTex");
        public static int s_VaNormalTexId = Shader.PropertyToID("_VaNormalTex");
        public static int s_VaTextureWidthId = Shader.PropertyToID("_VaTextureWidth");
        public static int s_VaTextureHeightId = Shader.PropertyToID("_VaTextureHeight");
        public static int s_VaVertexCountId = Shader.PropertyToID("_VaVertexCount");
        public static int s_VaBoundMinId = Shader.PropertyToID("_VaBoundMin");
        public static int s_VaBoundMaxId = Shader.PropertyToID("_VaBoundMax");

        public static int s_VaStartRowId = Shader.PropertyToID("_VaStartRow");
        public static int s_VaClipLengthId = Shader.PropertyToID("_VaClipLength");
        public static int s_VaFrameCountId = Shader.PropertyToID("_VaFrameCount");
        public static int s_VaIsLoopId = Shader.PropertyToID("_VaIsLoop");
        
        public static int s_beginArrayID = Shader.PropertyToID("_BeginArray");
        public static int s_offsetArrayID = Shader.PropertyToID("_OffsetArray");
        public static int s_speedArrayID = Shader.PropertyToID("_SpeedArray");
        public static int s_vaIndexArrayID = Shader.PropertyToID("_VaIndexArray");


        public InternalInstanceDrawer(VAData vaData, int maxInstanceCount, int drawerIndex)
        {
            _vaData = vaData;
            _mesh = _vaData.mesh;
            _isMultipleVA = _vaData.vertexAnimations.Length > 1;

            SetMaterial();
            _renderParams = new RenderParams(_vaData.material);
            _propertyBlock = new MaterialPropertyBlock();
            
            _drawerIndex = drawerIndex;
            _maxInstanceCount = maxInstanceCount;
            _instanceCount = 0;

            _instanceData = new InstanceData[maxInstanceCount];
            _beginArray = new float[maxInstanceCount];
            _offsetArray = new float[maxInstanceCount];
            _speedArray = new float[maxInstanceCount];
            _vaIndexArray = _isMultipleVA ? new float[maxInstanceCount] : null;

            for (int i = 0; i < maxInstanceCount; i++)
            {
                _offsetArray[i] = 0.0f;
                _speedArray[i] = 1.0f;
                if (_isMultipleVA)
                {
                    _vaIndexArray[i] = 0f;
                }
            }
            _isDirty_Offset = true;
            _isDirty_Speed = true;

            _vaArray = new VertexAnimationInstance[maxInstanceCount];
            _vaSet = new HashSet<VertexAnimationInstance>();
        }

        ~InternalInstanceDrawer()
        {
            _vaData = null;
            _mesh = null;
            _propertyBlock = null;

            _instanceData = null;
            _beginArray = null;
            _offsetArray = null;
            _speedArray = null;
            _vaIndexArray = null;

            _vaArray = null;
            _vaSet = null;
        }

        public void SetMaterial()
        {
            if (_vaData == null || _vaData.material == null || _vaData.vertexAnimations == null || _vaData.vertexAnimations.Length == 0)
                return;

            var mat = _vaData.material;
            mat.SetTexture(s_VaPositionTexId, _vaData.positionTexture);
            mat.SetTexture(s_VaNormalTexId, _vaData.normalTexture);
            mat.SetFloat(s_VaTextureWidthId, _vaData.textureSize.x);
            mat.SetFloat(s_VaTextureHeightId, _vaData.textureSize.y);
            mat.SetFloat(s_VaVertexCountId, _vaData.vertexCount);
            mat.SetVector(s_VaBoundMinId, _vaData.boundsMin);
            mat.SetVector(s_VaBoundMaxId, _vaData.boundsMax);

            if (_isMultipleVA && _vaData.availableVACount > 1)
            {
                var count = _vaData.availableVACount;
                var array = new float[count];
                
                for (int i = 0; i < count; i++)
                {
                    array[i] = _vaData.vertexAnimations[i].startRow;
                }
                mat.SetFloatArray(s_VaStartRowId, array);
                
                for (int i = 0; i < count; i++)
                {
                    array[i] = _vaData.vertexAnimations[i].length;
                }
                mat.SetFloatArray(s_VaClipLengthId, array);
                
                for (int i = 0; i < count; i++)
                {
                    array[i] = _vaData.vertexAnimations[i].bakeFrameCount;
                }
                mat.SetFloatArray(s_VaFrameCountId, array);

                for (int i = 0; i < count; i++)
                {
                    array[i] = _vaData.vertexAnimations[i].isLoop ? 1.0f : 0.0f;
                }
                mat.SetFloatArray(s_VaIsLoopId, array);
            }
            else
            {
                var va = _vaData.vertexAnimations[0];
                mat.SetFloat(s_VaStartRowId, va.startRow);
                mat.SetFloat(s_VaClipLengthId, va.length);
                mat.SetFloat(s_VaFrameCountId, va.bakeFrameCount);
                mat.SetFloat(s_VaIsLoopId, va.isLoop ? 1.0f : 0.0f);
            }
        }

        public void SetSpeed(VertexAnimationInstance va, float speed)
        {
            _speedArray[va.index] = speed;
            _isDirty_Speed = true;
        }

        public void SetBegin(VertexAnimationInstance va, float begin)
        {
            _beginArray[va.index] = begin;
            _isDirty_Begin = true;
        }

        public void SetOffset(VertexAnimationInstance va, float offset)
        {
            _offsetArray[va.index] = offset;
            _isDirty_Offset = true;
        }

        public void SetVAIndex(VertexAnimationInstance va, int vaIndex)
        {
            if (!_isMultipleVA)
                return;

            vaIndex = Mathf.Clamp(vaIndex, 0, _vaData.availableVACount - 1);
            _vaIndexArray[va.index] = vaIndex;
            _isDirty_VAIndex = true;
        }

        public void Update()
        {
            if (_instanceCount == 0)
                return;

            // Update instance data
            for (int i = 0; i < _instanceCount; i++)
            {
                _instanceData[i].objectToWorld = _vaArray[i].GetMatrix();
            }

            // Update render params
            if (_isDirty_Offset || _isDirty_Speed || _isDirty_VAIndex || _isDirty_Begin)
            {
                if (_isDirty_Begin)
                {
                    _propertyBlock.SetFloatArray(s_beginArrayID, _beginArray);
                    _isDirty_Begin = false;
                }

                if (_isDirty_Offset)
                {
                    _propertyBlock.SetFloatArray(s_offsetArrayID, _offsetArray);
                    _isDirty_Offset = false;
                }

                if (_isDirty_Speed)
                {
                    _propertyBlock.SetFloatArray(s_speedArrayID, _speedArray);
                    _isDirty_Speed = false;
                }

                if (_isDirty_VAIndex)
                {
                    _propertyBlock.SetFloatArray(s_vaIndexArrayID, _vaIndexArray);
                    _isDirty_VAIndex = false;
                }

                _renderParams.matProps = _propertyBlock;
            }
        }

        public void Draw()
        {
            if (_instanceCount <= 0)
                return;

            Graphics.RenderMeshInstanced(_renderParams, _mesh, 0, _instanceData, _instanceCount, 0);
        }

        #region VertexAnimationInstance management

        public void Add(VertexAnimationInstance va)
        {
            if (_vaSet.Contains(va) || _instanceCount >= _maxInstanceCount)
                return;

            _vaSet.Add(va);

            SetVA(_instanceCount, va);
            _instanceCount++;
        }

        public void Remove(VertexAnimationInstance va)
        {
            if (!_vaSet.Contains(va))
                return;

            // Move last element to the removed index
            if (_instanceCount - 1 != va.index && _instanceCount > 0)
            {
                SetVA(va.index, _vaArray[_instanceCount - 1]);
            }
            
            va.SetInternalIndex(-1, -1);
            _instanceCount--;
            _vaSet.Remove(va);
        }

        private void SetVA(int index, VertexAnimationInstance va)
        {
            _vaArray[index] = va;
            va.SetInternalIndex(index, _drawerIndex);
            SetSpeed(va, va.speed);
            SetOffset(va, va.offset);
            SetBegin(va, va.begin);
        }

        #endregion //VertexAnimationInstance management
    }
}
