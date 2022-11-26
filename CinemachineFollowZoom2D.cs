using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// An add-on module for Cinemachine Virtual Camera that adjusts
    /// the FOV of the lens to keep the target object at a constant size on the screen,
    /// regardless of camera and target position.
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CinemachineFollowZoom2D : CinemachineExtension
    {
        /// The shot width to maintain, in world units, at target distance.
        /// FOV will be adusted as far as possible to maintain this width at the
        /// target distance from the camera
        [Tooltip("The shot width to maintain, in world units, at target distance.")]
        public float m_Width = 2f;

        /// Increase this value to soften the aggressiveness of the follow-zoom.
        /// Small numbers are more responsive, larger numbers give a more heavy slowly responding camera.
        [Range(0, 20)]
        [Tooltip(
            "Increase this value to soften the aggressiveness of the follow-zoom.  Small numbers are more responsive, larger numbers give a more heavy slowly responding camera.")]
        public float m_Damping = 1f;

        /// Will not generate an FOV smaller than this.
        [Range(1, 179)]
        [Tooltip("Lower limit for the FOV that this behaviour will generate.")]
        public float m_MinOrthgraphicSize = 6f;

        /// Will not generate an FOV larget than this.
        [Range(1, 179)]
        [Tooltip("Upper limit for the FOV that this behaviour will generate.")]
        public float m_MaxOrthographicSize = 8f;

        void OnValidate()
        {
            m_Width = Mathf.Max(0, m_Width);
            m_MaxOrthographicSize = Mathf.Clamp(m_MaxOrthographicSize, 1, 179);
            m_MinOrthgraphicSize = Mathf.Clamp(m_MinOrthgraphicSize, 1, m_MaxOrthographicSize);
        }

        class VcamExtraState
        {
            public float m_previousFrameZoom = 0;
        }

        /// Report maximum damping time needed for this component.
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => m_Damping;

        /// Callback to preform the zoom adjustment
        override protected void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            VcamExtraState extra = GetExtraState<VcamExtraState>(vcam);
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid)
                extra.m_previousFrameZoom = state.Lens.FieldOfView;

            // Set the zoom after the body has been positioned, but before the aim,
            // so that composer can compose using the updated fov.
            if (stage == CinemachineCore.Stage.Body)
            {
                // Try to reproduce the target width
                float targetWidth = Mathf.Max(m_Width, 0);
                float fov = 179f;
                float d = Vector3.Distance(state.CorrectedPosition, state.ReferenceLookAt);
                if (d > UnityVectorExtensions.Epsilon)
                {
                    // Clamp targetWidth to FOV min/max
                    float minW = d * 2f * Mathf.Tan(m_MinOrthgraphicSize * Mathf.Deg2Rad / 2f);
                    float maxW = d * 2f * Mathf.Tan(m_MaxOrthographicSize * Mathf.Deg2Rad / 2f);
                    targetWidth = Mathf.Clamp(targetWidth, minW, maxW);

                    // Apply damping
                    if (deltaTime >= 0 && m_Damping > 0 && VirtualCamera.PreviousStateIsValid)
                    {
                        float currentWidth = d * 2f * Mathf.Tan(extra.m_previousFrameZoom * Mathf.Deg2Rad / 2f);
                        float delta = targetWidth - currentWidth;
                        delta = VirtualCamera.DetachedLookAtTargetDamp(delta, m_Damping, deltaTime);
                        targetWidth = currentWidth + delta;
                    }

                    fov = 2f * Mathf.Atan(targetWidth / (2 * d)) * Mathf.Rad2Deg;
                }

                LensSettings lens = state.Lens;
                lens.OrthographicSize = extra.m_previousFrameZoom =
                    Mathf.Clamp(fov, m_MinOrthgraphicSize, m_MaxOrthographicSize);

                state.Lens = lens;
            }
        }
    }
}
