using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool
{
    [Serializable]
    public class MUCTMeshRemovalData : MonoBehaviour, ISerializationCallbackReceiver, IEditorOnly
    {
        [SerializeField]
        private bool useModularAvatar = false;
        
        [SerializeField]
        private string customName = "";
        
        [SerializeField]
        [HideInInspector]
        private int associatedShapeFilterInstanceId;
        
        [SerializeField]
        [HideInInspector]
        private int associatedMenuItemInstanceId;
        
        public bool UseModularAvatar
        {
            get => useModularAvatar;
            set => useModularAvatar = value;
        }
        
        public string CustomName
        {
            get => customName;
            set => customName = value;
        }
        
        public int AssociatedShapeFilterInstanceId
        {
            get => associatedShapeFilterInstanceId;
            set => associatedShapeFilterInstanceId = value;
        }
        
        public int AssociatedMenuItemInstanceId
        {
            get => associatedMenuItemInstanceId;
            set => associatedMenuItemInstanceId = value;
        }
        
        void Start()
        {
        }
        
        public void OnBeforeSerialize()
        {
            // 何もしない
        }
        
        public void OnAfterDeserialize()
        {
            // 何もしない
        }
        
        [Serializable]
        public class RemovalInfo
        {
            public Renderer targetRenderer;
            public List<int> verticesToRemove = new List<int>();
            public string originalMeshPath;
            
            public RemovalInfo(Renderer renderer)
            {
                targetRenderer = renderer;
            }
        }
        
        [HideInInspector]
        public List<RemovalInfo> removalInfos = new List<RemovalInfo>();
        
        public RemovalInfo GetOrCreateRemovalInfo(Renderer renderer)
        {
            var existing = removalInfos.Find(info => info.targetRenderer == renderer);
            if (existing != null)
                return existing;
            
            var newInfo = new RemovalInfo(renderer);
            removalInfos.Add(newInfo);
            return newInfo;
        }
        
    }
}