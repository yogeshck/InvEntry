using InvEntry.Tally.Model;

namespace InvEntry.Tally;

public enum TallyXMLMessageType
{
    SendVoucherToTally // Send to Tally
}


public class TallyMessageBuilder
{
    TallyXmlMesage _env;

    private string _currentCompany;

    public TallyMessageBuilder(TallyXMLMessageType messageType, string currentCompany) 
    {
        _currentCompany = currentCompany;
        Initialize(messageType);
    }

    private void Initialize(TallyXMLMessageType messageType)
    {
        switch (messageType)
        {
            case TallyXMLMessageType.SendVoucherToTally:
                _env = new TallyXmlMesage();
                _env.HEADER = new TallyHeader()
                {
                    Version = 1,
                    TallyRequest = TallyRequestEnum.Import,
                    Type = TallyDataTypeEnum.Data,
                    ID = "VOUCHERS"
                };
                _env.BODY = new TallyBody();
                _env.BODY.BodyDesc = new TallyBodyDesc();
                _env.BODY.BodyDesc.Variables = new TallyStaticVariables();
                _env.BODY.BodyDesc.Variables.SVCURRENTCOMPANY = _currentCompany;

                _env.BODY.Data = new TallyVoucherMessage()
                {
                    Vouchers = new List<TallyVoucher>()
                };
                break;
            default: 
                _env = new TallyXmlMesage();
                break;
        }
    }

    public TallyMessageBuilder AddVoucher(TallyVoucher voucher)
    {
        (_env.BODY.Data).Vouchers.Add(voucher);
        return this;
    }

    public TallyXmlMesage Build() => _env;
}
