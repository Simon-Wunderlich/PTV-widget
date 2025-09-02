using Android.Content;
using Android.Content.Res;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PTV_widget.Platforms.Android.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PTV_widget.Platforms.Android
{
	public class APIclient
	{
		private static string addCredentials(string url)
		{

			// add developer id
			url = string.Format("{0}{1}devid={2}", url, url.Contains("?") ? "&" : "?", SECRETS.DEV_ID);
			System.Text.ASCIIEncoding encoding = new ASCIIEncoding();
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

		internal static Dictionary<int, string> parseRoutes(JToken json)
		{
			Dictionary<int, string> routes = new Dictionary<int, string>();
			foreach (var route in json["routes"])
			{
				int route_num = int.Parse(route["route_id"].ToString());
				string route_name;
				if (route["route_type"].ToString() == "0")
					route_name = route["route_name"].ToString();
				else if (route["route_type"].ToString() == "3")
				{
					route_name = route["route_name"].ToString();
					route_name = Regex.Replace(route_name, " via(.+?)", "fast");
				}
					route_name = route["route_number"].ToString();

				routes.Add(route_num, route_name);
			}
			return routes;
		}

		internal static Stop getClosestStop(double _long, double _lat)
		{
			using var client = new HttpClient();
			client.BaseAddress = new Uri("https://timetableapi.ptv.vic.gov.au" + addCredentials($"/v3/stops/location/{_lat},{_long}?max_results=1"));
			var response = client.GetAsync(client.BaseAddress).Result;
			string jsonString = response.Content.ReadAsStringAsync().Result;
			JObject respObj = JObject.Parse(jsonString);
			string name = respObj["stops"].First()["stop_name"].ToString();
			int id = int.Parse(respObj["stops"].First()["stop_id"].ToString());
			int type = int.Parse(respObj["stops"].First()["route_type"].ToString());

			Dictionary<int, string> routes = parseRoutes(respObj["stops"].First());

			return new Stop(name, id, (RouteType)type, routes);
		}

		internal static Departure getNextDeparture(Stop stop)
		{
			using var client = new HttpClient();
			client.BaseAddress = new Uri("https://timetableapi.ptv.vic.gov.au" + addCredentials($"/v3/departures/route_type/{(int)stop.route_type}/stop/{stop.stop_id}?max_results=1"));
			var response = client.GetAsync(client.BaseAddress).Result;
			string jsonString = response.Content.ReadAsStringAsync().Result;
			JObject respObj = JObject.Parse(jsonString);
			int route_num = int.Parse(respObj["departures"].First()["route_id"].ToString());
			string route_name = stop.routes[route_num];
			DateTime eta = DateTime.Parse(respObj["departures"].First()["estimated_departure_utc"].ToString()).ToLocalTime();
			bool isAtPlatform = bool.Parse(respObj["departures"].First()["at_platform"].ToString());
			return new Departure(route_name, route_num, eta, isAtPlatform);
		}
	}
}
