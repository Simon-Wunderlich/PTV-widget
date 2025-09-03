using Newtonsoft.Json;

namespace PTV_widget
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
		public Dictionary<string, string> directions = new Dictionary<string, string>();

		public MainPage()
        {
            InitializeComponent();

            Task.Run(async () =>
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }
            });
		}
	}
}
