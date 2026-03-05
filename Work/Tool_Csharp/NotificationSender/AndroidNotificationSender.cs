// #if UNITY_ANDROID
// using SLua;
// using System;
// using System.Collections.Generic;
// using Unity.Notifications.Android;
// using UnityEngine;
// using UnityEngine.Purchasing.MiniJSON;
// [CustomLuaClass]
// public class AndroidNotificationSender : MonoBehaviour
// {
// 	private static bool _isInitialized;
// 	private static List<NotificationInfo> _notificationInfos;
// 	private static int _notificationId = 1;
// 	private static void Init()
// 	{
// 		if (_isInitialized)
// 			return;
// 		_notificationInfos = new List<NotificationInfo>();
// 		ResetNotificationChannel();
// 		var notificationGo = new GameObject("NotificationBehaviour").AddComponent<NotificationSender>();
// 		DontDestroyOnLoad(notificationGo);
//
// 		_isInitialized = true;
// 	}
//
// 	private static void ResetNotificationChannel()
// 	{
// 		_notificationId = 1;
// 		AndroidNotificationCenter.CancelAllNotifications(); //清除上次注册的通知
// 		var channel = new AndroidNotificationChannel
// 		{
// 					Id = "channel_id",
// 					Name = "Default Channel",
// 					Importance = Importance.High,
// 					Description = "Generic notifications",
// 					CanShowBadge = true,
// 					EnableLights = true,
// 					LockScreenVisibility = LockScreenVisibility.Public
// 		};
//
// 		AndroidNotificationCenter.RegisterNotificationChannel(channel);
//
// 	}
//
// 	static protected void ReSendNotification()
// 	{
// 		if (_isInitialized && _notificationInfos != null && _notificationInfos.Count > 0) {
// 			ResetNotificationChannel();
// 			for (int i = 0; i < _notificationInfos.Count; i++) {
// 				SendNotification(_notificationInfos[i]);
// 			}
// 		}
//
// 	}
// 	static public void SendNotification(string notificationJson)
// 	{
// 		var noteInfo = Json.Deserialize(notificationJson) as Dictionary<string,object>;
// 		Debug.Log(noteInfo?["title"]);
// 		Debug.Log(noteInfo?["text"]);
// 		Debug.Log(noteInfo?["day"]);
// 		Debug.Log(noteInfo?["hour"]);
// 		Debug.Log(noteInfo?["minute"]);
// 		Debug.Log(noteInfo?["second"]);
// 		string title = Convert.ToString(noteInfo?["title"]);
// 		string text = Convert.ToString(noteInfo?["text"]);
// 		int day = Convert.ToInt32(noteInfo?["day"]);
// 		int hour = Convert.ToInt32(noteInfo?["hour"]);
// 		int minute = Convert.ToInt32(noteInfo?["minute"]);
// 		int second = Convert.ToInt32(noteInfo?["second"]);
//
// 		SendNotification(title,text,day,hour,minute,second);
// 	}
// 	private static void SendNotification(string title,string text,
// 				int day,int hour,
// 				int minute,int second,
// 				string smallIconId = null,string largeIconId = null)
// 	{
// 		Init();
//
// 		var notificationInfo = new NotificationInfo
// 		{
// 					title = title,
// 					text = text,
// 					day = day,
// 					hour = hour,
// 					minute = minute,
// 					second = second,
// 					smallIcon = smallIconId,
// 					largeIcon = largeIconId
// 		};
//
// 		_notificationInfos.Add(notificationInfo);
// 		SendNotification(notificationInfo);
//
//
// 	}
//
// 	private static void SendNotification(NotificationInfo notificationInfo) //string title, string text,DateTime time,string smallIconId=null,string largeIconId=null)
// 	{
// 		var time = NotificationSender.GetNotificationTime(notificationInfo);
// 		var notification = new AndroidNotification
// 		{
// 					Title = notificationInfo.title,
// 					Text = notificationInfo.text,
// 					FireTime = time,
// 					SmallIcon = notificationInfo.smallIcon,
// 					LargeIcon = notificationInfo.largeIcon,
// 					Number = _notificationId
// 		};
// 		_notificationId++;
// 		AndroidNotificationCenter.SendNotification(notification,"channel_id");
// 	}
// }
// #endif

