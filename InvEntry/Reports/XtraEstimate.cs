﻿using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using InvEntry.Utils;
using System.Globalization;

namespace InvEntry.Reports
{
    public partial class XtraEstimate : DevExpress.XtraReports.UI.XtraReport
    {
		private string NumberToWordsFormat = "{0} ONLY";
        public XtraEstimate()
        {
            InitializeComponent();
        }

    }
}
