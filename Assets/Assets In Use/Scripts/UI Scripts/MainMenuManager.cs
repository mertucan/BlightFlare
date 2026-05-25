using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager:MonoBehaviour
{
    // Start butonuna tıklandığında çalışacak fonksiyon
    public void StartGame()
    {
        // "GameScene" yazan yere asıl oyun sahnenin tam adını yazmalısın
        SceneManager.LoadScene("Level1"); 
    }

    // Quit butonuna tıklandığında çalışacak fonksiyon
    public void QuitGame()
    {
        Debug.Log("Oyun kapatılıyor..."); // Editörde çalıştığını görmek için
        Application.Quit(); 
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
