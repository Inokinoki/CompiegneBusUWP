using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompiegneBus
{
    public class BusStopLineDirection
    {
        // 5 - Direction name
        public ObservableCollection<BusLineDirection> LineDirection { get; set; } = 
            new ObservableCollection<BusLineDirection>();
        public string StopName { get; set; }
    }

    public class BusLineDirection
    {
        public string Line { get; set; }
        public string DirectionName { get; set; }
    }
}
