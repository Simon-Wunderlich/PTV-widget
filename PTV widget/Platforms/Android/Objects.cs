using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTV_widget.Platforms.Android.Resources
{
	public record Departure(string RouteName, int RouteNumber, DateTime eta, bool isAtPlatform, string destination);
	public record Stop(string stop_name, int stop_id, RouteType route_type, Dictionary<int, string> routes);
	public enum RouteType
	{
		Train = 0,
		Tram = 1,
		Bus = 2,
		Vline = 3,
		NightBus = 4
	}
	public class WidgetInfo
	{
		public string route_name = "";
		public string stop_name = "";
		public string route_color = "#000";
		public int etaMins = -1;
		public int platformNum = 0;
		public string destination = "";
		public List<Departure> currentDepartures = new List<Departure>();
		public Stop? closestStop;
	}
}
