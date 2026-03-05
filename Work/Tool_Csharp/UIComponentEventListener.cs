using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using SLua;

[CustomLuaClassAttribute]
public class UIComponentEventListener: EventTrigger {
	public delegate void VoidDelegate(GameObject obj);
	public VoidDelegate onClick;
	public VoidDelegate onDown;
	public VoidDelegate onEnter;
	public VoidDelegate onExit;
	public VoidDelegate onUp;
	public VoidDelegate onSelect;
	public VoidDelegate onUpdateSelect;
	public VoidDelegate onDragEnd;
	public VoidDelegate onDrag;

	static public UIComponentEventListener Get (GameObject go)
	{
		UIComponentEventListener listener = go.GetComponent<UIComponentEventListener>();
		if (listener == null) listener = go.AddComponent<UIComponentEventListener>();
		return listener;
	}

	public override void OnPointerClick (PointerEventData eventData){
		if(onClick != null) onClick(gameObject);
	}

	public override void OnPointerDown (PointerEventData eventData){
		if(onDown != null) onDown(gameObject);
	}

	public override void OnPointerEnter (PointerEventData eventData){
		if(onEnter != null) onEnter(gameObject);
	}

	public override void OnPointerExit (PointerEventData eventData){
		if(onExit != null) onExit(gameObject);
	}

	public override void OnPointerUp (PointerEventData eventData){
		if(onUp != null) onUp(gameObject);
	}

	public override void OnSelect (BaseEventData eventData){
		if(onSelect != null) onSelect(gameObject);
	}

	public override void OnUpdateSelected (BaseEventData eventData){
		if(onUpdateSelect != null) onUpdateSelect(gameObject);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		if(onDragEnd != null) onDragEnd(gameObject);
	}
	public override void OnDrag(PointerEventData eventData)
	{
		if(onDrag != null) onDrag(gameObject);
	}
}
