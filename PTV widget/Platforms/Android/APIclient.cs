using Newtonsoft.Json.Linq;
using PTV_widget.Platforms.Android.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace PTV_widget.Platforms.Android
{
	public class APIclient
	{
		// Calculates signature based on DEV_ID and DEV_KEY
		// Formats url with signature
		private static string addCredentials(string url)
		{
			// add developer id
			url = string.Format("{0}{1}devid={2}", url, url.Contains("?") ? "&" : "?", SECRETS.DEV_ID);
			ASCIIEncoding encoding = new ASCIIEncoding();
			// encode key
			byte[] keyBytes = encoding.GetBytes(SECRETS.DEV_KEY);
			// encode url
			byte[] urlBytes = encoding.GetBytes(url);
			byte[] tokenBytes = new System.Security.Cryptography.HMACSHA1(keyBytes).ComputeHash(urlBytes);
			var sb = new StringBuilder();
			// convert signature to string
			Array.ForEach<byte>(tokenBytes, x => sb.Append(x.ToString("X2")));
			// add signature to url
			url = string.Format("{0}&signature={1}", url, sb.ToString());
			return url;
		}

		// Gets dictionary associating route number and route name
		// Saves extra api call per route
		internal static Dictionary<int, string> parseRoutes(JToken json)
		{
			Dictionary<int, string> routes = new Dictionary<int, string>();
			foreach (var route in json["routes"])
			{
				int route_num = int.Parse(route["route_id"].ToString());
				// Skip if not distinct
				if (routes.ContainsKey(route_num))
					continue;
				string route_name;
				// If train, use route name
				if (route["route_type"].ToString() == "0")
					route_name = route["route_name"].ToString();
				// Shorten VLine route names
				else if (route["route_type"].ToString() == "3")
				{
					route_name = route["route_name"].ToString();
					route_name = Regex.Replace(route_name, " via(.+?)", "fast");
				}
				// If bus, nightbus, or tram, use route_number
				else
					route_name = route["route_number"].ToString();
				
				routes.Add(route_num, route_name);
			}
			return routes;
		}
		// Get stop of a given route type closest to _long and _lat
		internal async static Task<Stop?> getClosestStop(double _long, double _lat, RouteType route_type)
		{
			// Call PTV API endpoint /v3/stops/location/{latitude},{longitude}
			using var client = new HttpClient(new Xamarin.Android.Net.AndroidMessageHandler());
			client.BaseAddress = new Uri("https://timetableapi.ptv.vic.gov.au" + addCredentials($"/v3/stops/location/{_lat},{_long}?max_results=1&route_types={(int)route_type}&max_distance=1000"));
			var response = await client.GetAsync(client.BaseAddress);

			// Parse JSON
			string jsonString = await response.Content.ReadAsStringAsync();
			JObject respObj = JObject.Parse(jsonString);

			// Existence check
			if (respObj["stops"].Count() == 0)
				return null;

			// Parse id and name
			int id = int.Parse(respObj["stops"].First()["stop_id"].ToString());
			string name = respObj["stops"].First()["stop_name"].ToString();

			// If tram stop, store only stop number as stop name
			if (route_type == RouteType.Tram)
				name = "Stop #" + Regex.Replace(name, "(.+?)#", "");
			// If bus-ish stop, only keep unique part of name
			// Original: {side street} / {main street}
			// Formatted: {side street}
			else if ((RouteType)route_type == RouteType.Bus || (RouteType)route_type == RouteType.NightBus)
				name = Regex.Replace(name, "/(.+?)$", "");

			// Gets route name for each route number
			Dictionary<int, string> routes = parseRoutes(respObj["stops"].First());

			return new Stop(name, id, route_type, routes);
		}

		// Get list of next distinct departures at a given stop
		internal async static Task<List<Departure>> getNextDeparture(Stop stop)
		{
			try
			{
				List<Departure> departures = new List<Departure>();
				List<string> platforms = new List<string>();

				// Call PTV API endpoint /v3/departures/route_type/{route_type}/stop/{stop_id}
				// Expand argument gives more detailed object, needed to find direction names
				using var client = new HttpClient(new Xamarin.Android.Net.AndroidMessageHandler());
				client.BaseAddress = new Uri("https://timetableapi.ptv.vic.gov.au" + addCredentials($"/v3/departures/route_type/{(int)stop.route_type}/stop/{stop.stop_id}?max_results=1&expand=0"));
				var response = await client.GetAsync(client.BaseAddress);

				//Parse JSON
				string jsonString = await response.Content.ReadAsStringAsync();
				JObject respObj = JObject.Parse(jsonString);

				int route_num;
				string route_name;
				DateTime eta;
				bool isAtPlatform;
				string dest;

				// Parse each departure object
				foreach (var dep in respObj["departures"])
				{
					// Check distinct direction
					if (platforms.Contains(dep["direction_id"].ToString()))
						continue;


					route_num = int.Parse(dep["route_id"].ToString());

					// Check distinct route
					if (!stop.routes.ContainsKey(route_num))
						continue;

					// Parse name, eta datetime UTC format, is at_platform
					route_name = stop.routes[route_num];
					string etaStr = dep["estimated_departure_utc"].ToString();
					if (etaStr == "")
						etaStr = dep["scheduled_departure_utc"].ToString();
					eta = DateTime.Parse(etaStr).ToLocalTime();
					isAtPlatform = bool.Parse(dep["at_platform"].ToString());

					// Parse direction num and use to find destination name 
					string dirNum = dep["direction_id"].ToString();
					var destObj = respObj["directions"][dirNum];
					dest = destObj["direction_name"].ToString();

					platforms.Add(dep["direction_id"].ToString());
					departures.Add(new Departure(route_name, route_num, eta, isAtPlatform, dest));
				}
				return departures;
			}
			catch (Exception e)
			{

				throw;
			}


		}
	}
}
