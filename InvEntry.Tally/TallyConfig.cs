using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Tally;

public interface ITallyConfig
{
    public string? BaseAddress { get; set; }
}

public class TallyConfig : ITallyConfig
{
    public TallyConfig(IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        configuration.Bind("TallyConfig", this);
    }

    public string? BaseAddress { get; set; }
}
