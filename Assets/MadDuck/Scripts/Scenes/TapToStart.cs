using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class TapToStart : MonoBehaviour
{
    private bool gameStarted = false;

    void Update()
    {
        if (gameStarted) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            StartGame();
        }
#elif UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            StartGame();
        }
#endif
    }

    void StartGame()
    {
        gameStarted = true;
        SceneManager.LoadScene("Gameplay");
        Debug.Log("Game Started!");
    }
}