using UnityEngine;
using TMPro;

public class Clicktogainage : MonoBehaviour
{
    public TMP_InputField inputField;

    public void IncrementValue()
    {
        int currentValue = ParseInput();
        currentValue++;
        inputField.text = currentValue.ToString();
    }

    public void DecrementValue()
    {
        int currentValue = ParseInput();
        currentValue--;
        
        if (currentValue < 0)
            currentValue = 0;

        inputField.text = currentValue.ToString();
    }

    private int ParseInput()
    {
        int value = 0;
        int.TryParse(inputField.text, out value);
        return value;
    }
    
}
