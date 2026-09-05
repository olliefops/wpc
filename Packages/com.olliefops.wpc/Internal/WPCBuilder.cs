using System.Collections.Generic;
using System.Linq;
using com.vrcfury.api;
using com.vrcfury.api.Actions;
using com.vrcfury.api.Components;
using HarmonyLib;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using WPC.Utils;

namespace WPC.Builder
{
    public class WPCBuilder : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -10010;
        
        private static readonly string BuildsPath = $"Packages/com.olliefops.wpc/Temp";

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            if (!AssetDatabase.IsValidFolder(BuildsPath)) AssetDatabase.CreateFolder("Packages/com.olliefops.wpc", "Temp");
            WPCSetup[] wpcSetups = avatarGameObject.GetComponentsInChildren<WPCSetup>();
            string avatarName = avatarGameObject.name.Replace("(Clone)", "").Trim();
            
            string avatarBuildPath = $"{BuildsPath}/{avatarName}";
            if (!AssetDatabase.IsValidFolder(avatarBuildPath))
            {
                if (AssetDatabase.DeleteAsset(avatarBuildPath))
                {
                    Debug.Log("WPC: Deleted Avatar Folder");
                }
                AssetDatabase.CreateFolder(BuildsPath, avatarName);
            }
            
            foreach (WPCSetup wpcSetup in wpcSetups)
            {
                switch (wpcSetup.setupType)
                {
                    case 0:
                        if (wpcSetup.secretKey == "" || wpcSetup.menuPath == "" || wpcSetup.receivers.Length == 0) break;
                        SetupReceiver(wpcSetup, avatarName);
                        break;
                    case 1:
                        if (wpcSetup.secretKey == "" || wpcSetup.menuPath == "" || wpcSetup.receivers.Length == 0) break;
                        SetupController(wpcSetup, avatarName);
                        break;
                }
            }

