using Microsoft.Maui.Devices.Sensors;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using PTV_widget.Platforms.Android.Resources;
using System.ComponentModel.Design;

namespace Maui.Widgets;

[BroadcastReceiver(Label = "PTV Departures", Exported = true)]
[IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget")]
[Service(Exported = true)]
public class Widget : AppWidgetProvider
{
    string route_name = "";
    string stop_name = "";
    string route_color = "#000";
    int etaMins = -1;

	public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var views = new RemoteViews(context.PackageName, Microsoft.Maui.Resource.Layout.widget);

        views.SetOnClickPendingIntent(Microsoft.Maui.Resource.Id.layout_2_1, GetPendingSelfIntent(context, "action.UPDATE"));


        Stop closestStop = GetClosestStop();
        Departure departure = GetStopDeparture(closestStop);

        //Update Stop
        if (closestStop.stop_name != stop_name)
        {
            stop_name = closestStop.stop_name;

			if (stop_name.Length > 24)
				stop_name = stop_name.Substring(0, 22) + "...";

			views.SetTextViewText(Microsoft.Maui.Resource.Id.stopName, stop_name);
        }

        //Update route details
        if (departure.RouteName != route_name)
        {
            route_name = departure.RouteName;
            route_color = GetRouteColour(departure.RouteNumber);
            views.SetInt(Microsoft.Maui.Resource.Id.colourStrip, "setBackgroundColor", Android.Graphics.Color.ParseColor(route_color));


			if (route_name.Length > 5 && route_name.Length < 14)
			{
				views.SetFloat(Microsoft.Maui.Resource.Id.routeName, "setTextSize", MathF.Round(160 / route_name.Length));
			}
			else if (route_name.Length >= 14)
			{
				views.SetFloat(Microsoft.Maui.Resource.Id.routeName, "setTextSize", MathF.Round(160 / 14));
				route_name = route_name.Substring(0,13) + "...";
			}

			views.SetTextViewText(Microsoft.Maui.Resource.Id.routeName, route_name);
        }

        //Update eta
        if (departure.isAtPlatform)
        {
            views.SetTextViewText(Microsoft.Maui.Resource.Id.minsText, "NOW");
            views.SetTextViewText(Microsoft.Maui.Resource.Id.minsNum, "");
        }
        else
        {
            etaMins = calcEta(departure.eta);
            views.SetTextViewText(Microsoft.Maui.Resource.Id.minsNum, etaMins.ToString());
            if (etaMins == 1)
                views.SetTextViewText(Microsoft.Maui.Resource.Id.minsText, " min");
            else
                views.SetTextViewText(Microsoft.Maui.Resource.Id.minsText, " mins");
        }


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

        if (intent?.Action == "action.UPDATE")
		{
			AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
			ComponentName thisAppWidgetComponentName = new ComponentName(context.PackageName, this.Class.Name);
			int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(thisAppWidgetComponentName);
			OnUpdate(context, appWidgetManager, appWidgetIds);
		}
	}

    internal Stop GetClosestStop()
    {
		Location loc = GetCurrentLocation().Result;

		return new Stop("Undef", -1, 0);
	}

    internal Departure GetStopDeparture(Stop stop)
    {
        return new Departure("Undef", -1, DateTime.UtcNow,  false);
	}

    internal string GetRouteColour(int routeNum)
    {
        return "#000000";
	}
    internal int calcEta(DateTime dt)
    {
		return DateTime.UtcNow.Subtract(dt).Minutes;
	}
	public async Task<Location> GetCurrentLocation()
	{
		try
		{
			// Check and request permissions if needed
			var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
			if (status != PermissionStatus.Granted)
			{
				status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
				if (status != PermissionStatus.Granted)
				{
					// Handle permission denied
					return null;
				}
			}

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
		catch (FeatureNotSupportedException fnsEx)
		{
			// Handle not supported on device
			Console.WriteLine($"Location not supported on this device: {fnsEx.Message}");
			return null;
		}
		catch (FeatureNotEnabledException fneEx)
		{
			// Handle not enabled in device settings
			Console.WriteLine($"Location not enabled in settings: {fneEx.Message}");
			return null;
		}
		catch (PermissionException pEx)
		{
			// Handle permission denied
			Console.WriteLine($"Location permission denied: {pEx.Message}");
			return null;
		}
		catch (Exception ex)
		{
			// Handle other exceptions
			Console.WriteLine($"An error occurred: {ex.Message}");
			return null;
		}
	}
}