using UnityEditor;
using UnityEditor.SceneManagement;

// Al presionar Play en el editor, el juego siempre arranca desde el menú principal
// (igual que en el juego exportado), sin importar qué escena esté abierta.
[InitializeOnLoad]
public static class IniciarDesdeMenu
{
    const string RutaMenu = "Assets/Scenes/MenuPrincipal.unity";

    static IniciarDesdeMenu()
    {
        // Las pruebas automáticas necesitan arrancar directo en la escena abierta
        if (SessionState.GetBool("prueba_activa", false))
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaMenu);
    }
}
