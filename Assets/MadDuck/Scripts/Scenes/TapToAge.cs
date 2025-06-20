using UnityEngine;

public class TapToAge : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject agePanel;

    private bool startTapped = false;

    void Start()
    {
        startPanel.SetActive(true);
        agePanel.SetActive(false);
    }

    void Update()
    {
        if (startTapped) return;

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            startTapped = true;
            startPanel.SetActive(false);
            agePanel.SetActive(true);
        }
    }
}