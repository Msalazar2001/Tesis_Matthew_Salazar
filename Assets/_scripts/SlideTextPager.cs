using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PageData
{
    [TextArea(3, 6)] public string text;
    public Sprite image;
}

public class SlideTextPager : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform panel;
    public TextMeshProUGUI label;
    public Image imageHolder;
    public CanvasGroup cg;
    [Header("Anim")]
    public float duration = 0.4f;
    public Ease ease = Ease.InOutSine;
    [Header("Contenido")]
    public List<PageData> pages = new List<PageData>();

    [Header("Botones")]
    public GameObject finalButton;   // Botón final opcional
    public GameObject nextButton;    // NUEVO: Botón "Next"

    int index = 0;
    bool busy = false;
    float slideDistance;

    void Awake()
    {
        if (panel == null) panel = GetComponent<RectTransform>();
        if (cg == null) cg = GetComponent<CanvasGroup>();
        slideDistance = panel.rect.width + 100f;

        if (pages.Count > 0)
            SetPage(0);

        if (finalButton != null)
            finalButton.SetActive(false); // Ocultar al inicio
    }

    void SetPage(int i)
    {
        if (label) label.text = pages[i].text;
        if (imageHolder && pages[i].image)
        {
            imageHolder.sprite = pages[i].image;
            imageHolder.enabled = true;
        }
        else if (imageHolder)
        {
            imageHolder.enabled = false;
        }

        // Mostrar botón final solo en la última página
        if (finalButton != null)
            finalButton.SetActive(i == pages.Count - 1);

        // Ocultar botón Next en la última página
        if (nextButton != null)
            nextButton.SetActive(i < pages.Count - 1);
    }

    public void Next()
    {
        if (busy || pages.Count <= 1) return;
        int next = (index + 1) % pages.Count;
        StartCoroutine(Swap(next, +1));
    }

    IEnumerator Swap(int newIndex, int dir)
    {
        busy = true;
        Vector2 centerPos = panel.anchoredPosition;
        Vector2 outPos = centerPos + new Vector2(-dir * slideDistance, 0);

        Sequence s = DOTween.Sequence();
        s.Join(panel.DOAnchorPos(outPos, duration).SetEase(ease));
        s.Join(cg.DOFade(0f, duration * 0.8f));
        yield return s.WaitForCompletion();

        SetPage(newIndex);
        panel.anchoredPosition = centerPos + new Vector2(dir * slideDistance, 0);

        s = DOTween.Sequence();
        s.Join(panel.DOAnchorPos(centerPos, duration).SetEase(ease));
        s.Join(cg.DOFade(1f, duration * 0.8f));
        yield return s.WaitForCompletion();

        index = newIndex;
        busy = false;
    }
}
