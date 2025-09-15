using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using PTV_widget;
using PTV_widget.Platforms.Android;
using PTV_widget.Platforms.Android.Resources;
using Android.Util;

namespace Maui.Widgets;

[BroadcastReceiver(Label = "Train Departures", Exported = true)]
[IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget")]
[Service(Exported = true)]

public class TrainWidget : Widget
{
	internal override RouteType routeType => RouteType.Train;

	internal override string GetRouteColour(int routeNum)
	{
		if ((new[] { 1, 2, 7, 9 }).Contains(routeNum))
			return "#b8bf2e";
		else if ((new[] { 3, 14, 15 }).Contains(routeNum))
			return "#ffbe00";
		else if ((new[] { 4, 11 }).Contains(routeNum))
			return "#279fd5";
		else if ((new[] { 5, 8 }).Contains(routeNum))
			return "#be1014";
		else if ((new[] { 6, 13, 16, 17 }).Contains(routeNum))
			return "#3d8825";
		else if (routeNum == 721)
			return "#65baf7";
		else
			return "#000000";
	}
}