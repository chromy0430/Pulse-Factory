using UnityEditor;
using UnityEditor.SceneManagement;
namespace PulseFactory.EditorTools
{
    internal static class PFMenuPreview
    {
        [MenuItem("Tools/PF UI/메인 메뉴 씬 열기")]
        private static void OpenMenu()
        {
            if (!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/1_Scenes/Menu.unity");
        }
        [MenuItem("Tools/PF UI/설정 창 프리팹 열기")]
        private static void OpenOptions() => PrefabStageUtility.OpenPrefab("Assets/7_UI/PFMenu/SettingsDialog.prefab");
    }
}
