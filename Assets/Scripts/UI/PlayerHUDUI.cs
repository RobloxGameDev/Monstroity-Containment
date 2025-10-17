using System;
using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDUI : MonoBehaviour
{
    public static PlayerHUDUI instance;

    [SerializeField] private Slider staminaBar;

    long staminaBarUp = 0;
    public Vector3 normalStamBarPos;
    RectTransform stamBarRectTransform;
    [SerializeField] private Image bar;

    private void Awake()
    {
        instance = this;
        DOTween.Init(true, true, LogBehaviour.Verbose);
    }

    void Start()
    {
        stamBarRectTransform = staminaBar.GetComponent<RectTransform>();
        stamBarRectTransform.position = new Vector3(stamBarRectTransform.position.x, -normalStamBarPos.y, stamBarRectTransform.position.z);
    }

    void Update()
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - staminaBarUp >= 1.5f)
        {
            DOTween.Kill("StaminaBarRiser");
            stamBarRectTransform.DOMoveY(-normalStamBarPos.y, 0.5f).SetId("StaminaBarRiser");
        }
    }

    public void UpdateStaminaBar(float val)
    {
        staminaBar.value = val;
        staminaBarUp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        DOTween.Kill("StaminaBarRiser");
        stamBarRectTransform.DOMoveY(normalStamBarPos.y, 0.5f).SetId("StaminaBarRiser");

        float greenAmt = val;
        float redAmt = 1f - greenAmt;
        bar.color = new Color(redAmt, greenAmt, 0, 1);
    }
}
