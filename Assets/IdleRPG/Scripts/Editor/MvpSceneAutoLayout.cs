#if UNITY_EDITOR
using IdleRPG.Runtime.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleRPG.Editor
{
    [InitializeOnLoad]
    public static class MvpSceneAutoLayout
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string Week1ScenePath = "Assets/Scenes/Week1VerticalSlice.unity";
        private const string DefaultActorAnimatorControllerPath = "Assets/IdleRPG/Animations/Default_Character.overrideController";
        private static bool Queued;

        static MvpSceneAutoLayout()
        {
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
            QueueRebuild();
        }

        private static void HandleSceneOpened(Scene _Scene, OpenSceneMode _Mode)
        {
            QueueRebuild();
        }

        private static void HandleActiveSceneChanged(Scene _PreviousScene, Scene _NewScene)
        {
            QueueRebuild();
        }

        private static void QueueRebuild()
        {
            if (Queued || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Queued = true;
            EditorApplication.delayCall += RebuildLoadedMvpScenesAfterDelay;
        }

        private static void RebuildLoadedMvpScenesAfterDelay()
        {
            RebuildLoadedMvpScenes();
        }

        private static void RebuildLoadedMvpScenes()
        {
            Queued = false;

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            MvpSceneController[] controllers = Object.FindObjectsOfType<MvpSceneController>(true);
            bool changedAnyScene = false;

            if (controllers.Length == 0 && IsMvpScene(activeScene.path))
            {
                GameObject controllerObject = new GameObject("MVP Scene Controller");
                controllers = new[] { controllerObject.AddComponent<MvpSceneController>() };
                EditorSceneManager.MarkSceneDirty(activeScene);
                changedAnyScene = true;
            }

            foreach (MvpSceneController controller in controllers)
            {
                if (controller == null)
                    continue;

                controller.RebuildSceneLayout();
                AssignDefaultActorAnimatorController(controller);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(controller.gameObject);
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                changedAnyScene = true;
            }

            if (changedAnyScene)
                EditorSceneManager.SaveOpenScenes();
        }

        private static bool IsMvpScene(string _Path)
        {
            return _Path == SampleScenePath || _Path == Week1ScenePath;
        }

        private static void AssignDefaultActorAnimatorController(MvpSceneController _Controller)
        {
            if (_Controller == null || _Controller.DesignerEditableSettings == null || _Controller.DesignerEditableSettings.Actors == null)
                return;

            if (_Controller.DesignerEditableSettings.Actors.AnimatorController != null)
                return;

            RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultActorAnimatorControllerPath);
            if (animatorController != null)
                _Controller.DesignerEditableSettings.Actors.AnimatorController = animatorController;
        }
    }
}
#endif
