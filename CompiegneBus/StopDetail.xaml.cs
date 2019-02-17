using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
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
    public sealed partial class StopDetail : Page
    {
        private string stop;
        private int id;

        private double latitude;
        private double longitude;

        public StopDetail()
        {
            this.InitializeComponent();
        }

        private bool isLoading = false;
        public ObservableCollection<BusStopLineTime> StopLine { get; set; } = new ObservableCollection<BusStopLineTime>();
        async private Task GetStopBusTimesAsync(int id, int limit=3)
        {
            // https://jp.inoki.cc/wechat/api/stop_all.php

            if (!isLoading)
            {
                isLoading = true;
                lineRing.Visibility = Visibility.Visible;

                StopLine.Clear();

                //Create an HTTP client object
                HttpClient httpClient = new HttpClient();

                //Add a user-agent header to the GET request. 
                var headers = httpClient.DefaultRequestHeaders;

                Uri requestUri = new Uri("https://jp.inoki.cc/wechat/api/stop_all.php");


                List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>();
                formData.Add(new KeyValuePair<string, string>("id", id + ""));
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

                    for (uint i = 0; i < data.Count; i++)
                    {
                        // Parse stop
                        JsonObject lineDirectionObject = data.GetObjectAt(i);

                        string line = lineDirectionObject["line"].GetString();
                        string direction = lineDirectionObject["direction"].GetString();

                        JsonArray times = lineDirectionObject["times"].GetArray();

                        if (times != null && line != null && direction != null)
                        {
                            BusStopLineTime busStopLineTime = new BusStopLineTime();
                            busStopLineTime.Line = line;
                            busStopLineTime.DirectionName = Line.NAME[(int.Parse(line) - 1) * 2 + int.Parse(direction) - 1];
                            busStopLineTime.Direction = direction;
                            /*for (uint j = 0; j < times.Count; j++)
                            {
                                string time = times.GetStringAt(j);
                                Debug.WriteLine(time);
                            }*/
                            // Not Display
                            StopLine.Add(busStopLineTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Refresh error
                }

                isLoading = false;
                lineRing.Visibility = Visibility.Collapsed;
            }

        }

        async private void RefreshLines(object sender, RoutedEventArgs e)
        {
            await GetStopBusTimesAsync(id);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            BusStopAll receiveData = (BusStopAll)e.Parameter;

            if (receiveData != null)
            {
                latitude = receiveData.Lat;
                longitude = receiveData.Lon;

                id = receiveData.BusStopId;
                stop = receiveData.BusStopName;

                title.Text = stop;

                GetStopBusTimesAsync(id);  // Async refresh
                
                if (MainPage.isMapElementsLayersAPIPresent)
                {
                    MapElementsLayer layer = new MapElementsLayer
                    {
                        ZIndex = 1
                    };
                    map.Layers.Add(layer);

                    List<MapElement> BusStopMarkers = new List<MapElement>();

                    BasicGeoposition stopPosition = new BasicGeoposition
                    {
                        Latitude = latitude,
                        Longitude = longitude
                    };

                    map.Center = new Geopoint(stopPosition);
                    map.ZoomLevel = 17;

                    BusStopMarkers.Add(new MapIcon
                    {
                        Location = new Geopoint(stopPosition),
                        NormalizedAnchorPoint = new Point(0.5, 1.0),
                        ZIndex = 0,
                        Title = stop
                    }
                    );

                    layer.MapElements = BusStopMarkers;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private void LineListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopLineTime lineTime = (BusStopLineTime)e.ClickedItem;

            if (lineTime != null)
            {
                BusStopName stopName = new BusStopName()
                {
                    Direction = uint.Parse(lineTime.Direction),
                    Line = lineTime.Line,
                    StopName = stop
                };

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), stopName);
            }
        }
    }
}
