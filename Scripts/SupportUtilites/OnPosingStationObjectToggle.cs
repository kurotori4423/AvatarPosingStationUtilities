
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace AvatarPosingStationUtilities
{
    /// <summary>
    /// AvatarPosingStationのコールバックを受け取りオブジェクトのオンオフを行うスクリプト
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OnPosingStationObjectToggle : UdonSharpBehaviour
    {
        [SerializeField]
        private IPosingStation m_avatarPosingStation;

        [SerializeField, Tooltip("プレイヤーが着席中に表示するオブジェクト")]
        public GameObject[] m_OnSeatedDisplayObjects;
        [SerializeField, Tooltip("プレイヤーが着席中に非表示にするオブジェクト")]
        public GameObject[] m_OnSeatedHideObjects;
        [SerializeField, Tooltip("座っている人のみに表示するオブジェクト")]
        public GameObject[] m_OnLocalSeatedDisplayObjects;
        [SerializeField, Tooltip("プレイヤーが着席中に座っているプレイヤー以外に非表示にするオブジェクト")]
        public GameObject[] m_OnOtherSeatedHideObjects;

        void Start()
        {
            foreach(var obj in m_OnSeatedDisplayObjects)
            {
                if(Utilities.IsValid(obj))
                {
                    obj.SetActive(false);
                }
            }

            foreach (var obj in m_OnSeatedHideObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(true);
                }
            }

            foreach (var obj in m_OnLocalSeatedDisplayObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(false);
                }
            }

            foreach(var obj in m_OnOtherSeatedHideObjects)
            {
                if(Utilities.IsValid(obj))
                {
                    obj.SetActive(true);
                }
            }
        }

        public void _OnPosingStationEntered()
        {
            foreach (var obj in m_OnSeatedDisplayObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(true);
                }
            }

            foreach (var obj in m_OnSeatedHideObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(false);
                }
            }

            if (Utilities.IsValid(m_avatarPosingStation))
            {
                if (m_avatarPosingStation.SeatedPlayer.Equals(Networking.LocalPlayer))
                {

                    foreach (var obj in m_OnLocalSeatedDisplayObjects)
                    {
                        if (Utilities.IsValid(obj))
                        {
                            obj.SetActive(true);
                        }
                    }

                    foreach (var obj in m_OnOtherSeatedHideObjects)
                    {
                        if (Utilities.IsValid(obj))
                        {
                            obj.SetActive(true);
                        }
                    }
                }
                else
                {
                    foreach (var obj in m_OnLocalSeatedDisplayObjects)
                    {
                        if (Utilities.IsValid(obj))
                        {
                            obj.SetActive(false);
                        }
                    }

                    foreach (var obj in m_OnOtherSeatedHideObjects)
                    {
                        if (Utilities.IsValid(obj))
                        {
                            obj.SetActive(false);
                        }
                    }
                }
            }
            else
            {
                foreach (var obj in m_OnLocalSeatedDisplayObjects)
                {
                    if (Utilities.IsValid(obj))
                    {
                        obj.SetActive(false);
                    }
                }

                foreach (var obj in m_OnOtherSeatedHideObjects)
                {
                    if (Utilities.IsValid(obj))
                    {
                        obj.SetActive(true);
                    }
                }
            }

        }

        public void _OnPosingStationExited()
        {
            foreach (var obj in m_OnSeatedDisplayObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(false);
                }
            }

            foreach (var obj in m_OnSeatedHideObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(true);
                }
            }

            foreach (var obj in m_OnLocalSeatedDisplayObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(false);
                }
            }

            foreach (var obj in m_OnOtherSeatedHideObjects)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.SetActive(true);
                }
            }
        }
    }
}