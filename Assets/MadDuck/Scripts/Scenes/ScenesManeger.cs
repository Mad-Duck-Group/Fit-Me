using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManeger : MonoBehaviour
{
    private bool gameStarted = false;
    
        void Update()
        {
            if (!gameStarted)
            {
                if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                {
                    StartGame();
                }
            }
        }
    
        void StartGame()
        {
            gameStarted = true;
            SceneManager.LoadScene("Gameplay");
            Debug.Log("Game Started!");
        }
}
