#if IMPOSTORS_UNITY_PIPELINE_URP
using Impostors.Managers;
using Impostors.URP;
using UnityEditor;

namespace Impostors.Editor
{
    
    [CustomEditor(typeof(UniversalRenderPipelineProxy))]
    public class UniversalRenderPipelineProxyEditor : UnityEditor.Editor
    {
        private SerializedProperty _ImpostorRenderingType;
        private SerializedProperty _cameraWhereToScheduleCommandBufferExecution;
        private SerializedProperty _updateImpostorsTextureFeature;
        
        private void OnEnable()
        {
            _ImpostorRenderingType = serializedObject.FindProperty("ImpostorRenderingType");
            _cameraWhereToScheduleCommandBufferExecution =
                serializedObject.FindProperty("_cameraWhereToScheduleCommandBufferExecution");
            _updateImpostorsTextureFeature = serializedObject.FindProperty("_updateImpostorsTextureFeature");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_ImpostorRenderingType);
            var t = target as UniversalRenderPipelineProxy;
            if (t.ImpostorRenderingType == UniversalRenderPipelineProxy.ImpostorTextureRenderingType.Scheduled)
            {
                EditorGUILayout.HelpBox("Scheduled rendering provides better performance, but might work incorrectly with VR projects.", MessageType.Warning);
                EditorGUILayout.PropertyField(_cameraWhereToScheduleCommandBufferExecution);
                if (_cameraWhereToScheduleCommandBufferExecution.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox($"Please, specify camera where CommandBuffers will be scheduled. In most cases this is a camera specified in {nameof(CameraImpostorsManager)}.", MessageType.Error);    
                }

                EditorGUILayout.PropertyField(_updateImpostorsTextureFeature);
                if (_updateImpostorsTextureFeature.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Please, specify Render Feaeture. For more details refer to documentation about URP setup.", MessageType.Error);    
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif