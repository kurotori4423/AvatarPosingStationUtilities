
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace AvatarPosingStationUtilities
{
    /// <summary>
    /// SyncPoseStationの再生位置をスライダーで変更するスクリプト
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncPoseStationSlider : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(AnimTime))]
        float animTime = 0;
        public float AnimTime
        {
            get => animTime;
            set
            {
                animTime = value;
                if (!Networking.IsOwner(gameObject))
                {
                    m_slider.SetValueWithoutNotify(animTime);
                }
                m_syncPoseStation.SetTime(animTime);
            }
        }

        [SerializeField]
        private SyncAvatarPosingStation m_syncPoseStation;

        [SerializeField]
        Slider m_slider;
        [SerializeField]
        TMP_Text m_ownerNameText;

        public void OnChangeSlider()
        {
            if (Networking.IsOwner(gameObject))
            {
                AnimTime = m_slider.value;
                RequestSerialization();
            }
        }
        public void GetOwnerButton()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

            
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                var owner = Networking.GetOwner(gameObject);
                m_ownerNameText.text = $"[{owner.playerId}]{owner.displayName}";
                m_slider.interactable = Networking.IsOwner(gameObject);
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            m_ownerNameText.text = $"[{player.playerId}]{player.displayName}";
            m_slider.interactable = Networking.IsOwner(gameObject);
        }

    }
}