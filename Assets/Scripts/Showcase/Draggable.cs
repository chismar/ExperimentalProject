﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    
    
    RectTransform transform;

    void Awake()
    {
        transform = gameObject.GetComponent<RectTransform>();
        
    }
    

    bool dragged = false;
    Vector2 preDragPivot;

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragged = true;

        GetComponent<CanvasGroup>().blocksRaycasts = false;
        
    }



    public void OnDrag(PointerEventData eventData)
    {
        //transform.position = eventData.position;
        transform.position += (Vector3)eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragged = false;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }
    
    


}
