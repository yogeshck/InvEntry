namespace InvEntry.Tally;

public class TallyMessageBuilder
{
    ENVELOPE _env;

    public TallyMessageBuilder() 
    {
        _env = new ENVELOPE();
        _env.HEADER = new HEADER();
        _env.HEADER.TALLYREQUEST = "Import Data";

        _env.BODY = new BODY();
    }
}
