using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace CompiegneBus
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class BusDetail : Page
    {
        private uint direction;
        private string stop;
        private string line;

        public BusDetail()
        {
            this.InitializeComponent();
        }

        public ObservableCollection<string> times { get; set; } = new ObservableCollection<string>();

        async private Task GetBusTime(string s, string l, uint d)
        {
            //Create an HTTP client object
            HttpClient httpClient = new HttpClient();

            //Add a user-agent header to the GET request. 
            var headers = httpClient.DefaultRequestHeaders;

            Uri requestUri = new Uri("http://66.42.32.248/wechat/api/get_with_bus_stop.php");


            List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>();
            formData.Add(new KeyValuePair<string, string>("stop", s));
            formData.Add(new KeyValuePair<string, string>("line", l));
            formData.Add(new KeyValuePair<string, string>("direction", d + ""));
            HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(formData);

            try
            {
                //Send the GET request asynchronously and retrieve the response as a string.
                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";
                // Send the POST request
                httpResponse = await httpClient.PostAsync(requestUri, content);
                httpResponse.EnsureSuccessStatusCode();

                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine(httpResponseBody);

                JsonObject jsonObject = JsonObject.Parse(httpResponseBody);
                // Parse state

                // Parse datas
                JsonArray data = jsonObject["data"].GetArray();

                times.Clear();

                for (uint i = 0; i < data.Count; i++)
                {
                    // Parse stop
                    string time = data.GetStringAt(i);

                    if (time == null) continue;

                    times.Add(time);

                    // times.Add(new DateTime();
                }

                loadingProgressRing.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Refresh error
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            BusStopName receiveData = (BusStopName)e.Parameter;

            if (receiveData != null)
            {
                line = receiveData.Line;
                stop = receiveData.StopName;
                direction = receiveData.Direction;

                title.Content = "Line " + line + " " + Line.NAME[(int.Parse(line) - 1) * 2 + (direction - 1)];

                GetBusTime(stop, line, direction);  // Async refresh
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }
    }
}
