using System;

namespace InvEntry.GST.Rules
{
    public interface IGstRuleProvider
    {
        decimal GetB2ClThreshold(DateTime documentDate);
    }
}