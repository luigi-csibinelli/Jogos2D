using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public string primeiraSala = "Sala01_Saguao";
    public GameObject painelControles;
    public GameObject botaoSair;

    void Start()
    {
        Cursor.visible = true;
        Time.timeScale = 1;
        AudioListener.pause = false;
        painelControles.SetActive(false);
        // no navegador nao existe "sair do jogo"
        if (Application.platform == RuntimePlatform.WebGLPlayer) botaoSair.SetActive(false);
    }

    void Update()
    {
        if (painelControles.activeSelf && Input.GetKeyDown(KeyCode.Escape)) painelControles.SetActive(false);
    }

    public void NovoJogo()
    {
        EstadoSalas.Limpar();
        SceneManager.LoadScene(primeiraSala);
    }

    public void AlternarControles()
    {
        painelControles.SetActive(!painelControles.activeSelf);
    }

    public void Sair()
    {
        Application.Quit();
    }
}
