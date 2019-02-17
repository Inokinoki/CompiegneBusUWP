using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompiegneBus
{
    public class BusStopLineTime
    {
        // 5 - Direction name - Preview time
        public ObservableCollection<string> Times { get; set; } =
            new ObservableCollection<string>();
        public string Line { get; set; }
        public string Direction { get; set; }
        public string DirectionName { get; set; }
    }
}
