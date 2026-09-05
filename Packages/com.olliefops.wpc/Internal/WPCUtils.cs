using System.Runtime.InteropServices;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace WPC.Utils
{
    public class WPCUtils : MonoBehaviour, IEditorOnly
    {
        public static AnimationClip CreateGOStateAnim(GameObject gameObject, bool state, float delay, string avatarBuildPath, string animationName)
        {
            // Create the animation clip
            AnimationClip animationClip = new AnimationClip();
            animationClip.name = animationName;
        
            // Create the active-state curve.
            // The property "m_IsActive" controls whether the GameObject is active.
            AnimationCurve curve = new AnimationCurve();
        
            // Create the single keyframe at the specified delay.
            Keyframe keyframe = new Keyframe(delay, state ? 1f : 0f);
            curve.AddKey(keyframe);
        
            // Bind the curve to the GameObject's active state.
            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = AnimationUtility.CalculateTransformPath(
                    gameObject.transform,
                    gameObject.transform.root
                ),
                type = typeof(GameObject),
                propertyName = "m_IsActive"
            };
        
            AnimationUtility.SetEditorCurve(
                animationClip,
                binding,
                curve
            );
        
            // Save the animation asset.
            if (!AssetDatabase.IsValidFolder($"{avatarBuildPath}/Animations")) AssetDatabase.CreateFolder(avatarBuildPath, "Animations");
            AssetDatabase.CreateAsset(animationClip, $"{avatarBuildPath}/Animations/{animationName.Replace("/", "_")}.anim");
            AssetDatabase.SaveAssets();
        
            return animationClip;
        }

        public static void AddVRCMenuToggle(VRCExpressionsMenu menu, string name, VRCExpressionsMenu.Control.ControlType type, float value, [Optional] string parameterName, [Optional] VRCExpressionsMenu subMenu, bool checkIfExists = true)
        {
            if (checkIfExists && MenuToggleExists(menu, name)) return;
            
            VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control();
            
            if (type == VRCExpressionsMenu.Control.ControlType.SubMenu)
            {
                control.name = name; 
                control.type = type;
                control.value = value;
                control.subMenu = subMenu;
                menu.controls.Add(control);
                
                return;
            }
            
            VRCExpressionsMenu.Control.Parameter parameter = new VRCExpressionsMenu.Control.Parameter();
            parameter.name = parameterName;
                
            control.name = name; 
            control.type = type;
            control.value = value;
            control.parameter = parameter;
            
            menu.controls.Add(control);
        }
        
        public static void AddVRCParameter(VRCExpressionParameters parameters, string name, VRCExpressionParameters.ValueType type, float defaultValue = 0f, bool isSaved = false, bool isSynced = false)
        {
            VRCExpressionParameters.Parameter parameter = new VRCExpressionParameters.Parameter();
            parameter.name = name;
            parameter.valueType = type;
            parameter.defaultValue = defaultValue;
            parameter.saved = isSaved;
            parameter.networkSynced = isSynced;
            parameters.parameters = parameters.parameters.AddToArray(parameter);
        }
        
        public static bool MenuToggleExists(VRCExpressionsMenu menu, string name)
        {
            foreach (var control in menu.controls)
            {
                if (control.name == name) return true;
            }

            return false;
        }

        public static void AddToggleLayer(AnimatorController controller, AnimationClip animation, string parameter)
        {
            // Create Layer
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.stateMachine = new AnimatorStateMachine();
            layer.defaultWeight = 1f;
            controller.AddLayer(layer);
            
            // Create ChildMotion
            ChildMotion childMotion = new ChildMotion();
            childMotion.motion = animation;
            childMotion.directBlendParameter = parameter;
            childMotion.timeScale = 1f;
            
            // Create BlendTree & Add ChildMotion
            BlendTree blendTree = new BlendTree();
            blendTree.blendType = BlendTreeType.Direct;
            blendTree.useAutomaticThresholds = false;
            blendTree.children = blendTree.children.AddToArray(childMotion);
            
            // Create State & Set Motion
            AnimatorState state = layer.stateMachine.AddState(parameter);
            state.motion = blendTree;
        }

        public static void AddProxyLayer(AnimatorController controller, string parameter, WPCSetup.Parameter[] parameters)
        {
            // Create Layer
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.name = parameter;
            layer.stateMachine = new AnimatorStateMachine();
            controller.AddLayer(layer);
                
            // Add States
            AnimatorState offState = layer.stateMachine.AddState("Off");
            AnimatorState onState = layer.stateMachine.AddState("On");

            // Add on behaviour
            VRCAvatarParameterDriver behaviour = onState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            foreach (var targetParameter in parameters)
            {
                VRC_AvatarParameterDriver.Parameter vrcDriverParameter = new VRC_AvatarParameterDriver.Parameter();
                vrcDriverParameter.name = targetParameter.name;
                vrcDriverParameter.type = VRC_AvatarParameterDriver.ChangeType.Set;
                vrcDriverParameter.value = targetParameter.value;
                behaviour.parameters.Add(vrcDriverParameter);
            }

            // Add off -> on transition (greater)
            AnimatorStateTransition greaterOnTransition = offState.AddTransition(onState, false);
            greaterOnTransition.exitTime = 0f;
            greaterOnTransition.duration = 0f;
            greaterOnTransition.AddCondition(AnimatorConditionMode.Greater, 0.001f, parameter);
            
            // Add off -> on transition (less)
            AnimatorStateTransition lessOnTransition = offState.AddTransition(onState, false);
            lessOnTransition.exitTime = 0f;
            lessOnTransition.duration = 0f;
            lessOnTransition.AddCondition(AnimatorConditionMode.Less, -0.001f, parameter);
                
            // Add on -> off
            AnimatorStateTransition offTransition = onState.AddTransition(offState, false);
            offTransition.exitTime = 0f;
            offTransition.duration = 0f;
            offTransition.AddCondition(AnimatorConditionMode.Less, 0.001f, parameter);
            offTransition.AddCondition(AnimatorConditionMode.Greater, -0.001f, parameter);
        }
    }
}