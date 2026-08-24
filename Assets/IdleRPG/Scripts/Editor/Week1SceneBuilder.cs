#if UNITY_EDITOR
using IdleRPG.Runtime.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleRPG.Editor
{
    public static class Week1SceneBuilder
    {
        [MenuItem("Idle RPG/Build Week 1 Demo Scene")]
        public static void BuildWeek1DemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject bootstrapObject = new GameObject("MVP Scene Controller");
            bootstrapObject.AddComponent<MvpSceneController>();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Week1VerticalSlice.unity");
            EditorUtility.DisplayDialog(
                "Idle RPG",
                "Week1VerticalSlice scene was created with actors, components, and HUD. Press Play to run the MVP combat loop.",
                "OK");
        }
    }
}
#endif
