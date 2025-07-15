
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace AvatarPosingStationUtilities
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SequencePosingStationSetButton : UdonSharpBehaviour
    {
        [SerializeField]
        public SequencePosingStation sequencePosingStation;
        [SerializeField]
        public int id;

        public void OnClick()
        {
            if(Utilities.IsValid(sequencePosingStation))
            {
                sequencePosingStation.AttachStation(id);
            }
        }
    }
}