using DG.Tweening;
using UnityEditor;
using UnityEngine;

public class MainMenuBtn : MonoBehaviour
{
    [SerializeField] private RectTransform txt;
    Vector3 normalPos, hoverPos;

    string id;

    void Start()
    {
        normalPos = txt.position;
        hoverPos = normalPos + new Vector3(30, 0, 0);
        id = GUID.Generate().ToString();
    }

    public void OnHover()
    {
        DOTween.Kill(id);
        txt.DOMove(hoverPos, 0.2f);
    }
    
    public void OnHoverExit()
    {
        DOTween.Kill(id);
        txt.DOMove(normalPos, 0.2f);
    }
}
