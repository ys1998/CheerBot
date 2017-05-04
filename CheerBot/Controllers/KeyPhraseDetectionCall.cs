using System.Collections.Generic;

// Classes to store the input for the keyphrase detection API call
public class KPD_Input            // KPD - KeyPhrase Detection :)
{
    public List<KPD_DocumentInput> documents { get; set; }
}
public class KPD_DocumentInput
{
    public string language { get; set; }
    public double id { get; set; }
    public string text { get; set; }
}

// Classes to store the result from the keyphrase detection 
public class KPD_Result
{
    public List<KPD_DocumentResult> documents { get; set; }
}
public class KPD_DocumentResult
{
    public string id { get; set; }
    public List<string> keyPhrases { get; set; }
}