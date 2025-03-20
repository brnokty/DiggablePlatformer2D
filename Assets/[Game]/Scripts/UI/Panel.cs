using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class Panel : MonoBehaviour
{
    protected CanvasGroup canvasGroup;
    public float appearTime;
    public float disappearTime;

    private Coroutine appearDisappearCoroutine;

    protected virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void Appear(EventArgs eventArgs = null)
    {
        StopAppearDisappearCoroutine();
        appearDisappearCoroutine = StartCoroutine(AppearRoutine());
    }

    public virtual void Appear()
    {
        StopAppearDisappearCoroutine();
        appearDisappearCoroutine = StartCoroutine(AppearRoutine());
    }

    public IEnumerator AppearRoutine()
    {
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime / appearTime;
            yield return new WaitForEndOfFrame();
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        WhenAppearFinished();
    }

    public virtual void Disappear()
    {
        StopAppearDisappearCoroutine();
        appearDisappearCoroutine = StartCoroutine(DisappearRoutine());
    }

    private void StopAppearDisappearCoroutine()
    {
        if (appearDisappearCoroutine != null)
        {
            StopCoroutine(appearDisappearCoroutine);
        }
    }

    public IEnumerator DisappearRoutine()
    {
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / disappearTime;
            yield return new WaitForEndOfFrame();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        WhenDisappearFinished();
    }

    public virtual void WhenAppearFinished()
    {
    }

    public virtual void WhenDisappearFinished()
    {
    }
}