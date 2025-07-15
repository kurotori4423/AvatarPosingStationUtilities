using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AvatarPosingStationUtilities
{

    public class PosingStationSetupper : IProcessSceneWithReport
    {
        // UdonBehaviorの生成が登録に間に合わないときがたまにあるので優先度を上げる。
        public int callbackOrder => -1;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var sceneRootObjects = scene.GetRootGameObjects();

            List<SequencePosingStation> sequencePosingStations = new List<SequencePosingStation>();
            List<ResetTrackingStation> resetTrackingStations = new List<ResetTrackingStation>();
            List<AvatarPosingStation> posingStations = new List<AvatarPosingStation>();
            List<SyncAvatarPosingStation> syncAvatarPosingStations = new List<SyncAvatarPosingStation>();

            // ステーションコンポーネント収集する
            foreach (var sceneRootObject in sceneRootObjects)
            {
                var sequencePosingStationList = sceneRootObject.GetComponentsInChildren<SequencePosingStation>(true);
                sequencePosingStations.AddRange(sequencePosingStationList);

                var resetTrackingStationList = sceneRootObject.GetComponentsInChildren<ResetTrackingStation>(true);
                resetTrackingStations.AddRange(resetTrackingStationList);

                var avatarPosingStations = sceneRootObject.GetComponentsInChildren<AvatarPosingStation>(true);
                posingStations.AddRange(avatarPosingStations);

                var syncAvatarPosingStationList = sceneRootObject.GetComponentsInChildren<SyncAvatarPosingStation>(true);
                syncAvatarPosingStations.AddRange(syncAvatarPosingStationList);
            }

            // リセットステーションで使うアニメーターコントローラーを読み込む
            var resetController = (RuntimeAnimatorController)AssetDatabase.LoadAssetAtPath("Assets/AvatarPosingStationUtilities/Animators/ResetTrackingController.controller", typeof(RuntimeAnimatorController));

            // リセットステーションをインスタンス化する
            foreach (var resetTrackingStationInstaller in resetTrackingStations)
            {
                var vrcStation = resetTrackingStationInstaller.gameObject.GetComponent<VRC.SDK3.Components.VRCStation>();

                vrcStation.animatorController = resetController;
                vrcStation.seated = false;
                vrcStation.disableStationExit = false;
                vrcStation.canUseStationFromStation = false;
            }

            
            // SequencePosingStationの事前処理
            var poseStationPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/AvatarPosingStationUtilities/Prefabs/PosingStation.prefab", typeof(GameObject));

            foreach (var sequencePosingStation in sequencePosingStations)
            {
                var poseStationList = new List<AvatarPosingStation>();

                for (int i = 0; i < sequencePosingStation.PoseClips.Length; i++)
                {
                    // ポーズ変更用のアニメーションコントローラーを生成する
                    var poseClip = sequencePosingStation.PoseClips[i];
                    var poseStationObj = Object.Instantiate(poseStationPrefab, sequencePosingStation.transform);
                    poseStationObj.name = poseStationPrefab.name + "_" + poseClip.name;

                    var poseStation = poseStationObj.GetComponent<AvatarPosingStation>();
                    var vrcStation = poseStationObj.GetComponent<VRC.SDK3.Components.VRCStation>();

                    // ポーズアニメーションのアニメーターコントローラーテンプレートを取得する。
                    var poseController = (AnimatorController)AssetDatabase.LoadAssetAtPath(poseStation.animatorControllerPath, typeof(AnimatorController));

                    if(poseController == null)
                    {
                        Debug.LogError($"animatorControllerPath is not found. {poseStation.animatorControllerPath}");
                        continue;
                    }

                    var tmpPoseController = RoughlyDeepCopyAnimatorController(poseController);
                    tmpPoseController.name = tmpPoseController.name + "_" + poseClip.name;

                    var layer = tmpPoseController.layers[0];
                    var stateMachine = layer.stateMachine;
                    foreach (var childState in stateMachine.states)
                    {
                        // PoseChangeMotionステートのモーションを変更する。
                        if (childState.state.name == "PoseChangeMotion")
                        {
                            childState.state.motion = poseClip;
                            break;
                        }
                    }

                    vrcStation.animatorController = tmpPoseController;
                    poseStation.m_sequencePosingStation = sequencePosingStation;
                    poseStation.m_station = vrcStation;
                    poseStation.m_resetTrackingStation = null;
                    poseStation.StationID = i;
                    poseStation.IsAvatarScaleAdjust = sequencePosingStation.IsAvatarScaleAdjust;
                    poseStation.m_defaultAvatarScale = sequencePosingStation.m_defaultAvatarScale;
                    poseStationList.Add(poseStation);
                }

                sequencePosingStation.avatarPosingStations = poseStationList.ToArray();
            }


            // ポーズステーションをインスタンス化

            foreach (var avatarPosingStation in posingStations)
            {
                // ポーズアニメーションのアニメーターコントローラーテンプレートを取得する。
                var poseController = (AnimatorController)AssetDatabase.LoadAssetAtPath(avatarPosingStation.animatorControllerPath, typeof(AnimatorController));

                if(poseController == null)
                {
                    Debug.LogError($"animatorControllerPath is not found. {avatarPosingStation.animatorControllerPath}");
                    continue;
                }

                // アニメーターコントローラーを複製 
                var tmpPoseController = RoughlyDeepCopyAnimatorController(poseController);

                var layer = tmpPoseController.layers[0];
                var stateMachine = layer.stateMachine;

                foreach (var childState in stateMachine.states)
                {
                    // PoseChangeMotionステートのモーションを変更する。
                    if (childState.state.name == "PoseChangeMotion")
                    {
                        childState.state.motion = avatarPosingStation.PoseClip;
                        break;
                    }
                }

                if(tmpPoseController.layers.Length > 2){
                    var facialLayer = tmpPoseController.layers[2];
                    var facialStateMachine = facialLayer.stateMachine;
                    foreach (var childState in facialStateMachine.states)
                    {
                        // PoseChangeMotionステートのモーションを変更する。
                        if (childState.state.name == "Facial")
                        {
                            childState.state.motion = avatarPosingStation.FacialClip;
                        }
                    }
                }

                var vrcStation = avatarPosingStation.gameObject.GetComponent<VRC.SDK3.Components.VRCStation>();
                vrcStation.animatorController = tmpPoseController;
                vrcStation.seated = false;
                //vrcStation.disableStationExit = true;
                vrcStation.canUseStationFromStation = false;

                avatarPosingStation.m_station = vrcStation;
            }

            foreach(var syncAvatarPosingStation in syncAvatarPosingStations)
            {
                // 同期ポーズアニメーションのアニメーターコントローラーテンプレートを取得する。
                var syncPoseController = (AnimatorController)AssetDatabase.LoadAssetAtPath(syncAvatarPosingStation.animatorControllerPath, typeof(AnimatorController));

                if(syncPoseController == null)
                {
                    Debug.LogError($"animatorControllerPath is not found. {syncAvatarPosingStation.animatorControllerPath}");
                    continue;
                }

                var tmpPoseController = RoughlyDeepCopyAnimatorController(syncPoseController);
                var layer = tmpPoseController.layers[0];
                var stateMachine = layer.stateMachine;

                foreach(var childState in stateMachine.states)
                {
                    // PoseChangeMotionステートのモーションを変更する。
                    if (childState.state.name == "PoseChangeMotion")
                    {
                        childState.state.motion = syncAvatarPosingStation.PoseClip;
                        break;
                    }
                }

                var facialLayer = tmpPoseController.layers[2];
                var facialStateMachine = facialLayer.stateMachine;
                foreach (var childState in facialStateMachine.states)
                {
                    // PoseChangeMotionステートのモーションを変更する。
                    if (childState.state.name == "Facial")
                    {
                        childState.state.motion = syncAvatarPosingStation.FacialClip;
                    }
                }


                var vrcStation = syncAvatarPosingStation.gameObject.GetComponent<VRC.SDK3.Components.VRCStation>();
                vrcStation.animatorController = tmpPoseController;
                vrcStation.seated = false;
                vrcStation.disableStationExit = true;
                vrcStation.canUseStationFromStation = false;

                syncAvatarPosingStation.m_station = vrcStation;
            }

        }

        /// <summary>
        /// アニメーターコントローラーをざっくりディープコピーする（BlendTree、サブステートマシンはディープコピーしません）
        /// </summary>
        /// <param name="originalController">コピー元コントローラ</param>
        /// <returns></returns>
        AnimatorController RoughlyDeepCopyAnimatorController(AnimatorController originalController)
        {
            if (originalController != null)
            {
                var tempController = new AnimatorController();
                tempController.name = originalController.name + "_temp";

                // パラメーターを複製する
                foreach (var parameter in originalController.parameters)
                {
                    tempController.AddParameter(parameter);
                }


                // レイヤー情報の複製
                for (int i = 0; i < originalController.layers.Length; i++)
                {
                    // レイヤー設定をコピーする
                    var layer = new UnityEditor.Animations.AnimatorControllerLayer();
                    layer.name = originalController.layers[i].name;
                    layer.iKPass = originalController.layers[i].iKPass;
                    layer.avatarMask = originalController.layers[i].avatarMask;
                    layer.blendingMode = originalController.layers[i].blendingMode;
                    layer.defaultWeight = originalController.layers[i].defaultWeight;
                    layer.syncedLayerAffectsTiming = originalController.layers[i].syncedLayerAffectsTiming;
                    layer.syncedLayerIndex = originalController.layers[i].syncedLayerIndex;

                    layer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();

                    // レイヤーを追加
                    tempController.AddLayer(layer);
                    var tempLayer = tempController.layers[i];

                    var tempStateMachine = tempLayer.stateMachine;

                    // のちのTransition設定で使用するステートの名前検索用辞書
                    Dictionary<string, AnimatorState> stateDict = new Dictionary<string, AnimatorState>();

                    // ステートを複製する
                    for (int j = 0; j < originalController.layers[i].stateMachine.states.Length; j++)
                    {
                        var originalState = originalController.layers[i].stateMachine.states[j].state;

                        var tempState = tempStateMachine.AddState(originalState.name, originalController.layers[i].stateMachine.states[j].position);
                        tempState.motion = originalState.motion;
                        tempState.speed = originalState.speed;
                        tempState.speedParameterActive = originalState.speedParameterActive;
                        tempState.speedParameter = originalState.speedParameter;
                        tempState.timeParameter = originalState.timeParameter;
                        tempState.timeParameterActive = originalState.timeParameterActive;
                        tempState.cycleOffset = originalState.cycleOffset;
                        tempState.cycleOffsetParameter = originalState.cycleOffsetParameter;
                        tempState.cycleOffsetParameterActive = originalState.cycleOffsetParameterActive;
                        tempState.iKOnFeet = originalState.iKOnFeet;
                        tempState.mirror = originalState.mirror;
                        tempState.mirrorParameter = originalState.mirrorParameter;
                        tempState.mirrorParameterActive = originalState.mirrorParameterActive;
                        tempState.writeDefaultValues = originalState.writeDefaultValues;

                        foreach (var originalBehavior in originalState.behaviours)
                        {
                            // 同じ型の StateMachineBehavior を新しく追加
                            var newBehavior = tempState.AddStateMachineBehaviour(originalBehavior.GetType());
                            // 元ビヘイビアの内容をコピー
                            EditorUtility.CopySerialized(originalBehavior, newBehavior);
                        }

                        stateDict.Add(tempState.name, tempState);
                    }

                    // Anyステートからのトランジションを設定する
                    foreach (var originAnyTransition in originalController.layers[i].stateMachine.anyStateTransitions)
                    {
                        var destinationState = originAnyTransition.destinationState;

                        var tempDestinationState = stateDict[destinationState.name];
                        var transition = tempStateMachine.AddAnyStateTransition(tempDestinationState);

                        transition.hasExitTime = originAnyTransition.hasExitTime;
                        transition.exitTime = originAnyTransition.exitTime;
                        transition.hasFixedDuration = originAnyTransition.hasFixedDuration;
                        transition.duration = originAnyTransition.duration;
                        transition.offset = originAnyTransition.offset;
                        transition.interruptionSource = originAnyTransition.interruptionSource;
                        transition.orderedInterruption = originAnyTransition.orderedInterruption;
                        transition.canTransitionToSelf = originAnyTransition.canTransitionToSelf;


                        foreach (var condition in originAnyTransition.conditions)
                        {
                            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
                        }
                    }

                    // Entryからのトランジションを設定する
                    foreach (var originEntryTransition in originalController.layers[i].stateMachine.entryTransitions)
                    {
                        var destinationState = originEntryTransition.destinationState;
                        var tempDestinationState = stateDict[destinationState.name];
                        var transition = tempStateMachine.AddEntryTransition(tempDestinationState);

                        transition.isExit = originEntryTransition.isExit;
                        transition.mute = originEntryTransition.mute;
                        transition.solo = originEntryTransition.solo;
                        transition.hideFlags = originEntryTransition.hideFlags;

                        foreach (var condition in originEntryTransition.conditions)
                        {
                            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
                        }
                    }

                    // デフォルトステートを設定する
                    var defaultStateName = originalController.layers[i].stateMachine.defaultState.name;
                    var tempDefaultState = stateDict[defaultStateName];

                    tempStateMachine.defaultState = tempDefaultState;


                    // それぞれのステートからのトランジションの設定
                    for (int j = 0; j < originalController.layers[i].stateMachine.states.Length; j++)
                    {
                        var originState = originalController.layers[i].stateMachine.states[j];
                        var stateName = originState.state.name;
                        var tempState = stateDict[stateName];

                        foreach (var originTransition in originState.state.transitions)
                        {

                            if (!originTransition.isExit)
                            {
                                var destinationState = originTransition.destinationState;

                                if (stateDict.ContainsKey(destinationState.name))
                                {
                                    var tempDestinationState = stateDict[destinationState.name];

                                    var transition = tempState.AddTransition(tempDestinationState);

                                    transition.hasExitTime = originTransition.hasExitTime;
                                    transition.exitTime = originTransition.exitTime;
                                    transition.hasFixedDuration = originTransition.hasFixedDuration;
                                    transition.duration = originTransition.duration;
                                    transition.offset = originTransition.offset;
                                    transition.interruptionSource = originTransition.interruptionSource;
                                    transition.orderedInterruption = originTransition.orderedInterruption;
                                    transition.canTransitionToSelf = originTransition.canTransitionToSelf;

                                    foreach (var condition in originTransition.conditions)
                                    {
                                        transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"{destinationState.name} is not exitst");
                                }
                            }
                            else
                            {

                                var transition = tempState.AddExitTransition(originTransition.hasExitTime);

                                transition.hasExitTime = originTransition.hasExitTime;
                                transition.exitTime = originTransition.exitTime;
                                transition.hasFixedDuration = originTransition.hasFixedDuration;
                                transition.duration = originTransition.duration;
                                transition.offset = originTransition.offset;
                                transition.interruptionSource = originTransition.interruptionSource;
                                transition.orderedInterruption = originTransition.orderedInterruption;
                                transition.canTransitionToSelf = originTransition.canTransitionToSelf;

                                foreach (var condition in originTransition.conditions)
                                {
                                    transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
                                }
                            }
                        }
                    }
                }

                return tempController;
            }
            return null;
        }
    }
}