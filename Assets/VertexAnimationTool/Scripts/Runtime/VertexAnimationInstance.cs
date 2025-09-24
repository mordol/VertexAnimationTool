using UnityEngine;

namespace VertexAnimation
{
    public class VertexAnimationInstance : MonoBehaviour
    {
        public VAData m_VAData;

        public int m_VAIndex = 0;
		private VAData.VertexAnimation currentVA => (m_VAIndex >= 0 ? m_VAData.vertexAnimations[m_VAIndex] : null);
        private float m_Speed = 1.0f;
        private float m_Begin = 0.0f;
        private float m_Offset = 0.0f;
        public float begin => m_Begin;
        public float offset => m_Offset;

        private bool m_RandomizeOffset = true;

        private Transform m_Transform;
        public Transform cachedTransform => m_Transform;
        private bool m_IsValidVAData = false;
        // for internal instance drawer
        private int m_InternalIndex = -1;
        private int m_InternalDrawerIndex = -1;
        public int index { get { return m_InternalIndex; } }
        public int drawerIndex { get { return m_InternalDrawerIndex; } }
        public bool isRegistered { get { return m_InternalIndex >= 0; } }
        private InternalInstanceDrawer drawer => VertexAnimationInstanceDrawer.GetInstanceDrawer(m_InternalDrawerIndex);

        void OnEnable()
        {
            VertexAnimationInstanceDrawer.Register(this);
            if (isRegistered)
            {
                ResetAnimation();
            }
        }

        void OnDisable()
        {
            if (isRegistered)
            {
                VertexAnimationInstanceDrawer.Unregister(this);
            }
        }

        void Awake()
        {
            m_Transform = transform;
        }

        void Start()
        {
            if (!isRegistered)  
            {
                VertexAnimationInstanceDrawer.Register(this);
                //ResetAnimation();
            }
            m_IsValidVAData = m_VAData != null;

            // TEMP: random play
            m_VAIndex = UnityEngine.Random.Range(0, m_VAData.availableVACount);
            drawer?.SetVAIndex(this, m_VAIndex);
            var randomize = m_VAIndex == 0 && m_RandomizeOffset;
            ResetAnimation(randomize);
        }

        private void SetSpeed(float speed, bool force = false)
        {
            if (Mathf.Approximately(m_Speed, speed) && !force)
                return;

            var progress = GetNormalizedTime();

            m_Speed = speed;
            drawer?.SetSpeed(this, m_Speed);

            var current = GetNormalizedTime();
            m_Offset -= (current - progress) * currentVA.length;
            drawer?.SetOffset(this, m_Offset);
        }

        private void ResetAnimation(bool randomize = false)
        {
            m_Begin = Time.time;
            m_Offset = randomize ? Random.Range(0.0f, m_VAData.vertexAnimations[m_VAIndex].length) : 0.0f;

            drawer?.SetBegin(this, m_Begin);
            drawer?.SetOffset(this, m_Offset);
        }

        public Matrix4x4 GetMatrix()
        {
            return m_Transform.localToWorldMatrix;
        }

        public void SetInternalIndex(int index, int drawerIndex)
        {
            m_InternalIndex = index;
            m_InternalDrawerIndex = drawerIndex;
        }

        private float GetNormalizedTime()
        {
            var elapsedTime = m_Offset + (Time.time - m_Begin) * m_Speed;
            var progress = currentVA.isLoop ? 
                elapsedTime % currentVA.length : 
                Mathf.Clamp(elapsedTime, 0f, currentVA.length);

            return progress / currentVA.length;
        }

        public float speed { 
            get { return m_Speed; } 
            set { SetSpeed(value); } }

        public void Play(string name)
        {
            Play(Animator.StringToHash(name));
        }

        public void Play(int stateNameHash)
        {
            var vaIndex = m_VAData.GetVertexAnimationIndex(stateNameHash);
            if (vaIndex == m_VAIndex || vaIndex == -1)
                return;

            m_VAIndex = vaIndex;
            drawer?.SetVAIndex(this, m_VAIndex);
            var randomize = m_VAIndex == 0 && m_RandomizeOffset;
            ResetAnimation(randomize);
        }

        public void Reset()
        {
            ResetAnimation();
        }
    }
}