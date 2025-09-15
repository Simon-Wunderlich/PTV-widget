using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using PTV_widget;
using PTV_widget.Platforms.Android;
using PTV_widget.Platforms.Android.Resources;
using Android.Util;

namespace Maui.Widgets;

public abstract class Widget : AppWidgetProvider
{

	static Dictionary<RouteType, WidgetInfo> info = new Dictionary<RouteType, WidgetInfo>()
	{
		{ RouteType.Train, new WidgetInfo()},
		{RouteType.Tram, new WidgetInfo()},
		{RouteType.Bus, new WidgetInfo()},
		{RouteType.Vline, new WidgetInfo()},
		{RouteType.NightBus, new WidgetInfo()},
	};
	internal virtual RouteType routeType { get;}

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
			info[routeType].closestStop = await GetClosestStop();
			if (info[routeType].closestStop == null)
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
			info[routeType].currentDepartures = await APIclient.getNextDeparture(info[routeType].closestStop);
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
		catch { }
    }

	private void SetWidgetInfo(RemoteViews views, AppWidgetManager appWidgetManager, int[] appWidgetIds)
	{
		try
		{
			if (info[routeType].currentDepartures.Count == 0)
			{
				views.SetTextViewText(PTV_widget.Resource.Id.routeName, "Not found");
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", 16f);
				views.SetTextViewText(PTV_widget.Resource.Id.stopName, info[routeType].closestStop.stop_name);
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, "--");
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, " mins");
				throw new Exception("Not found");
			}
			if (info[routeType].closestStop.stop_name[..Math.Min(11, info[routeType].closestStop.stop_name.Length)] != info[routeType].stop_name[..Math.Min(11, info[routeType].stop_name.Length)])
				info[routeType].platformNum = 0;

			Departure departure = info[routeType].currentDepartures[info[routeType].platformNum];
			//Update Stop
			if (info[routeType].closestStop.stop_name != info[routeType].stop_name || departure.destination != info[routeType].destination)
			{
				info[routeType].stop_name = info[routeType].closestStop.stop_name;
				info[routeType].destination = departure.destination;
				string output = info[routeType].stop_name + " • " + info[routeType].destination;
				if (info[routeType].stop_name.Length > 13)
					info[routeType].stop_name = info[routeType].stop_name.Substring(0, 11) + "...";
				output = info[routeType].stop_name + " • " + info[routeType].destination;
				if (output.Length > 27)
					output = output.Substring(0, 25) + "...";
				views.SetTextViewText(PTV_widget.Resource.Id.stopName, output);
			}

			//Update route details
			if (departure.RouteName != info[routeType].route_name)
			{
				info[routeType].route_name = departure.RouteName;
				info[routeType].route_color = GetRouteColour(departure.RouteNumber);
				views.SetInt(PTV_widget.Resource.Id.colourStrip, "setBackgroundColor", Android.Graphics.Color.ParseColor(info[routeType].route_color));

			}
				views.SetTextViewText(PTV_widget.Resource.Id.routeName, info[routeType].route_name);

			//Update eta
			string etaStr = " mins";
			if (departure.isAtPlatform)
			{
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, "NOW");
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, "");
			}
			else
			{
				info[routeType].etaMins = calcEta(departure.eta);
				if (info[routeType].etaMins >= 60)
				{
					info[routeType].etaMins /= 60;
					if (info[routeType].etaMins == 1)
						etaStr = " hr";
					else
						etaStr = " hrs";
				}
				else
				{
					if (info[routeType].etaMins == 1)
						etaStr = " min";
					else
						etaStr = " mins";

				}
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, etaStr);
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, info[routeType].etaMins.ToString());


			}

			int availableSpace = 160 - 10 * info[routeType].etaMins.ToString().Length;
			availableSpace += 40 - 10 * etaStr.Length;
			if (departure.isAtPlatform)
				availableSpace = 160;
			if (info[routeType].route_name.Length > 5 && info[routeType].route_name.Length < 11)
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", MathF.Round(availableSpace / info[routeType].route_name.Length));
			}
			else if (info[routeType].route_name.Length >= 11)
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", MathF.Round(availableSpace / 10.5f));
				info[routeType].route_name = info[routeType].route_name.Substring(0, 10) + "...";
			}
			else
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", 32f);
			}
			views.SetTextViewText(PTV_widget.Resource.Id.routeName, info[routeType].route_name);
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
			info[routeType].platformNum++;
			if (info[routeType].platformNum > info[routeType].currentDepartures.Count - 1)
				info[routeType].platformNum = 0;
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
		else if (intent?.Action == "action.PREV_PLATFORM")
		{
			info[routeType].platformNum--;
			if (info[routeType].platformNum < 0)
				info[routeType].platformNum = info[routeType].currentDepartures.Count - 1;
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
	}

    internal async Task<Stop?> GetClosestStop()
    {
		Location loc = await GetCurrentLocation();
		return await APIclient.getClosestStop(loc.Longitude, loc.Latitude, routeType);
	}

	internal abstract string GetRouteColour(int routeNum);


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