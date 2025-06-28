using UnityEngine;
using PrimeTween;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScenesAnimation : MonoBehaviour
{
    [SerializeField] public Image xImage;
    [SerializeField] RectTransform yCanvasRect;
    [SerializeField] GameObject xCanvas;
    [SerializeField] GameObject yCanvas;
    private const Ease AnimationEase = Ease.Linear;
    public float fadeDuration;
    public float showDuration;
    

    private void Start()
    {
        xCanvas.SetActive(true);
        yCanvas.SetActive(true);
        Sequence.Create(cycles: 1, cycleMode: CycleMode.Yoyo)
            .Chain(Tween.Alpha(xImage, 1f, fadeDuration))
            .Chain(Tween.UIAnchoredPositionX(yCanvasRect, 0, showDuration, AnimationEase));
        
    }
    
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    
}
