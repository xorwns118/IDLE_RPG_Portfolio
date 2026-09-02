#if UNITY_EDITOR
using IdleRPG.Runtime.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleRPG.Editor
{
    [CustomEditor(typeof(MvpSceneController))]
    public sealed class MvpSceneControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty gameContentProperty = serializedObject.FindProperty("GameContent");
            SerializedProperty designerSettingsProperty = serializedObject.FindProperty("DesignerSettings");

            EditorGUILayout.PropertyField(gameContentProperty, true);
            EditorGUILayout.Space(8f);
            EditorGUILayout.PropertyField(designerSettingsProperty, true);
            EditorGUILayout.Space(12f);

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Rebuild MVP Scene Layout", GUILayout.Height(30f)))
                    RebuildSceneLayout();
            }
        }

        private void RebuildSceneLayout()
        {
            MvpSceneController controller = (MvpSceneController)target;
            Undo.RecordObject(controller, "Rebuild MVP Scene Layout");
            controller.RebuildSceneLayout();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(controller.gameObject);

            Scene scene = controller.gameObject.scene;
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
