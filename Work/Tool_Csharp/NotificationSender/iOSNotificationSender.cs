// #if UNITY_IOS
// using SLua;
// using System;
// using System.Collections.Generic;
// using Unity.Notifications.iOS;
// using UnityEngine;
// using UnityEngine.Purchasing.MiniJSON;
// [CustomLuaClass]
// public class iOSNotificationSender : MonoBehaviour
// {
// 	private static bool _isInitialized;
// 	private static int _notificationId = 1;
// 	private static List<NotificationInfo> _notificationInfos;
// 	// Start is called before the first frame update
// 	private static void Init()
// 	{
// 		if (_isInitialized)
// 			return;
// 		_notificationInfos = new List<NotificationInfo>();
// 		ResetNotificationChannel();
// 		var notificationGo = new GameObject("NotificationBehaviour").AddComponent<NotificationSender>();
// 		DontDestroyOnLoad(notificationGo);
// 		_isInitialized = true;
// 	}
// 	private static void ResetNotificationChannel()
// 	{
// 		_notificationId = 1;
// 		iOSNotificationCenter.ApplicationBadge = 0;
// 		iOSNotificationCenter.RemoveAllDeliveredNotifications();
// 		iOSNotificationCenter.RemoveAllScheduledNotifications();
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
// 		Debug.Log("noteInfo:" + noteInfo);
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
// 				int minute,int second)
// 	{
// 		Init();
//
//
// 		var notificationInfo = new NotificationInfo
// 		{
// 					title = title,
// 					text = text,
// 					day = day,
// 					hour = hour,
// 					minute = minute,
// 					second = second
// 		};
// 		_notificationInfos.Add(notificationInfo);
//
// 		SendNotification(notificationInfo);
//
// 	}
//
//
// 	private static void SendNotification(NotificationInfo notificationInfo) //string title, string text,TimeSpan timeInterval)
// 	{
//
// 		var time = NotificationSender.GetNotificationTime(notificationInfo);
// 		var timeInterval = time.Subtract(DateTime.Now);
//
// 		var timeTrigger = new iOSNotificationTimeIntervalTrigger
// 		{
// 					TimeInterval = new TimeSpan(timeInterval.Days,timeInterval.Hours,timeInterval.Minutes,timeInterval.Seconds), // timeInterval,
// //            TimeInterval = new TimeSpan(0,0,0,5),// timeInterval,
// 					Repeats = false
// 		};
//
// 		var notification = new iOSNotification
// 		{
// 					// You can optionally specify a custom identifier which can later be 
// 					// used to cancel the notification, if you don't set one, a unique 
// 					// string will be generated automatically.
// 					Identifier = "_notification_" + _notificationId,
// 					Title = notificationInfo.title,
// 					Body = notificationInfo.text,
// 					Badge = _notificationId,
// 					ShowInForeground = true,
// 					ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge,
// 					CategoryIdentifier = "category_a",
// 					ThreadIdentifier = "thread1",
// 					Trigger = timeTrigger
// 		};
// 		_notificationId++;
// 		iOSNotificationCenter.ScheduleNotification(notification);
// 	}
// }
// #endif

