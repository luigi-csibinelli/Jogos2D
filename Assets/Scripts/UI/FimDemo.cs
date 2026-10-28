using UnityEngine;
using UnityEngine.SceneManagement;

public class FimDemo : MonoBehaviour
{
    public string cenaMenu = "MainMenu";

    void Start()
    {
        Cursor.visible = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)) Voltar();
    }

    public void Voltar()
    {
        EstadoSalas.Limpar();
        SceneManager.LoadScene(cenaMenu);
    }
}
