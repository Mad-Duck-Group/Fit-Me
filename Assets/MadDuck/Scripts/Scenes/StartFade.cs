using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Serialization;

public class ScenesFade : MonoBehaviour
{
    [FormerlySerializedAs("whiteBackground")] public Image xImage; // อ้างอิงถึง Image Component ของ WhiteBG (ตอนนี้เราจะใช้ StartCanvas แทน แต่เก็บไว้ก่อนเผื่อใช้)
    [FormerlySerializedAs("madduckLogo")] public Image yImage;     // อ้างอิงถึง Image Component ของ MadduckIcon

    [FormerlySerializedAs("startCanvas")] public GameObject xCanvas;    // อ้างอิงถึง GameObject ของ StartCanvas
    [FormerlySerializedAs("agreementCanvas")] public GameObject yCanvas; // อ้างอิงถึง GameObject ของ AgreementCanvas

    public float fadeDuration = 2.0f; // ระยะเวลาในการเฟด (หน่วยเป็นวินาที)

    void Start()
    {
        // ตรวจสอบว่ามี GameObject ที่อ้างอิงอยู่หรือไม่
        if (xImage == null || yImage == null || xCanvas == null || yCanvas == null)
        {
            Debug.LogError("กรุณากำหนด White Background, Madduck Logo, Start Canvas และ Agreement Canvas ใน Inspector ของ FadeEffect Script.");
            return;
        }

        // ตั้งค่า MadduckIcon ให้โปร่งใสในตอนแรก
        Color logoColor = yImage.color;
        logoColor.a = 0f;
        yImage.color = logoColor;

        // ตรวจสอบให้แน่ใจว่า StartCanvas เปิดอยู่และ AgreementCanvas ปิดอยู่เมื่อเริ่มเกม
        xCanvas.SetActive(true);
        yCanvas.SetActive(false);

        // เริ่ม Coroutine สำหรับการเฟดโลโก้
        StartCoroutine(FadeInLogoAndSwitchCanvas());
    }

    IEnumerator FadeInLogoAndSwitchCanvas()
    {
        float timer = 0f;
        Color logoColor = yImage.color;

        // === ส่วนที่ 1: เฟด MadduckIcon ขึ้นมา ===
        while (timer < fadeDuration)
        {
            // ตรวจสอบการกดข้าม (สำหรับ PC: คลิกซ้าย, สำหรับมือถือ: แตะ)
            // Input.GetMouseButtonDown(0) ตรวจจับการคลิกเมาส์ปุ่มซ้าย
            // Input.touchCount > 0 ตรวจจับว่ามีการแตะ
            // Input.GetTouch(0).phase == TouchPhase.Began ตรวจจับการเริ่มแตะนิ้วแรก
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                Debug.Log("กดเพื่อข้ามแล้ว !!");
                break; // ออกจาก Loop ทันทีเพื่อข้ามการเฟด
            }

            timer += Time.deltaTime;
            float normalizedTime = timer / fadeDuration; // ค่า 0 ถึง 1 สำหรับการไล่ระดับ

            // เฟด MadduckIcon จากโปร่งใสไปทึบ
            logoColor.a = normalizedTime; // จาก 0 ถึง 1
            yImage.color = logoColor;

            yield return null; // รอเฟรมถัดไป
        }

        // === ส่วนที่ 2: ตั้งค่าสถานะสุดท้ายและปิด StartCanvas และเปิด AgreementCanvas ===

        // ตรวจสอบให้แน่ใจว่า MadduckIcon ทึบสมบูรณ์แล้ว (ในกรณีที่ไม่ได้ถูกข้าม)
        // หรือถ้าถูกข้าม ก็จะตั้งค่าให้ทึบไปเลย
        logoColor.a = 1f;
        yImage.color = logoColor;

        xCanvas.SetActive(false);    // ปิด StartCanvas
        yCanvas.SetActive(true); // เปิด AgreementCanvas
    }
}