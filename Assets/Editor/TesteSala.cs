using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Roda o roteiro TesteAutomatico na Sala 01 dentro do editor.
// Window > General > Test Runner > EditMode, ou na linha de comando com -runTests.
public class TesteSala
{
    [UnityTest]
    public IEnumerator RoteiroDaSala01()
    {
        PlayerPrefs.SetInt("teste_forcar", 1);
        PlayerPrefs.SetInt("teste_fim", 0);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/Scenes/Sala01_Saguao.unity");

        yield return new EnterPlayMode();
        float limite = Time.realtimeSinceStartup + 90f;
        while (PlayerPrefs.GetInt("teste_fim", 0) == 0 && Time.realtimeSinceStartup < limite)
            yield return null;
        yield return new ExitPlayMode();

        PlayerPrefs.SetInt("teste_forcar", 0);
        int ok = PlayerPrefs.GetInt("teste_ok", 0);
        int falhas = PlayerPrefs.GetInt("teste_falhas", -1);
        Assert.AreEqual(1, PlayerPrefs.GetInt("teste_fim", 0), "o roteiro nao terminou");
        Assert.AreEqual(0, falhas, "falhas no roteiro (veja as linhas TESTE FALHA no log)");
        Assert.Greater(ok, 30);
    }
}