            return true;
        }
        
        public static void SetupReceiver(WPCSetup wpcSetup, string avatarName)
        {
            GameObject container = wpcSetup.gameObject;
            string avatarBuildPath = $"{BuildsPath}/{avatarName}";
            if (!AssetDatabase.IsValidFolder(avatarBuildPath)) AssetDatabase.CreateFolder(BuildsPath, avatarName);
            container.transform.position = new Vector3(0, 0, 0);
            container.SetActive(false);

            // Create Animator & VRCMenu & VRCParameters
            AnimatorController fxController = AnimatorController.CreateAnimatorControllerAtPath($"{avatarBuildPath}/{wpcSetup.secretKey}_Receiver_Animator.controller");
            VRCExpressionsMenu vrcMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            VRCExpressionParameters vrcParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            AssetDatabase.CreateAsset(vrcMenu, $"{avatarBuildPath}/{wpcSetup.secretKey}_Receiver_Menu.asset");
            AssetDatabase.CreateAsset(vrcParameters, $"{avatarBuildPath}/{wpcSetup.secretKey}_Receiver_Parameters.asset");
            
            foreach (var receiver in wpcSetup.receivers)
            {
                string parameterName = $"{wpcSetup.secretKey}_{receiver.name}";
                
                // Add Parameter & Layer
                fxController.AddParameter(parameterName, AnimatorControllerParameterType.Float);
                WPCUtils.AddVRCParameter(vrcParameters, parameterName, VRCExpressionParameters.ValueType.Bool);
                WPCUtils.AddProxyLayer(fxController, parameterName, receiver.parameters);
                
                // Create Receiver
                GameObject receiverGameObject =  new GameObject();
                receiverGameObject.transform.SetParent(container.transform);
                receiverGameObject.name = receiver.name;
                
                // Add Contact Receiver     
                VRCContactReceiver contactReceiver = receiverGameObject.AddComponent<VRCContactReceiver>();
                contactReceiver.rootTransform = container.transform;
                contactReceiver.size = new Vector3(7, 7, 7);
                contactReceiver.position = new Vector3(0, 3f, 0);
                contactReceiver.contentTypes = DynamicsUsageFlags.Avatar;
                contactReceiver.allowSelf = true;
                contactReceiver.shapeType = ContactBase.ShapeType.Box;
                contactReceiver.collisionTags = new List<string> { parameterName };
                contactReceiver.parameter = parameterName;
            }
            
            // Create Receiver Toggle
            string receiverStateParameterName = $"{wpcSetup.secretKey}_Receiver";
            fxController.AddParameter(receiverStateParameterName, AnimatorControllerParameterType.Float);
            AnimationClip receiverOnAnimation = WPCUtils.CreateGOStateAnim(container, true, 0f, avatarBuildPath, $"{wpcSetup.secretKey}_Receiver_On");
            WPCUtils.AddVRCParameter(vrcParameters, receiverStateParameterName, VRCExpressionParameters.ValueType.Bool, 0f, true);
            WPCUtils.AddVRCMenuToggle(vrcMenu, "Receiver", VRCExpressionsMenu.Control.ControlType.Toggle, 1f, receiverStateParameterName);
            WPCUtils.AddToggleLayer(fxController, receiverOnAnimation, receiverStateParameterName);
            
            // Save VRCMenu & VRCParameters
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Setup VRCFury Full Controller
            FuryFullController fullController = FuryComponents.CreateFullController(wpcSetup.gameObject);
            fullController.AddController(fxController);
            fullController.AddMenu(vrcMenu, wpcSetup.menuPath);
            fullController.AddParams(vrcParameters);
            fullController.AddGlobalParam("*");
        }

        public static void SetupController(WPCSetup wpcSetup, string avatarName)
        {
            GameObject container = wpcSetup.gameObject;
            string avatarBuildPath = $"{BuildsPath}/{avatarName}";
            if (!AssetDatabase.IsValidFolder(avatarBuildPath)) AssetDatabase.CreateFolder(BuildsPath, avatarName);
            container.transform.position = new Vector3(0, 0, 0);

            // Create Animator & VRCMenu & VRCParameters
            AnimatorController fxController = AnimatorController.CreateAnimatorControllerAtPath($"{avatarBuildPath}/{wpcSetup.secretKey}_Controller_Animator.controller");
            VRCExpressionsMenu vrcMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            VRCExpressionParameters vrcParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            
            // Save VRCMenu & VRCParameters
            AssetDatabase.CreateAsset(vrcMenu, $"{avatarBuildPath}/{wpcSetup.secretKey}_Controller_Menu.asset");
            AssetDatabase.CreateAsset(vrcParameters, $"{avatarBuildPath}/{wpcSetup.secretKey}_Controller_Parameters.asset");
            
            foreach (var receiver in wpcSetup.receivers)
            {
                string parameterName = $"{wpcSetup.secretKey}_{receiver.name}";
                
                // Create Controller
                GameObject controllerGameObject =  new GameObject();
                controllerGameObject.SetActive(false);
                controllerGameObject.transform.SetParent(container.transform);
                controllerGameObject.name = receiver.name;
                
                // Add Contact Sender   
                VRCContactSender contactController = controllerGameObject.AddComponent<VRCContactSender>();
                contactController.rootTransform = container.transform;
                contactController.size = new Vector3(7, 7, 7);
                contactController.position = new Vector3(0, 3f, 0);
                contactController.contentTypes = DynamicsUsageFlags.Avatar;
                contactController.shapeType = ContactBase.ShapeType.Box;
                contactController.collisionTags = new List<string> { parameterName };
                
                // Create Controller Toggle
                fxController.AddParameter(parameterName, AnimatorControllerParameterType.Float);
                AnimationClip controllerAnimation = WPCUtils.CreateGOStateAnim(controllerGameObject, true, 0f, avatarBuildPath, $"{parameterName}_On");
                WPCUtils.AddVRCParameter(vrcParameters, parameterName, VRCExpressionParameters.ValueType.Bool, 0f, false, true);
                WPCUtils.AddToggleLayer(fxController, controllerAnimation, parameterName);
                
                string[] vrcMenuItems = receiver.name.Split('/');
                VRCExpressionsMenu vrcSubMenu = vrcMenu;
                
                foreach (var vrcMenuItem in vrcMenuItems)
                {
                    string subMenuPath = $"{avatarBuildPath}/{wpcSetup.secretKey}_Controller_SubMenu_{vrcMenuItem}.asset";
                    
                    if (vrcMenuItem == vrcMenuItems.Last())
                    {
                        WPCUtils.AddVRCMenuToggle(vrcSubMenu, vrcMenuItem, VRCExpressionsMenu.Control.ControlType.Button, 1f, parameterName, null);
                    }
                    else
                    {
                        VRCExpressionsMenu newVRCSubMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(subMenuPath);
                        if (newVRCSubMenu == null)
                        {
                            newVRCSubMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                            AssetDatabase.CreateAsset(newVRCSubMenu, subMenuPath);
                        }
                        
                        WPCUtils.AddVRCMenuToggle(vrcSubMenu, vrcMenuItem, VRCExpressionsMenu.Control.ControlType.SubMenu, 0f, "", newVRCSubMenu);
                        vrcSubMenu = newVRCSubMenu;
                    }
                }
            }
            
            // Save VRCMenu & VRCParameters
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Setup VRCFury Full Controller
            FuryFullController fullController = FuryComponents.CreateFullController(wpcSetup.gameObject);
            fullController.AddController(fxController);
            fullController.AddMenu(vrcMenu, wpcSetup.menuPath);
            fullController.AddParams(vrcParameters);
            fullController.AddGlobalParam("*");
        }
    }
}