using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using PTV_widget;
using PTV_widget.Platforms.Android;
using PTV_widget.Platforms.Android.Resources;
using Android.Util;

namespace Maui.Widgets;

[BroadcastReceiver(Label = "Tram Departures", Exported = true)]
[IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget")]
[Service(Exported = true)]

public class TramWidget : Widget
{
	internal override RouteType routeType => RouteType.Tram;

	internal override string GetRouteColour(int routeNum)
	{
		switch (routeNum)
		{
			case 721: return "#b5c525";
			case 722: return "#f27f25";
			case 724: return "#fdd962";
			case 725: return "#8a4c74";
			case 887: return "#34bccc";
			case 897: return "#498057";
			case 909: return "#05a76e";
			case 913: return "#af7964";
			case 940: return "#eb8cb7";
			case 947: return "#99b5a6";
			case 958: return "#079bd5";
			case 976: return "#877bbd";
			case 1002: return "#bcd433";
			case 1041: return "#db397f";
			case 1083: return "#e33f38";
			case 1880: return "#4f48a3";
			case 1881: return "#fbba11";
			case 2903: return "#424244";
			case 3343: return "#87c3a1";
			case 8314: return "#028692";
			case 11529: return "#7f868c";
			case 11544: return "#004d6c";
			case 15833: return "#7fd3f1";
			case 15834: return "#743718";
			default: return "#000000";
		}
	}
}