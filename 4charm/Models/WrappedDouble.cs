using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4charm.Models
{
    public class WrappedDouble
    {
        public double Value
        {
            get;
            set;
        }

        public WrappedDouble()
        {
            Value = CriticalSettingsManager.Current.TextSize;
        }
    }
}
