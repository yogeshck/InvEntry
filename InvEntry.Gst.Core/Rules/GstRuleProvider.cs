using System;

namespace InvEntry.Gst.Core.Rules
{
    public class GstRuleProvider : IGstRuleProvider
    {
        public decimal GetB2ClThreshold(DateTime documentDate)
        {
            // Effective from 01-Aug-2024
            if (documentDate >= new DateTime(2024, 8, 1))
                return 100000M;

            // Earlier rule
            return 250000M;
        }
    }
}