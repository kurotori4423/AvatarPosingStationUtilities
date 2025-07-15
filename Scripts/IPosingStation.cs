
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace AvatarPosingStationUtilities
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class IPosingStation : UdonSharpBehaviour
    { 
        [SerializeField, Tooltip("デバッグテキスト")]
        protected TMP_Text m_debugText;

        [SerializeField, Tooltip("アバターがステーションに座ったり、外れたときのコールバック(_OnPosingStationEntered, _OnPosingStationExited)")]
        public UdonBehaviour[] m_eventCallbacks;

        [HideInInspector]
        public readonly string OnPosingStationEnteredCallbackName = "_OnPosingStationEntered";
        [HideInInspector]
        public readonly string OnPosingStationExitedCallbackName = "_OnPosingStationExited";

        [FieldChangeCallback(nameof(SeatedPlayer))]
        protected VRCPlayerApi m_seatedPlayer = null;
        public VRCPlayerApi SeatedPlayer
        {
            get { return m_seatedPlayer; }
            set
            {
                m_seatedPlayer = value;

                UpdateDebugText();
            }
        }

        virtual protected void UpdateDebugText()
        {
            if (Utilities.IsValid(m_debugText))
            {
                if (Utilities.IsValid(SeatedPlayer))
                {
                    m_debugText.text = $"Seated: [{SeatedPlayer.playerId}]{SeatedPlayer.displayName}";
                }
                else
                {
                    m_debugText.text = $"Seated: None";
                }
            }
        }

        virtual public void AttachLocalPlayer()
        {

        }

        virtual public void AttachPlayer(VRCPlayerApi player)
        {

        }

        virtual public void DetachPlayer()
        {
            
        }
    }
}