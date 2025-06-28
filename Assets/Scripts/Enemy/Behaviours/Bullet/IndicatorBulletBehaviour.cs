using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class IndicatorBulletBehaviour : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float maxDistance;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float showTime;
    private void Awake()
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
    }

    private void OnEnable()
    {
        lineRenderer.positionCount = 2;
        if (Physics.Raycast(transform.position, transform.forward, out var hit, maxDistance, groundLayer))
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.forward * maxDistance);
        }

        Timing.RunCoroutine(SelfDestructionCoroutine(showTime));
    }
    private IEnumerator<float> SelfDestructionCoroutine(float countDown)
    {
        yield return Timing.WaitForSeconds(countDown);
        if(!gameObject) yield break;
        Destroy(gameObject);
    }
}
