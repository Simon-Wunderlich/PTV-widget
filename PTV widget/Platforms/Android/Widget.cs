using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using PTV_widget;
using PTV_widget.Platforms.Android;
using PTV_widget.Platforms.Android.Resources;
using Android.Util;

namespace Maui.Widgets;

[BroadcastReceiver(Label = "PTV Departures", Exported = true)]
[IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget")]
[Service(Exported = true)]

public class Widget : AppWidgetProvider
{
    static string route_name = "";
    static string stop_name = "";
    static string route_color = "#000";
    static int etaMins = -1;
	static int platformNum = 0;
	static string destination = "";
	static List<Departure> currentDepartures = new List<Departure>();
	static Stop? closestStop;

	public override async void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var views = new RemoteViews(context.PackageName, Microsoft.Maui.Resource.Layout.widget);

		views.SetOnClickPendingIntent(PTV_widget.Resource.Id.update, GetPendingSelfIntent(context, "action.UPDATE"));
		views.SetOnClickPendingIntent(PTV_widget.Resource.Id.nextPlatform, GetPendingSelfIntent(context, "action.NEXT_PLATFORM"));
		views.SetOnClickPendingIntent(PTV_widget.Resource.Id.prevPlatform, GetPendingSelfIntent(context, "action.PREV_PLATFORM"));

		views.SetViewVisibility(PTV_widget.Resource.Id.indeterminateBar, Android.Views.ViewStates.Visible);
		foreach (var appWidgetId in appWidgetIds)
		{
			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}
		
		try
		{
			closestStop = await GetClosestStop();
			if (closestStop == null)
			{
				views.SetTextViewText(PTV_widget.Resource.Id.routeName, "Not found");
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", 16f);
				views.SetTextViewText(PTV_widget.Resource.Id.stopName, "Not found");
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, "--");
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, " mins");
				views.SetViewVisibility(PTV_widget.Resource.Id.indeterminateBar, Android.Views.ViewStates.Invisible);
				foreach (var appWidgetId in appWidgetIds)
				{
					appWidgetManager.UpdateAppWidget(appWidgetId, views);
				}
				throw new Exception("No stops found");
			}
			currentDepartures = await APIclient.getNextDeparture(closestStop);
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
		catch { }
    }

	private void SetWidgetInfo(RemoteViews views, AppWidgetManager appWidgetManager, int[] appWidgetIds)
	{
		try
		{
			if (currentDepartures.Count == 0)
			{
				views.SetTextViewText(PTV_widget.Resource.Id.routeName, "Not found");
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", 16f);
				views.SetTextViewText(PTV_widget.Resource.Id.stopName, closestStop.stop_name);
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, "--");
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, " mins");
				throw new Exception("Not found");
			}
			if (closestStop.stop_name != stop_name)
				platformNum = 0;

			Departure departure = currentDepartures[platformNum];
			//Update Stop
			if (closestStop.stop_name != stop_name || departure.destination != destination)
			{
				stop_name = closestStop.stop_name;
				destination = departure.destination;
				string output = stop_name + " • " + destination;
				if (stop_name.Length > 13)
					stop_name = stop_name.Substring(0, 11) + "...";
				output = stop_name + " • " + destination;
				if (output.Length > 27)
					output = output.Substring(0, 25) + "...";
				views.SetTextViewText(PTV_widget.Resource.Id.stopName, output);
			}

			//Update route details
			if (departure.RouteName != route_name)
			{
				route_name = departure.RouteName;
				route_color = GetRouteColour(departure.RouteNumber, closestStop.route_type);
				views.SetInt(PTV_widget.Resource.Id.colourStrip, "setBackgroundColor", Android.Graphics.Color.ParseColor(route_color));

				views.SetTextViewText(PTV_widget.Resource.Id.routeName, route_name);
			}

			//Update eta
			string etaStr = " mins";
			if (departure.isAtPlatform)
			{
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, "NOW");
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, "");
			}
			else
			{
				etaMins = calcEta(departure.eta);
				if (etaMins >= 60)
				{
					etaMins /= 60;
					if (etaMins == 1)
						etaStr = " hr";
					else
						etaStr = " hrs";
				}
				else
				{
					if (etaMins == 1)
						etaStr = " min";
					else
						etaStr = " mins";

				}
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, etaStr);
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, etaMins.ToString());


			}

			int availableSpace = 170 - 10 * etaMins.ToString().Length;
			availableSpace += 40 - 10 * etaStr.Length;
			if (departure.isAtPlatform)
				availableSpace = 160;
			if (route_name.Length > 5 && route_name.Length < 14)
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", MathF.Round(availableSpace / route_name.Length));
			}
			else if (route_name.Length >= 14)
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", MathF.Round(availableSpace / 14));
				route_name = route_name.Substring(0, 13) + "...";
			}
			else
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", 32f);
			}
		}
		catch { }
		views.SetViewVisibility(PTV_widget.Resource.Id.indeterminateBar, Android.Views.ViewStates.Invisible);
		foreach (var appWidgetId in appWidgetIds)
		{
			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}
	}

	private PendingIntent GetPendingSelfIntent(Context context, String action)
	{

		Intent intent = new Intent(context, this.Class);
		intent.SetAction(action);
		return PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.Mutable);
	}

	public override void OnReceive(Context? context, Intent? intent)
	{
        base.OnReceive(context, intent);
		var views = new RemoteViews(context.PackageName, Microsoft.Maui.Resource.Layout.widget);
		AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
		ComponentName thisAppWidgetComponentName = new ComponentName(context.PackageName, this.Class.Name);
		int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(thisAppWidgetComponentName);
		if (intent?.Action == "action.UPDATE")
		{
			OnUpdate(context, appWidgetManager, appWidgetIds);
		}
		else if (intent?.Action == "action.NEXT_PLATFORM")
		{
			platformNum++;
			if (platformNum > currentDepartures.Count - 1)
				platformNum = 0;
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
		else if (intent?.Action == "action.PREV_PLATFORM")
		{
			platformNum--;
			if (platformNum < 0)
				platformNum = currentDepartures.Count - 1;
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
	}

    internal async Task<Stop?> GetClosestStop()
    {
		Location loc = await GetCurrentLocation();
		return await APIclient.getClosestStop(loc.Longitude, loc.Latitude);
	}

    internal string GetRouteColour(int routeNum, RouteType type)
    {
		switch(type)
		{
			case RouteType.Train:
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
			case RouteType.Tram:
				switch (routeNum)
				{
					case 721:return "#b5c525";
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
			case RouteType.Bus:
				return "#ff8000";
			case RouteType.Vline:
				return "#8f1a95";
			case RouteType.NightBus:
				return "#ff8200";
			default:
				return "#000000";
		}
	}

    internal int calcEta(DateTime dt)
    {
		return (int)MathF.Ceiling((float)dt.Subtract(DateTime.Now).TotalSeconds/60f);
	}
	public async Task<Location> GetCurrentLocation()
	{
		try
		{
			// Get the location
			Location location = await Geolocation.Default.GetLastKnownLocationAsync();

			if (location != null)
			{
				// Location obtained
				return location;
			}
			else
			{
				// Location not available
				return null;
			}
		}
		catch (Exception ex)
		{
			return null;
		}
	}
}