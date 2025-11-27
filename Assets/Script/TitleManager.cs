using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public GameObject helpPanel;

    public void ClickStart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void ClickHelp()
    {
        if (helpPanel != null) helpPanel.SetActive(true);
    }

    public void ClickQuit()
    {
        Debug.Log("∞‘¿” ≤®¡¸!");
        Application.Quit();
    }

    public void ClickCloseHelp()
    {
        if (helpPanel != null) helpPanel.SetActive(false);
    }
}