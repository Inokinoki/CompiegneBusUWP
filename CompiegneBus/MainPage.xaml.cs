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

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace CompiegneBus
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool isMapElementsLayersAPIPresent;

        public MainPage()
        {
            this.InitializeComponent();

            // Init Pivot
            mainPivot.SelectionChanged += PivotChanged;

            isMapElementsLayersAPIPresent =
                Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Xaml.Controls.Maps.MapElementsLayer");

            if (isMapElementsLayersAPIPresent)
            {
                // Init map elements layer
                layer = new MapElementsLayer
                {
                    ZIndex = 1
                };
                map.Layers.Add(layer);
            }
            else
            {
                map.Visibility = Visibility.Collapsed;
            }

            // Get Nearby bus stop
            GetNearbyBusStop();
        }

        private void PivotChanged(object sender, RoutedEventArgs e)
        {
            PivotItem pivotItem = (PivotItem)(sender as Pivot).SelectedItem;
            if (pivotItem != null)
            {
                switch (pivotItem.Name)
                {
                    case "NearbyPivotItem":
                        break;
                    case "Line1PivotItem":
                        getLine1(sender, e);
                        break;
                    case "Line2PivotItem":
                        getLine2(sender, e);
                        break;
                    case "Line3PivotItem":
                        getLine3(sender, e);
                        break;
                    case "Line4PivotItem":
                        getLine4(sender, e);
                        break;
                    case "Line5PivotItem":
                        getLine5(sender, e);
                        break;
                }
            }
        }

        // Frame root = Window.Current.Content as Frame;
        // root.Navigate(typeof(BlankPage1),info);

        private MapElementsLayer layer; // Map Layer for stocking bus stops

        async private Task<List<string>> GetLineBusStop(string line, uint direction)
        {
            List<string> stopList = new List<string>();

            //Create an HTTP client object
            HttpClient httpClient = new HttpClient();

            //Add a user-agent header to the GET request. 
            var headers = httpClient.DefaultRequestHeaders;

            Uri requestUri = new Uri("https://jp.inoki.cc/wechat/api/get_bus_stop.php");


            List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>();
            formData.Add(new KeyValuePair<string, string>("line", line));
            formData.Add(new KeyValuePair<string, string>("direction", direction + ""));
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
                    string stop = data.GetStringAt(i);

                    if (stop == null) continue;
                    
                    stopList.Add(stop);
                }
            }
            catch (Exception ex)
            {
                // Refresh error
            }

            return stopList;
        }

        async private Task GetNearbyBusStop()
        {
            var access = await Geolocator.RequestAccessAsync();
            switch (access)
            {
                case GeolocationAccessStatus.Unspecified:
                    // Geoposition not opened          
                    return;
                case GeolocationAccessStatus.Allowed:
                    // All is well
                    var gt = new Geolocator();
                    var position = await gt.GetGeopositionAsync();
                    // position.Coordinate.Latitude;    // Descraped
                    // map.Center = position.Coordinate.Point;
                    BasicGeoposition snPosition = new BasicGeoposition { Latitude = 49.40347, Longitude = 2.8072 }; // Standard test coordinate

                    if (isMapElementsLayersAPIPresent)
                    {
                        map.Center = new Geopoint(snPosition);
                        map.ZoomLevel = 17;
                    }

                    //Create an HTTP client object
                    HttpClient httpClient = new HttpClient();

                    //Add a user-agent header to the GET request. 
                    var headers = httpClient.DefaultRequestHeaders;

                    Uri requestUri = new Uri("https://jp.inoki.cc/wechat/api/stop_nearby.php");


                    List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>();
                    formData.Add(new KeyValuePair<string, string>("lat", snPosition.Latitude + ""));
                    formData.Add(new KeyValuePair<string, string>("lon", snPosition.Longitude + ""));
                    formData.Add(new KeyValuePair<string, string>("level", "2"));
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

                        var BusStopMarkers = new List<MapElement>();   // Create a list

                        for (uint i = 0; i < data.Count; i++)
                        {
                            // Parse stop
                            JsonObject stopObject = data.GetObjectAt(i);

                            if (stopObject == null) continue;

                            string stopName = stopObject["stop"].GetString();

                            JsonObject positionObject = stopObject["pos"].GetObject();

                            if (positionObject == null) continue;

                            double longitude = double.Parse(positionObject["lon"].GetString()),
                                latitude = double.Parse(positionObject["lat"].GetString());

                            // Create bus stop - line list


                            // Create and add bus stop marker
                            BasicGeoposition stopPosition = new BasicGeoposition
                            {
                                Latitude = latitude,
                                Longitude = longitude
                            };
                            BusStopMarkers.Add(new MapIcon
                                {
                                    Location = new Geopoint(stopPosition),
                                    NormalizedAnchorPoint = new Point(0.5, 1.0),
                                    ZIndex = 0,
                                    Title = stopName
                                }
                            );
                        }

                        if (isMapElementsLayersAPIPresent)
                        {
                            layer.MapElements = BusStopMarkers;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Refresh error
                    }

                    break;
                case GeolocationAccessStatus.Denied:            
                    // Not allowed           
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings://privacy/location"));
                    return;
                default:
                    break;
            }
            
        }

        async private void RefreshNearby(object sender, RoutedEventArgs e)
        {
            await GetNearbyBusStop();
        }

        // Line 1
        private bool isLine1Loaded = false;
        private bool isLine1Loading = false;
        public ObservableCollection<BusStopName> Line1Direction1Items { get; set; } = new ObservableCollection<BusStopName>();
        public ObservableCollection<BusStopName> Line1Direction2Items { get; set; } = new ObservableCollection<BusStopName>();
        async private void getLine1(object sender, RoutedEventArgs e)
        {
            if (!isLine1Loaded && !isLine1Loading)
            {
                // is Loading
                isLine1Loading = true;  
                line1Refresh.IsEnabled = false;

                // Load line 1 direction 1
                List<string> direction0 = await GetLineBusStop("1", 1);
                Line1Direction1Items.Clear();
                direction0.ForEach((stop) =>
                {
                    Line1Direction1Items.Add(new BusStopName { StopName = stop });
                });

                // Load line 1 direction 2
                List<string> direction1 = await GetLineBusStop("1", 2);
                Line1Direction2Items.Clear();
                direction1.ForEach((stop) =>
                {
                    Line1Direction2Items.Add(new BusStopName { StopName = stop });
                });

                // Change isloaded state
                if (direction0.Count > 0 && direction1.Count > 0)
                {
                    isLine1Loaded = true;
                    line1Refresh.Visibility = Visibility.Collapsed;
                    line1ProgressRing1.Visibility = Visibility.Collapsed;
                    line1ProgressRing2.Visibility = Visibility.Collapsed;
                }

                // Set isloading state
                isLine1Loading = false; 
                line1Refresh.IsEnabled = true;
            }

        }

        // Line 2
        private bool isLine2Loaded = false;
        private bool isLine2Loading = false;
        public ObservableCollection<BusStopName> Line2Direction1Items { get; set; } = new ObservableCollection<BusStopName>();
        public ObservableCollection<BusStopName> Line2Direction2Items { get; set; } = new ObservableCollection<BusStopName>();
        async private void getLine2(object sender, RoutedEventArgs e)
        {
            if (!isLine2Loaded && !isLine2Loading)
            {
                // is Loading
                isLine2Loading = true;
                line2Refresh.IsEnabled = false;

                // Load line 2 direction 1
                List<string> direction0 = await GetLineBusStop("2", 1);
                Line2Direction1Items.Clear();
                direction0.ForEach((stop) =>
                {
                    Line2Direction1Items.Add(new BusStopName { StopName = stop });
                });

                // Load line 2 direction 2
                List<string> direction1 = await GetLineBusStop("2", 2);
                Line2Direction2Items.Clear();
                direction1.ForEach((stop) =>
                {
                    Line2Direction2Items.Add(new BusStopName { StopName = stop });
                });

                // Change isloaded state
                if (direction0.Count > 0 && direction1.Count > 0)
                {
                    isLine2Loaded = true;
                    line2Refresh.Visibility = Visibility.Collapsed;
                    line2ProgressRing1.Visibility = Visibility.Collapsed;
                    line2ProgressRing2.Visibility = Visibility.Collapsed;
                }

                // Set isloading state
                isLine2Loading = false;
                line2Refresh.IsEnabled = true;
            }

        }

        // Line 3
        private bool isLine3Loaded = false;
        private bool isLine3Loading = false;
        public ObservableCollection<BusStopName> Line3Direction1Items { get; set; } = new ObservableCollection<BusStopName>();
        public ObservableCollection<BusStopName> Line3Direction2Items { get; set; } = new ObservableCollection<BusStopName>();
        async private void getLine3(object sender, RoutedEventArgs e)
        {
            if (!isLine3Loaded && !isLine3Loading)
            {
                // is Loading
                isLine3Loading = true;
                line3Refresh.IsEnabled = false;

                // Load line 3 direction 1
                List<string> direction0 = await GetLineBusStop("3", 1);
                Line3Direction1Items.Clear();
                direction0.ForEach((stop) =>
                {
                    Line3Direction1Items.Add(new BusStopName { StopName = stop });
                });

                // Load line 3 direction 2
                List<string> direction1 = await GetLineBusStop("3", 2);
                Line3Direction2Items.Clear();
                direction1.ForEach((stop) =>
                {
                    Line3Direction2Items.Add(new BusStopName { StopName = stop });
                });

                // Change isloaded state
                if (direction0.Count > 0 && direction1.Count > 0)
                {
                    isLine3Loaded = true;
                    line3Refresh.Visibility = Visibility.Collapsed;
                    line3ProgressRing1.Visibility = Visibility.Collapsed;
                    line3ProgressRing2.Visibility = Visibility.Collapsed;
                }

                // Set isloading state
                isLine3Loading = false;
                line3Refresh.IsEnabled = true;
            }

        }

        // Line 4
        private bool isLine4Loaded = false;
        private bool isLine4Loading = false;
        public ObservableCollection<BusStopName> Line4Direction1Items { get; set; } = new ObservableCollection<BusStopName>();
        public ObservableCollection<BusStopName> Line4Direction2Items { get; set; } = new ObservableCollection<BusStopName>();
        async private void getLine4(object sender, RoutedEventArgs e)
        {
            if (!isLine4Loaded && !isLine4Loading)
            {
                // is Loading
                isLine4Loading = true;
                line4Refresh.IsEnabled = false;

                // Load line 4 direction 1
                List<string> direction0 = await GetLineBusStop("4", 1);
                Line4Direction1Items.Clear();
                direction0.ForEach((stop) =>
                {
                    Line4Direction1Items.Add(new BusStopName { StopName = stop });
                });

                // Load line 4 direction 2
                List<string> direction1 = await GetLineBusStop("4", 2);
                Line4Direction2Items.Clear();
                direction1.ForEach((stop) =>
                {
                    Line4Direction2Items.Add(new BusStopName { StopName = stop });
                });

                // Change isloaded state
                if (direction0.Count > 0 && direction1.Count > 0)
                {
                    isLine4Loaded = true;
                    line4Refresh.Visibility = Visibility.Collapsed;
                    line4ProgressRing1.Visibility = Visibility.Collapsed;
                    line4ProgressRing2.Visibility = Visibility.Collapsed;
                }

                // Set isloading state
                isLine4Loading = false;
                line4Refresh.IsEnabled = true;
            }

        }

        // Line 5
        private bool isLine5Loaded = false;
        private bool isLine5Loading = false;
        public ObservableCollection<BusStopName> Line5Direction1Items { get; set; } = new ObservableCollection<BusStopName>();
        public ObservableCollection<BusStopName> Line5Direction2Items { get; set; } = new ObservableCollection<BusStopName>();
        async private void getLine5(object sender, RoutedEventArgs e)
        {
            if (!isLine5Loaded && !isLine5Loading)
            {
                // is Loading
                isLine5Loading = true;
                line5Refresh.IsEnabled = false;

                // Load line 5 direction 1
                List<string> direction0 = await GetLineBusStop("5", 1);
                Line5Direction1Items.Clear();
                direction0.ForEach((stop) =>
                {
                    Line5Direction1Items.Add(new BusStopName { StopName = stop });
                });

                // Load line 5 direction 2
                List<string> direction1 = await GetLineBusStop("5", 2);
                Line5Direction2Items.Clear();
                direction1.ForEach((stop) =>
                {
                    Line5Direction2Items.Add(new BusStopName { StopName = stop });
                });

                // Change isloaded state
                if (direction0.Count > 0 && direction1.Count > 0)
                {
                    isLine5Loaded = true;
                    line5Refresh.Visibility = Visibility.Collapsed;
                    line5ProgressRing1.Visibility = Visibility.Collapsed;
                    line5ProgressRing2.Visibility = Visibility.Collapsed;
                }

                // Set isloading state
                isLine5Loading = false;
                line5Refresh.IsEnabled = true;
            }

        }

        private void ListLine1Direction1_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "1";
                clickedMenuItem.Direction = 1;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine1Direction2_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "1";
                clickedMenuItem.Direction = 2;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine2Direction1_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "2";
                clickedMenuItem.Direction = 1;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine2Direction2_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "2";
                clickedMenuItem.Direction = 2;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine3Direction1_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "3";
                clickedMenuItem.Direction = 1;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine3Direction2_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "3";
                clickedMenuItem.Direction = 2;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine4Direction1_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "4";
                clickedMenuItem.Direction = 1;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine4Direction2_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "4";
                clickedMenuItem.Direction = 2;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine5Direction1_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "5";
                clickedMenuItem.Direction = 1;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }

        private void ListLine5Direction2_ItemClick(object sender, ItemClickEventArgs e)
        {
            BusStopName clickedMenuItem = (BusStopName)e.ClickedItem;

            if (clickedMenuItem != null)
            {
                Debug.WriteLine(clickedMenuItem.StopName);
                clickedMenuItem.Line = "5";
                clickedMenuItem.Direction = 2;

                Frame root = Window.Current.Content as Frame;
                root.Navigate(typeof(BusDetail), clickedMenuItem);
            }
        }
    }
}
