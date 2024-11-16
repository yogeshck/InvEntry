using InvEntry.Tally.Model;
using InvEntry.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace InvEntry.Tally;

public interface ITallyXMLService
{
    Task SendToTally(TallyXmlMesage tallyXmlMesage);
}

public class TallyXMLService : ITallyXMLService 
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TallyXMLService> _logger;

    public TallyXMLService(IHttpClientFactory httpClientFactory,
         ILogger<TallyXMLService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendToTally(TallyXmlMesage tallyXmlMesage)
    {
        using var httpClient = _httpClientFactory.CreateClient("Tally");

        var xmlrequest = XMLUtil.SerializeToString(tallyXmlMesage);
        _logger.LogDebug("Tally Request \n*******\n\t{0}\n*******\n", xmlrequest);

        HttpContent content = new StringContent(xmlrequest, Encoding.UTF8, "text/xml");

        var response = await httpClient.PostAsync(httpClient.BaseAddress, content);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error sending to Tally {0}", response.ReasonPhrase);
        }

        _logger.LogDebug("Tally Reponse \n*******\n\t{0}\n*******\n", responseContent);
    }
}
