using System;
using UnityEngine;
using VRC.SDKBase;

namespace WPC {
    [System.Serializable]
    public class WPCSetup : MonoBehaviour, IEditorOnly
    {
        public string menuPath;
        public string secretKey;
        public WPCReceiver[] receivers;
        public int setupType;

        [Serializable]
        public class WPCReceiver
        {
            public string name;
            public Parameter[] parameters;
        }
    
        [Serializable]
        public class Parameter
        {
            public string name;
            public float value;
        }

        public void SetSetupType(int value)
        {
            setupType = value;
        }
    }
}
