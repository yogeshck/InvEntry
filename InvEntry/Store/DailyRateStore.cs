using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Store
{
    public class DailyRateStore
    {
        private Dictionary<DateTime, List<DailyRate>> _dailyRate;

        private static DailyRateStore _instance { get; set; }
        private DailyRateStore() 
        {
            _dailyRate = new();
        }

        public static DailyRateStore Instance 
        {
            get
            {
                if( _instance == null )
                _instance = new DailyRateStore();
                return _instance;
            }
        }
    }
}
