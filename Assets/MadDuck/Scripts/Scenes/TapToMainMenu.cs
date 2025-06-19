using UnityEngine;

public class TapToMainMenu : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject mainMenuPanel;

    private bool startTapped = false;

    void Start()
    {
        startPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (startTapped) return;

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            startTapped = true;
            startPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}