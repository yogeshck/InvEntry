using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
    {
        public partial class MtblReference : BaseEntity
        {
            [ObservableProperty]
            private int? _SeqNbr;

            [ObservableProperty]
            public string? _RefName; 

            [ObservableProperty]
            public string? _RefCode;

            [ObservableProperty]
            public string? _RefValue;

            [ObservableProperty]
            public short? _SortSeq;

            [ObservableProperty]
            public string? _RefDesc;

            [ObservableProperty]
            public string? _Module;

            [ObservableProperty]
            public bool _IsActive;
    }
}
