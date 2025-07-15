using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace AvatarPosingStationUtilities
{
    /// <summary>
    /// アバターポーズ
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SequencePosingStation : IPosingStation
    {
        [SerializeField, Tooltip("ポーズリスト")]
        public AnimationClip[] PoseClips;

        [SerializeField, Tooltip("リセットステーション")]
        ResetTrackingStation resetTrackingStation;

        [SerializeField, Tooltip("座ったアバターのスケール調整を行うかどうか")]
        public bool IsAvatarScaleAdjust = false;

        [SerializeField, Tooltip("スケール調整時のサイズ")]
        public float m_defaultAvatarScale = 1.4f;

        [HideInInspector]
        public AvatarPosingStation[] avatarPosingStations;

        int nextStation = -1;

        /// <summary>
        /// デバッグテキストの更新
        /// </summary>
        protected override void UpdateDebugText()
        {
            if (Utilities.IsValid(m_debugText))
            {
                if (Utilities.IsValid(SeatedPlayer))
                {
                    var usedStation = CheckUsedStationPlayer();

                    var usedStationName = avatarPosingStations[usedStation].name;

                    m_debugText.text = $"{usedStationName}\nSeated: [{SeatedPlayer.playerId}]{SeatedPlayer.displayName}";
                }
                else
                {
                    m_debugText.text = $"Seated: None";
                }
            }
        }

        private void Start()
        {
            UpdateDebugText();
        }

        /// <summary>
        /// ポーズの数を取得する
        /// </summary>
        /// <returns></returns>
        public int GetPoseNum()
        {
            return avatarPosingStations.Length;
        }

        public void OnPoseStationEntered(int id, VRCPlayerApi player)
        {
            SeatedPlayer = player;

            // コールバック関数の呼び出し
            foreach (var callback in m_eventCallbacks)
            {
                if (Utilities.IsValid(callback))
                {
                    callback.SendCustomEvent(OnPosingStationEnteredCallbackName);
                }
            }
        }

        public void OnExitPoseStation(int id, VRCPlayerApi player)
        {
            if(player.isLocal)
            {
                if (nextStation > -1)
                {
                    SendCustomEventDelayedFrames(nameof(AttachStationDelay), 1); // 離席直後に実行すると即降りするので1フレーム待つ
                }
                else
                {
                    SendCustomEventDelayedFrames(nameof(AttachResetStationDelay), 1); // 離席直後に実行すると即降りするので1フレーム待つ
                }

            }
            SeatedPlayer = null;

            // コールバック関数の呼び出し
            foreach (var callback in m_eventCallbacks)
            {
                if (Utilities.IsValid(callback))
                {
                    callback.SendCustomEvent(OnPosingStationExitedCallbackName);
                }
            }
        }

        public void AttachStationDelay()
        {
            Debug.Log($"SequencePosingStation: OnExitPoseStation AttachStation[{nextStation}]({Networking.LocalPlayer})");
            avatarPosingStations[nextStation].AttachLocalPlayer();
            nextStation = -1;
        }

        public void AttachResetStationDelay()
        {
            Debug.Log($"SequencePosingStation: OnExitPoseStation AttachResetStation({Networking.LocalPlayer})");
            resetTrackingStation.AttachStation(Networking.LocalPlayer);
            nextStation = -1;
        }

        public void AttachStation(int id)
        {
            // 指定されたIDの範囲チェック
            if(id < 0 || id >= avatarPosingStations.Length)
            {
                Debug.LogError($"SequencePosingStation: AttachStation invalid station id [{id}]");
                return;
            }

            // ユーザーのStationの使用状態をチェックする
            var usedStation = CheckUsedStationPlayer();
            
            if(usedStation < 0)
            {
                // 現在利用者が居ない場合はそのままAttachする
                Debug.Log($"SequencePosingStation: AttachStation LocalPlayer");
                avatarPosingStations[id].AttachLocalPlayer();
            }
            else
            {
                var usedPlayer = avatarPosingStations[usedStation].SeatedPlayer;

                if(usedPlayer.isLocal)
                {
                    Debug.Log($"SequencePosingStation: AttachStation DetachPlayer NextStation is [{id}]");
                    nextStation = id;
                    avatarPosingStations[usedStation].DetachPlayer();
                }
                else
                {
                    return;
                }
            }

        }

        public override void DetachPlayer()
        {
            var usedStation = CheckUsedStationPlayer();

            if(usedStation >= 0)
            {
                var userPlayer = avatarPosingStations[usedStation].SeatedPlayer;

                if(userPlayer.isLocal)
                {
                    Debug.Log($"SequencePosingStation: DetachPlayer");

                    avatarPosingStations[usedStation].DetachPlayer();
                    nextStation = -1;
                }
            }
            
        }

        /// <summary>
        /// 利用中のステーションのIDを検索する
        /// </summary>
        /// <returns></returns>
        private int CheckUsedStationPlayer()
        {
            for(int i = 0; i < avatarPosingStations.Length; i++)
            {
                if(Utilities.IsValid(avatarPosingStations[i].SeatedPlayer))
                {
                    return i;
                }
            }

            return -1;
        }

    }
}