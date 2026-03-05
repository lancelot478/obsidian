using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SLua;

[CustomLuaClassAttribute]
public class AnimEvent : MonoBehaviour {
	public bool isStartPlay;
	public bool isReceiveEvent = true;
	public GameObject[] objEffectArr;
	public ParticleSystem[] parEffectArr;
	public AudioSource[] souArr;
	public GameObject[] destroyObjArr;
	public GameObject[] disableObjArr;
	public GameObject[] enableObjArr;

    public string roleKey;
    private CS2LuaCustomEventsEntry entry;
    
    // 测试移动音效使用
    public Action<string> onPlayMoveSoundCallback;

	void Start() {
		CS2LuaCustomEventsEntry _entry =
			CS2LuaEventsEntryInstance.GetInstance().GetComponent<CS2LuaCustomEventsEntry>();
		if (_entry != null) {
			entry = _entry;
		}

		if (isStartPlay) {
			ReceiveEffectEvent(transform.name);
		}

	}

	void ReceiveEffectEvent(string animName)
	{
		if(isReceiveEvent)
		{
			foreach(GameObject i in objEffectArr)
			{
				if(i.name.Contains(animName))
				{
					i.gameObject.SetActive(true);
					i.GetComponent<Animator>().Play(animName);
				}
			}
			foreach(ParticleSystem i in parEffectArr)
			{
				if(i.name.Contains(animName))
				{
					i.Play(true);
				}
			}
		}
	}

	void ReceiveAnimEvent(string animName)
	{
		if(isReceiveEvent) {
			if (entry != null) entry.OnReceiveAnimEvent(this, roleKey+"_"+animName);
			onPlayMoveSoundCallback?.Invoke(animName);
		}
	}
	void ReceiveSouEvent(string animName)
	{
		if (isReceiveEvent)
		{
			foreach (AudioSource i in souArr)
			{
				if (i.name == animName)
				{
					//if (entry != null) entry.OnPlaySoundEvent(souArr);
					i.Play();
					break;
				}
			}
		}
	}

	void ReceiveAllEvent(string animName)
	{
		ReceiveSouEvent(animName);
		ReceiveEffectEvent(animName);
		ReceiveAnimEvent(animName);
	}

	void EnableObj()
	{
		foreach(GameObject i in enableObjArr)
		{
			i.SetActive(true);
		}
	}
	void DisableObj()
	{
		foreach(GameObject i in disableObjArr)
		{
			i.SetActive(false);
		}
	}
	void DestroyObj()
	{
		foreach(GameObject i in destroyObjArr)
		{
			Destroy(i);
		}
	}
    
    //特使使用音效

    public void SetRoleKey(string key)
    {
        roleKey = key;
    }
}
