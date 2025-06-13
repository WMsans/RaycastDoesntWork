using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sapra.InfiniteLands{
    [System.Serializable]
    public class ViewSettings : InfiniteLandsComponent{
        public bool ForceStayGameMode;

        public List<Camera> BaseCameras = new();
        public List<Transform> BaseTransforms = new();

        [Disabled] public List<Camera> AllCameras = new();
        [Disabled] public List<Transform> AllTransforms = new();

        public Action<Transform> OnTransformAdded;
        public Action<Transform> OnTransformRemoved;
        public Action<Camera> OnCameraAdded;
        public Action<Camera> OnCameraRemoved;

        private int EditorTargetDisplay;
        private bool GameViewFocused;
        public override void Initialize(IControlTerrain lands)
        {
            base.Initialize(lands);

            #if UNITY_EDITOR
            EditorApplication.update -= CheckIfEditorChanges;
            EditorApplication.update += CheckIfEditorChanges;
            #endif
            OnTransformAdded = null;
            OnTransformRemoved = null;
            OnCameraAdded = null;
            OnCameraRemoved = null;
            UpdateCamerasAndTransforms();
        }

        private void UpdateCamerasAndTransforms(){
            var liveCameras = GetCameras();
            var liveTransforms = GetAvailableTransforms();

            var prevCameras = new List<Camera>(AllCameras);
            if(liveCameras != null)
                AllCameras = BaseCameras.Concat(liveCameras).Where(a => a != null).Distinct().ToList();
            else
                AllCameras = new List<Camera>(BaseCameras.Where(a => a != null).Distinct());
            CameraModification(AllCameras.Except(prevCameras), prevCameras.Except(AllCameras));

            var prevTransforms = new List<Transform>(AllTransforms);
            if(liveTransforms != null)
                AllTransforms = BaseTransforms.Concat(liveTransforms).Where(a => a != null).Distinct().ToList();
            else
                AllTransforms = new List<Transform>(BaseTransforms.Where(a => a != null).Distinct());
            TransformModification(AllTransforms.Except(prevTransforms), prevTransforms.Except(AllTransforms));
            
        }
        private void TransformModification(IEnumerable<Transform> added, IEnumerable<Transform> removed){
            foreach(Transform tr in removed){
                OnTransformRemoved?.Invoke(tr);
            }

            foreach(Transform tr in added){
                OnTransformAdded?.Invoke(tr);
            }
        }

        private void CameraModification(IEnumerable<Camera> added, IEnumerable<Camera> removed){
            foreach(Camera cm in removed){
                OnCameraRemoved?.Invoke(cm);
            }

            foreach(Camera cm in added){
                OnCameraAdded?.Invoke(cm);
            }
        }
        public void AddNewTransform(Transform body){
            BaseTransforms.Add(body);
            OnTransformAdded?.Invoke(body);
        }

        public void RemoveTransform(Transform body){
            BaseTransforms.Remove(body);
            OnTransformRemoved?.Invoke(body);
        }
        
        public void AddNewCamera(Camera cam){
            BaseCameras.Add(cam);
            OnCameraAdded?.Invoke(cam);
        }

        public void RemoveCamera(Camera cam){
            BaseCameras.Remove(cam);
            OnCameraRemoved?.Invoke(cam);
        }

        public IEnumerable<Camera> GetCurrentCameras() => AllCameras.Where(a => a != null);
        public IEnumerable<Transform> GetCurrentTransforms() => AllTransforms.Where(a => a != null);

        public override void Disable()
        {
            #if UNITY_EDITOR
            EditorApplication.update -= CheckIfEditorChanges;
            #endif
        }

        private IEnumerable<Transform> GetAvailableTransforms(){
            return FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Select(a => a.transform);
        }
        #if UNITY_EDITOR
        private IEnumerable<Camera> GetCameras(){
            IEnumerable<Camera> target;
            //We are in editor mode and the scene is not playing. lets firts find if gameview is enabled
            
            //If this is the case, we should get the gameview camera
            GameViewFocused = true;
            EditorTargetDisplay = Display.activeEditorGameViewTarget;
            var enabledCameras = Camera.allCameras.Where(a => a.isActiveAndEnabled && a.targetTexture == null);
            target = enabledCameras.Where(a => a.targetDisplay.Equals(EditorTargetDisplay));
            
            if(!RuntimeTools.IsGameViewOpenAndFocused() && !ForceStayGameMode){
                GameViewFocused = false;
                target = target.Concat(SceneView.GetAllSceneCameras());
            }

            var TextureCameras = Camera.allCameras.Where(a => a.isActiveAndEnabled && a.targetTexture != null);
            var results = new List<Camera>(target);
            results.AddRange(TextureCameras);
            return results;
        }
        private void CheckIfEditorChanges(){
            var target = RuntimeTools.IsGameViewOpenAndFocused();
            bool shouldSwap = target != GameViewFocused;
            var shouldChangeCamera = target && EditorTargetDisplay != Display.activeEditorGameViewTarget;
            if(shouldSwap || shouldChangeCamera){
                UpdateCamerasAndTransforms();
            }
        }
        #else
        private IEnumerable<Camera> GetCameras(){
            return Camera.allCameras.Where(a => a.isActiveAndEnabled).ToList();
        }
        #endif
    }
}