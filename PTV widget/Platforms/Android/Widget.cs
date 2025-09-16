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
	// Define info to display on widget for each route type
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

		// Define button press actions
		views.SetOnClickPendingIntent(PTV_widget.Resource.Id.update, GetPendingSelfIntent(context, "action.UPDATE"));
		views.SetOnClickPendingIntent(PTV_widget.Resource.Id.nextPlatform, GetPendingSelfIntent(context, "action.NEXT_PLATFORM"));
		views.SetOnClickPendingIntent(PTV_widget.Resource.Id.prevPlatform, GetPendingSelfIntent(context, "action.PREV_PLATFORM"));
		
		// Show loading circle
		views.SetViewVisibility(PTV_widget.Resource.Id.indeterminateBar, Android.Views.ViewStates.Visible);

		// Update UI
		foreach (var appWidgetId in appWidgetIds)
		{
			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}
		
		try
		{
			// Gets location and finds closestStop of type routeType
			info[routeType].closestStop = await GetClosestStop();

			// If null, set to error state
			if (info[routeType].closestStop == null)
			{
				info[routeType].stop_name = ""
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
				// Breaks out of try
				throw new Exception("No stops found");
			}
			
			// Get next unique departure for stop
			info[routeType].currentDepartures = await APIclient.getNextDeparture(info[routeType].closestStop);

			// Refresh UI
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
		catch { }
    }

	private void SetWidgetInfo(RemoteViews views, AppWidgetManager appWidgetManager, int[] appWidgetIds)
	{
		try
		{
			// If no departures at stop, error state
			if (info[routeType].currentDepartures.Count == 0)
			{
				views.SetTextViewText(PTV_widget.Resource.Id.routeName, "Not found");
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", 16f);
				views.SetTextViewText(PTV_widget.Resource.Id.stopName, info[routeType].closestStop.stop_name);
				views.SetTextViewText(PTV_widget.Resource.Id.minsNum, "--");
				views.SetTextViewText(PTV_widget.Resource.Id.minsText, " mins");
				throw new Exception("Not found");
			}
			// Check if stop name has changed, if true: reset to display first departure 
			if (info[routeType].closestStop.stop_name[..Math.Min(11, info[routeType].closestStop.stop_name.Length)] != info[routeType].stop_name[..Math.Min(11, info[routeType].stop_name.Length)])
				info[routeType].platformNum = 0;


			Departure departure = info[routeType].currentDepartures[info[routeType].platformNum];

			//Update Stop if details have changed
			if (info[routeType].closestStop.stop_name != info[routeType].stop_name || departure.destination != info[routeType].destination)
			{
				info[routeType].stop_name = info[routeType].closestStop.stop_name;
				info[routeType].destination = departure.destination;

				// Combines stop name and destination
				string output = info[routeType].stop_name + " • " + info[routeType].destination;

				// Checks length of stop name to prevent overflow
				if (info[routeType].stop_name.Length > 13) {
					info[routeType].stop_name = info[routeType].stop_name.Substring(0, 11) + "...";
					// Update output with shortened stop name
					output = info[routeType].stop_name + " • " + info[routeType].destination;
				}
				//If output still overflows, shorten whole string
				if (output.Length > 27)
					output = output.Substring(0, 25) + "...";
				views.SetTextViewText(PTV_widget.Resource.Id.stopName, output);
			}

			//Update route details if details have changed
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
				// Get num mins until arrival
				info[routeType].etaMins = calcEta(departure.eta);

				// If eta over an hour, display hrs instead of mins
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
			
			// Calculate amount of space remaining for route name
			int availableSpace = 160 - 10 * info[routeType].etaMins.ToString().Length;
			availableSpace += 40 - 10 * etaStr.Length;
			if (departure.isAtPlatform)
				availableSpace = 160;

			// If name longer than 5 and less than 11 chars long, scale font size to length
			if (info[routeType].route_name.Length > 5 && info[routeType].route_name.Length < 11)
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", MathF.Round(availableSpace / info[routeType].route_name.Length));
			}
			// Shorten string if over 11 chars long
			else if (info[routeType].route_name.Length >= 11)
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", MathF.Round(availableSpace / 10.5f));
				info[routeType].route_name = info[routeType].route_name.Substring(0, 10) + "...";
			}
			// If name <= 5 chars long, use default font size
			else
			{
				views.SetFloat(PTV_widget.Resource.Id.routeName, "setTextSize", 32f);
			}
			views.SetTextViewText(PTV_widget.Resource.Id.routeName, info[routeType].route_name);
		}
		catch { }

		// Hide loading circle
		views.SetViewVisibility(PTV_widget.Resource.Id.indeterminateBar, Android.Views.ViewStates.Invisible);

		// Update UI
		foreach (var appWidgetId in appWidgetIds)
		{
			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}
	}
	
	// Assign action to clickable element
	private PendingIntent GetPendingSelfIntent(Context context, String action)
	{

		Intent intent = new Intent(context, this.Class);
		intent.SetAction(action);
		return PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.Mutable);
	}

	// Handle button press
	public override void OnReceive(Context? context, Intent? intent)
	{
        	base.OnReceive(context, intent);

		// Get variables needed to call OnUpdate
		var views = new RemoteViews(context.PackageName, Microsoft.Maui.Resource.Layout.widget);
		AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
		ComponentName thisAppWidgetComponentName = new ComponentName(context.PackageName, this.Class.Name);
		int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(thisAppWidgetComponentName);

		// If middle button (UPDATE) pressed, update widget
		if (intent?.Action == "action.UPDATE")
		{
			OnUpdate(context, appWidgetManager, appWidgetIds);
		}

		// If right button (NEXT_PLATFORM) pressed, increment platformNum by 1, (overflowing to 0)
		else if (intent?.Action == "action.NEXT_PLATFORM")
		{
			info[routeType].platformNum++;
			// Overflow check
			if (info[routeType].platformNum > info[routeType].currentDepartures.Count - 1)
				info[routeType].platformNum = 0;
			// Refresh UI
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
		
		// If left button (PREV_PLATFORM) pressed, decrement platformNum by 1, (underflowing to final departure)
		else if (intent?.Action == "action.PREV_PLATFORM")
		{
			info[routeType].platformNum--;
			// Underflow check
			if (info[routeType].platformNum < 0)
				info[routeType].platformNum = info[routeType].currentDepartures.Count - 1;
			// Refresh UI
			SetWidgetInfo(views, appWidgetManager, appWidgetIds);
		}
	}

    	internal async Task<Stop?> GetClosestStop()
    	{
		// Get current pos as Location obj
		Location loc = await GetCurrentLocation();

		// Returns closest stop as Stop obj
		// If none found, returns null
		return await APIclient.getClosestStop(loc.Longitude, loc.Latitude, routeType);
	}
	
	// Implemented by childeren
	internal abstract string GetRouteColour(int routeNum);

	// Calculate num mins until arrival
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
