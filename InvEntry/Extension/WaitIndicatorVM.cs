using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Extension
{
    public class WaitIndicatorVM
    {
        public bool IsVisible { get; set; }
        public string Content { get; set; }

        private WaitIndicatorVM(bool visible, string content) 
        {
            IsVisible = visible;
            Content = content;
        }

        public static WaitIndicatorVM ShowIndicator(string content) 
            => new WaitIndicatorVM(true, content);

        public static WaitIndicatorVM HideIndicator()
            => new WaitIndicatorVM(false, string.Empty);
    }
}
