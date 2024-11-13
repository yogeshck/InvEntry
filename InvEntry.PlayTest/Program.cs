// See https://aka.ms/new-console-template for more information
using InvEntry.Tally;
using InvEntry.Utils;

TallyMessageBuilder tallyMessageBuilder = new TallyMessageBuilder(TallyXMLMessageType.SendVoucherToTally);

TallyVoucher tallyVoucer = new TallyVoucher();

tallyVoucer.VOUCHERNUMBER = "ASASD";

tallyMessageBuilder.AddVoucher(tallyVoucer);

var content = tallyMessageBuilder.Build();

var xml = XMLUtil.SerializeToString(content);

Console.WriteLine(xml);

Console.ReadLine();