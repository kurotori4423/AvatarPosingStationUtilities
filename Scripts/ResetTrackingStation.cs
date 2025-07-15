
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace AvatarPosingStationUtilities
{
    /// <summary>
    /// アバターのトラッキング状態をリセットするためのStationスクリプト
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None), RequireComponent(typeof(VRCStation))]
    public class ResetTrackingStation : UdonSharpBehaviour
    {
        [SerializeField, Tooltip("アバターが席を離れた時のコールバック(_OnResetStationExited)")]
        public UdonBehaviour[] m_onStationExitedCallbacks;


        private VRCStation m_station;

        void Start()
        {
            m_station = GetComponent<VRCStation>();

            m_station.canUseStationFromStation = false;
            m_station.disableStationExit = true;
            m_station.stationExitPlayerLocation = transform;
            m_station.stationEnterPlayerLocation = transform;
        }

        public void AttachStation(VRCPlayerApi player)
        {
            if(Utilities.IsValid(player))
            {
                m_station.UseStation(player);
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if(player.isLocal)
            {
                Debug.Log($"ResetTrackingStation: OnStationEntered");

                SendCustomEventDelayedFrames(nameof(_ExitStation), 5);
            }
        }

        public void _ExitStation()
        {
            m_station.ExitStation(Networking.LocalPlayer);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            Debug.Log($"ResetTrackingStation: OnStationExited ([{player.playerId}]{player.displayName})");


            foreach (var callback in m_onStationExitedCallbacks)
            {
                if (Utilities.IsValid(callback))
                {
                    callback.SendCustomEvent("_OnResetStationExited");
                }
            }
        }
    }
}