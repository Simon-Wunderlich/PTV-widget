using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using PTV_widget;
using PTV_widget.Platforms.Android;
using PTV_widget.Platforms.Android.Resources;
using Android.Util;

namespace Maui.Widgets;

[BroadcastReceiver(Label = "Bus Departures", Exported = true)]
[IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget")]
[Service(Exported = true)]

public class BugWidget : Widget
{
	internal override RouteType routeType => RouteType.Bus;

	internal override string GetRouteColour(int routeNum)
	{
		return "#ff8000";
	}

}