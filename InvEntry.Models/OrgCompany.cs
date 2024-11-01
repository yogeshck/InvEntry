using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public class OrgCompany : BaseEntity
    {

        public string? AccountId { get; set; }

        public string? Name { get; set; }

        public long? DraftId { get; set; }

        public long? InvId { get; set; }

        public string? PanNbr { get; set; }

        public string? Notes { get; set; }

        public string? ServiceTaxNbr { get; set; }

        public string? Status { get; set; }

        public short? DeleteFlag { get; set; }

        public long TenantGkey { get; set; }

        public string? Tagline { get; set; }

        public string? CinNbr { get; set; }

        public string? TinNbr { get; set; }

        public long? AddressGkey { get; set; }

        public string? GstNbr { get; set; }

        public bool? ThisCompany { get; set; }
    }
}
