using System;

namespace InvEntry.Gst.Core.Rules
{
    public interface IGstRuleProvider
    {
        decimal GetB2ClThreshold(DateTime documentDate);
    }
}