using System;
using UnityEngine;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Movement tuning values. Defaults are the GDD §3.2 starting values — test and adjust, they are not final.
    /// </summary>
    [Serializable]
    public sealed class PlayerTuning
    {
        [Header("Speeds (m/s)")]
        public float WalkSpeed = 4.2f;
        public float SprintSpeed = 6.2f;
        public float CrouchSpeed = 2.2f;
        public float CrawlSpeed = 1.6f;

        [Header("Jump & gravity")]
        public float JumpHeight = 1.0f;
        public float Gravity = -20f;

        [Header("Acceleration")]
        [Tooltip("Ground acceleration in m/s². Higher = snappier.")]
        public float Acceleration = 40f;
        [Range(0f, 1f)] public float AirControl = 0.3f;

        [Header("Camera heights (m)")]
        public float NormalCameraHeight = 1.65f;
        public float CrouchCameraHeight = 1.05f;
        public float CrawlCameraHeight = 0.55f;

        [Header("Capsule heights (m)")]
        public float NormalCapsuleHeight = 1.8f;
        public float CrouchCapsuleHeight = 1.2f;
        public float CrawlCapsuleHeight = 0.7f;
        [Tooltip("How fast the capsule and camera move between stances (m/s).")]
        public float StanceTransitionSpeed = 5f;

        [Header("Interaction")]
        public float InteractionDistance = 2.5f;

        /// <summary>Initial upward velocity needed to reach <see cref="JumpHeight"/> under <see cref="Gravity"/>.</summary>
        public float JumpVelocity => Mathf.Sqrt(2f * JumpHeight * Mathf.Abs(Gravity));

        public float SpeedFor(PlayerMovementState state) => state switch
        {
            PlayerMovementState.Sprinting => SprintSpeed,
            PlayerMovementState.Crouching => CrouchSpeed,
            PlayerMovementState.Crawling => CrawlSpeed,
            PlayerMovementState.Interacting => 0f,
            _ => WalkSpeed
        };

        /// <summary>Camera height for a posture. Sprinting and Interacting use the standing height.</summary>
        public float CameraHeightFor(PlayerMovementState posture) => posture switch
        {
            PlayerMovementState.Crouching => CrouchCameraHeight,
            PlayerMovementState.Crawling => CrawlCameraHeight,
            _ => NormalCameraHeight
        };

        /// <summary>Capsule height for a posture. Sprinting and Interacting use the standing height.</summary>
        public float CapsuleHeightFor(PlayerMovementState posture) => posture switch
        {
            PlayerMovementState.Crouching => CrouchCapsuleHeight,
            PlayerMovementState.Crawling => CrawlCapsuleHeight,
            _ => NormalCapsuleHeight
        };
    }
}
