
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace AvatarPosingStationUtilities
{
    /// <summary>
    /// AvatarPosingStationの再生位置を同期するスクリプト
    /// </summary>

    public class SyncPoseStaitonTimeSync : UdonSharpBehaviour
    {
        [SerializeField]
        private SyncAvatarPosingStation syncAvatarPosingStation;

        private void Update()
        {
            var serverTime = Networking.GetServerTimeInSeconds();

            var normalizedTime = serverTime / syncAvatarPosingStation.PoseClip.length;

            normalizedTime = normalizedTime - Mathf.FloorToInt((float)normalizedTime);

            syncAvatarPosingStation.SetTime((float)normalizedTime);
        }


    }
}
