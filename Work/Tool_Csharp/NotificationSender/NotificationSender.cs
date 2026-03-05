// using System;
// using UnityEngine;
// public class NotificationInfo
// {
// 	public int day;
// 	public int hour;
// 	public string largeIcon;
// 	public int minute;
// 	public int second;
// 	public string smallIcon;
// 	public string text;
// 	public string title;
// }
//
// public class NotificationSender :
// #if UNITY_ANDROID
// 		AndroidNotificationSender
// #else
// 			iOSNotificationSender
// #endif
//
// {
// 	private void OnApplicationFocus(bool hasFocus)
// 	{
// 		if (hasFocus)
// 			ReSendNotification();
// 	}
//
//
//
// 	/// <summary>
// 	///     得到注册通知的时间
// 	/// </summary>
// 	/// <returns></returns>
// 	static public DateTime GetNotificationTime(NotificationInfo notificationInfo)
// 	{
// 		var daySpan = new TimeSpan(notificationInfo.day,0,0,0);
// 		var dateTime = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,notificationInfo.hour,notificationInfo.minute,notificationInfo.second);
// 		dateTime += daySpan;
// 		Debug.Log("RegisterNotification:" + dateTime);
// 		return dateTime;
//
// 	}
// }

