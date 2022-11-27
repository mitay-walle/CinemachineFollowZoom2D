using System;
using UnityEngine;
using UnityEditor;

namespace Cinemachine
{
    public enum Zoom2DPattern
    {
        CameraTargetDistance,
        CameraTargetVelocity,
        TargetVelocity,
    }

    /// An add-on module for Cinemachine Virtual Camera that adjusts
    /// the OrthographicSize of the lens to keep the target object at a constant size on the screen,
    /// regardless of camera and target position.
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CinemachineFollowZoom2D : CinemachineExtension
    {
        [SerializeField] Zoom2DPattern m_Pattern;

        [Tooltip("The shot height to maintain, in world units, at target distance.")]
        [SerializeField] AnimationCurve m_OrthographicSizeByDistance = AnimationCurve.Linear(0, -1, 2, 2);

        /// <summary>Increase this value to soften the aggressiveness of the follow-zoom.
        /// Small numbers are more responsive, larger numbers give a more heavy slowly responding camera. </summary>
        [Range(0f, 20f)]
        [Tooltip(
            "Increase this value to soften the aggressiveness of the follow-zoom.  Small numbers are more responsive, larger numbers give a more heavy slowly responding camera.")]
        [SerializeField] float m_Damping = 1;

        [SerializeField] bool m_Gizmo = true;
        float m_Distance = 1;
        float m_Velocity = 1;

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
            public float m_previousFrameResult = 0;
            public float m_previousFrameDistance = 0;
            public Vector2 m_previousFramePosition;
        }

        /// Report maximum damping time needed for this component.
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => m_Damping;

        override protected void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            VcamExtraState2D extra = GetExtraState<VcamExtraState2D>(vcam);

            if (stage == CinemachineCore.Stage.Body)
            {
                LensSettings lens = state.Lens;
                // From Documentation:
                // The orthographicSize is half the size of the vertical viewing volume.
                // The horizontal size of the viewing volume depends on the aspect ratio.

                float result;

                switch (m_Pattern)
                {
                    case Zoom2DPattern.CameraTargetDistance:
                    {
                        result = m_Distance = Vector2.Distance(state.CorrectedPosition, state.ReferenceLookAt);
                        extra.m_previousFrameDistance = result;
                        break;
                    }

                    case Zoom2DPattern.CameraTargetVelocity:
                    {
                        float newDistance = Vector2.Distance(state.CorrectedPosition, state.ReferenceLookAt);
                        result = m_Velocity = Mathf.Abs(newDistance - extra.m_previousFrameDistance);
                        extra.m_previousFrameDistance = newDistance;
                        break;
                    }

                    case Zoom2DPattern.TargetVelocity:
                    {
                        if (VirtualCamera.Follow)
                        {
                            var rb = VirtualCamera.Follow.GetComponent<Rigidbody2D>();
                            if (rb)
                            {
                                result = m_Velocity = rb.velocity.magnitude;
                            }
                            else
                            {
                                float newDistance =
                                    Vector2.Distance(state.ReferenceLookAt, extra.m_previousFramePosition);

                                result = m_Velocity = Mathf.Abs(newDistance - extra.m_previousFrameDistance);
                                extra.m_previousFrameDistance = newDistance;
                                extra.m_previousFramePosition = state.ReferenceLookAt;
                            }
                        }
                        else
                        {
                            result = default;
                        }

                        break;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }

                //if (result > UnityVectorExtensions.Epsilon)
                {
                    result = ApplyDampingIfNeed(deltaTime, result, extra);
                }

                extra.m_previousFrameResult = result;

                lens.OrthographicSize += m_OrthographicSizeByDistance.Evaluate(result);

                state.Lens = lens;
            }
        }

        float ApplyDampingIfNeed(float deltaTime, float distance, VcamExtraState2D extra)
        {
            if (m_Damping <= 0) return distance;
            // if (!(deltaTime >= 0)
            //     //|| !VirtualCamera.PreviousStateIsValid
            //     ) return distance;

            float delta = distance - extra.m_previousFrameResult;
            delta = VirtualCamera.DetachedFollowTargetDamp(delta, m_Damping, deltaTime);
            return extra.m_previousFrameResult + delta;
        }

        void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            if (!m_Gizmo || m_OrthographicSizeByDistance.length == 0) return;

            float length = m_OrthographicSizeByDistance.keys.Length;
            for (int i = 0; i < length; i++)
            {
                Handles.matrix = Gizmos.matrix = transform.localToWorldMatrix;
                Handles.color = Gizmos.color = m_GizmoColor.Evaluate(i / length + 1);

                var key = m_OrthographicSizeByDistance.keys[i];
                Handles.DrawWireDisc(default, Vector3.forward, key.time);
                float size = VirtualCamera.State.Lens.OrthographicSize + key.value;
                Gizmos.DrawWireCube(default, Vector2.one * size);
            }
            #endif
        }
    }
}
