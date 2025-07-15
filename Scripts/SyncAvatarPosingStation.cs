
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace AvatarPosingStationUtilities
{
    /// <summary>
    /// AvatarPosingStationの再生位置を同期するスクリプト
    /// </summary>
    public class SyncAvatarPosingStation : IPosingStation
    {
        public string animatorControllerPath = "Assets/AvatarPosingStationUtilities/Animators/SyncPoseChangeController.controller";

        [SerializeField, Tooltip("ポーズアニメーション")]
        public AnimationClip PoseClip;
        [SerializeField, Tooltip("フェイシャルモーション")]
        public AnimationClip FacialClip;

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

        [SerializeField, Tooltip("DebugText")]
        TMP_Text m_velocityText;

        [Header("検証用")]
        [SerializeField] private bool velocityUpdateInUpdate = true;
        [SerializeField] private bool velocityUpdateInLateUpdate = true;
        [SerializeField] private bool velocityUpdateInPostLateUpdate = true;


        /// <summary>
        /// ポーズステーションの管理システム
        /// </summary>
        [HideInInspector]
        public SequencePosingStation m_sequencePosingStation;
        [HideInInspector]
        public int StationID = -1;

        Vector3 m_playerVelocity = Vector3.zero;

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
            if (Utilities.IsValid(SeatedPlayer))
            {
                if (SeatedPlayer.isLocal)
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
                if (IsAvatarScaleAdjust && player.isLocal)
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

                if (Utilities.IsValid(m_sequencePosingStation))
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
                        SendCustomEventDelayedFrames(nameof(DelayedAttachResetTrackingStation), 1);
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
                    if (Utilities.IsValid(m_sequencePosingStation))
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
            if (player.Equals(SeatedPlayer))
            {
                OnStationExited(player);
            }
        }

        public void SetTime(float normalizeTime)
        {
            var velocity = transform.TransformVector(new Vector3(normalizeTime, 0, 0));
            m_playerVelocity = velocity;
        }

        /// <summary>
        /// プレイヤーの速度を更新する
        /// </summary>
        private void UpdataPlayerVelocity()
        {
            if (Utilities.IsValid(SeatedPlayer))
            {
                SeatedPlayer.SetVelocity(m_playerVelocity);
                
                if(m_velocityText)
                    m_velocityText.text = SeatedPlayer.GetVelocity().x.ToString();
            }
            else
            {
                if(m_velocityText)
                    m_velocityText.text ="";
            }
        }

        private void Update()
        {
            if (velocityUpdateInUpdate)
            {
                UpdataPlayerVelocity();
            }
        }

        private void LateUpdate()
        {
            // もしかしたら更新タイミングによっては速度が書き変わる可能性があるため、LateUpdateでも更新する
            if (velocityUpdateInLateUpdate)
            {
                UpdataPlayerVelocity();
            }
        }

        public override void PostLateUpdate()
        {
            // もしかしたら更新タイミングによっては速度が書き変わる可能性があるため、PostLateUpdateでも更新する
            if (velocityUpdateInPostLateUpdate)
            {
                UpdataPlayerVelocity();
            }
        }
    }
}