using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>Fire-and-forget positional one-shots (clunks, latches) with pitch, cleaned up when they finish.</summary>
    public static class Sfx
    {
        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float maxDistance = 12f)
        {
            if (clip == null)
            {
                return;
            }

            var src = new GameObject($"Sfx_{clip.name}").AddComponent<AudioSource>();
            src.transform.position = position;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = maxDistance;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
            Object.Destroy(src.gameObject, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }
    }
}
