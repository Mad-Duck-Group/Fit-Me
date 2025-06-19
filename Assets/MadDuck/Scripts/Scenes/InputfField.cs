using TMPro;
using UnityEngine;

public class InputFieldHandler : MonoBehaviour
{
    public TextMeshProUGUI output;
    public TMP_InputField userAge;
    public GameObject agePanal;
    public GameObject mainmenuPanal;

    public void OnSubmit()
    {
        output.text = "Your age is " + userAge.text;
        StartCoroutine(WaitAndDisable());
    }

    private System.Collections.IEnumerator WaitAndDisable()
    {
        yield return new WaitForSeconds(3f);
        agePanal.SetActive(false);
        mainmenuPanal.SetActive(true);
    }
}