using System;
using UnityEngine;
using Cinemachine.Utility;
using UnityEditor;

namespace Cinemachine
{
    /// An add-on module for Cinemachine Virtual Camera that adjusts
    /// the OrthographicSize of the lens to keep the target object at a constant size on the screen,
    /// regardless of camera and target position.
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CinemachineFollowZoom2D : CinemachineExtension
    {
        [Tooltip("The shot height to maintain, in world units, at target distance.")]
        [SerializeField] AnimationCurve OrthographicSizeByDistance = AnimationCurve.Linear(0, -1, 2, 2);

        /// <summary>Increase this value to soften the aggressiveness of the follow-zoom.
        /// Small numbers are more responsive, larger numbers give a more heavy slowly responding camera. </summary>
        [Range(0f, 20f)]
        [Tooltip(
            "Increase this value to soften the aggressiveness of the follow-zoom.  Small numbers are more responsive, larger numbers give a more heavy slowly responding camera.")]
        [SerializeField] float m_Damping = 1;

        [SerializeField] bool m_Gizmo = true;

        [SerializeField] Gradient m_GizmoColor = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(1, 0) },
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0),
                new GradientColorKey(Color.black, 1),
            },
        };

        class VcamExtraState2D
        {
            public float m_previousFrameDistance = 0;
        }

        /// Report maximum damping time needed for this component.
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => m_Damping;

        override protected void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            VcamExtraState2D extra = GetExtraState<VcamExtraState2D>(vcam);
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid)
                extra.m_previousFrameDistance = state.Lens.FieldOfView;

            if (stage == CinemachineCore.Stage.Body)
            {
                LensSettings lens = state.Lens;
                // From Documentation:
                // The orthographicSize is half the size of the vertical viewing volume.
                // The horizontal size of the viewing volume depends on the aspect ratio.

                float distance = Vector2.Distance(state.CorrectedPosition, state.ReferenceLookAt);

                Debug.DrawLine(state.CorrectedPosition, state.ReferenceLookAt);

                if (distance > UnityVectorExtensions.Epsilon)
                {
                    distance = DampingIfNeed(deltaTime, distance, extra);
                }

                extra.m_previousFrameDistance = distance;
                lens.OrthographicSize += OrthographicSizeByDistance.Evaluate(distance);

                state.Lens = lens;
            }
        }

        float DampingIfNeed(float deltaTime, float distance, VcamExtraState2D extra)
        {
            if (m_Damping <= 0) return distance;
            if (!(deltaTime >= 0) || !VirtualCamera.PreviousStateIsValid) return distance;

            float delta = distance - extra.m_previousFrameDistance;
            delta = VirtualCamera.DetachedLookAtTargetDamp(delta, m_Damping, deltaTime);
            return extra.m_previousFrameDistance + delta;
        }

        void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            if (!m_Gizmo || OrthographicSizeByDistance.length == 0) return;

            float length = OrthographicSizeByDistance.keys.Length;
            for (int i = 0; i < length; i++)
            {
                Handles.matrix = Gizmos.matrix = transform.localToWorldMatrix;
                Handles.color = Gizmos.color = m_GizmoColor.Evaluate(i / length + 1);

                var key = OrthographicSizeByDistance.keys[i];
                Handles.DrawWireDisc(default, Vector3.forward, key.time);
                float size = VirtualCamera.State.Lens.OrthographicSize + key.value;
                Gizmos.DrawWireCube(default, Vector2.one * size);
            }
            #endif
        }
    }
}
