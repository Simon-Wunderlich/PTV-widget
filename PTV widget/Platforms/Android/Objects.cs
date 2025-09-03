using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTV_widget.Platforms.Android.Resources
{
	record Departure(string RouteName, int RouteNumber, DateTime eta, bool isAtPlatform, string destination);
	internal record Stop(string stop_name, int stop_id, RouteType route_type, Dictionary<int, string> routes);
	internal enum RouteType
	{
		Train = 0,
		Tram = 1,
		Bus = 2,
		Vline = 3,
		NightBus = 4
	}
}
