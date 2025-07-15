using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace AvatarPosingStationUtilities
{
    /// <summary>
    /// プレイヤーを指定したアニメーターでポージングさせるStationスクリプト
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None), RequireComponent(typeof(VRCStation))]
    public class AvatarPosingStation : IPosingStation
    {
        public string animatorControllerPath = "Assets/AvatarPosingStationUtilities/Animators/PoseChangeController.controller";

        [SerializeField, Tooltip("ポーズアニメーション")]
        public AnimationClip PoseClip;

        [SerializeField, Tooltip("フェイシャルモーション")]
        public AnimationClip FacialClip;

        [Space]

        [SerializeField, Tooltip("アバターのトラッキング情報をリセットするためのStation")]
        public ResetTrackingStation m_resetTrackingStation;

        [SerializeField, Tooltip("座ったアバターのスケール調整を行うかどうか")]
        public bool IsAvatarScaleAdjust = false;

        [SerializeField, Tooltip("スケール調整時のアバターサイズ（目の高さ準拠）")]
        public float AdjustAvatarScale = 1.4f;
        
        [SerializeField, Tooltip("スケール調整時のサイズ")]
        public float m_defaultAvatarScale = 1.4f;

        [Space]

        [SerializeField, Tooltip("VRCStation")]
        public VRCStation m_station;


        /// <summary>
        /// ポーズステーションの管理システム
        /// </summary>
        [HideInInspector]
        public SequencePosingStation m_sequencePosingStation;
        [HideInInspector]
        public int StationID = -1;

        void Start()
        {
            UpdateDebugText();
        }

        /// <summary>
        /// この関数を実行したプレイヤーをステーションにセットする
        /// </summary>
        public override void AttachLocalPlayer()
        {
            m_station.UseStation(Networking.LocalPlayer);
        }

        /// <summary>
        /// プレイヤーをステーションにセットする。
        /// </summary>
        /// <param name="player"></param>
        public override void AttachPlayer(VRCPlayerApi player)
        {
            m_station.UseStation(player);
        }

        /// <summary>
        /// プレイヤーをステーションから降ろす。(現在座っているプレイヤーが実行したときのみ効果があります)
        /// </summary>
        public override void DetachPlayer()
        {
            if(Utilities.IsValid(SeatedPlayer))
            {
                if(SeatedPlayer.isLocal)
                {
                    Debug.Log($"AvatarPosingStation: ({gameObject.name})[{Networking.LocalPlayer.playerId}]DetachPlayer:[{SeatedPlayer.playerId}]");
                    m_station.ExitStation(SeatedPlayer);
                }
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(SeatedPlayer))
            {
                Debug.Log($"AvatarPosingStation: ({gameObject.name})[{Networking.LocalPlayer.playerId}]OnStationEntered:[{player.playerId}]");

                SeatedPlayer = player;

                // アバターのスケール調整
                if(IsAvatarScaleAdjust && player.isLocal)
                {
                    m_defaultAvatarScale = player.GetAvatarEyeHeightAsMeters();
                    player.SetAvatarEyeHeightByMeters(AdjustAvatarScale);
                }

                // コールバック関数の呼び出し
                foreach (var callback in m_eventCallbacks)
                {
                    if (Utilities.IsValid(callback))
                    {
                        callback.SendCustomEvent(OnPosingStationEnteredCallbackName);
                    }
                }

                if(Utilities.IsValid(m_sequencePosingStation))
                {
                    m_sequencePosingStation.OnPoseStationEntered(StationID, player);
                }
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (player.Equals(SeatedPlayer))
            {

                // リセットステーションに座らせる
                if (Utilities.IsValid(m_resetTrackingStation))
                {
                    Debug.Log($"AvatarPosingStation: ({gameObject.name})[{Networking.LocalPlayer.playerId}]OnStationExited:[{player.playerId}] to ResetStation");

                    if (SeatedPlayer.isLocal)
                    {
                        SendCustomEventDelayedFrames(nameof(DelayedAttachResetTrackingStation), 5);
                    }

                    // コールバック関数の呼び出し
                    foreach (var callback in m_eventCallbacks)
                    {
                        if (Utilities.IsValid(callback))
                        {
                            callback.SendCustomEvent(OnPosingStationExitedCallbackName);
                        }
                    }
                }
                else
                {
                    // リセットステーションが設定されていない場合かつ、連続ポーズステーションが指定されている場合
                    if(Utilities.IsValid(m_sequencePosingStation))
                    {
                        Debug.Log($"AvatarPosingStation: ({gameObject.name})[{Networking.LocalPlayer.playerId}]OnStationExited:[{player.playerId}] to NextSequenceStation");

                        // SequencePosingStationのコールバックを呼び出す。
                        m_sequencePosingStation.OnExitPoseStation(StationID, SeatedPlayer);
                    }
                }

                SeatedPlayer = null;

                // アバターのスケール調整
                if (IsAvatarScaleAdjust && player.isLocal)
                {
                    player.SetAvatarEyeHeightByMeters(m_defaultAvatarScale);
                }


            }
        }

        public void DelayedAttachResetTrackingStation()
        {
            m_resetTrackingStation.AttachStation(Networking.LocalPlayer);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // PlayerLeft時はOnStationExitedが走らないので自分で呼び出す。
            if(player.Equals(SeatedPlayer))
            {
                OnStationExited(player);
            }
        }
    }
}