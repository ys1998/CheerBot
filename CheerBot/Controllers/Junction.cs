using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebSearch;
using PhraseDetection;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;


namespace CheerBot.Controllers
{


    [Serializable]
    public class Junction
    {

        private const string BaseUrl = "https://westus.api.cognitive.microsoft.com/";
        private const string AccountKey = "972b605ce9c14ec29316db2e7271c47c";



        public static async Task<string> MakeRequests(string input)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseUrl);

                // Request headers.
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AccountKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Request body. Insert your text data here in JSON format.


                byte[] byteData = Encoding.UTF8.GetBytes(input);

                // Detect key phrases:
                var uri = "text/analytics/v2.0/keyPhrases";
                var response = await CallEndpoint(client, uri, byteData);
                return response;

            }
        }

        static async Task<String> CallEndpoint(HttpClient client, string uri, byte[] byteData)
        {
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(uri, content);
                return await response.Content.ReadAsStringAsync();
            }
        }







        [Prompt("Why? What happened?")]
        public string rep;


        //find out the key phrases present in rep
        public static IForm<Junction> BuildForm()

        {

            return new FormBuilder<Junction>()

                    .Message("")

                    .OnCompletion(async (context, junctionForm) =>

                    {

                        string res=await MakeRequests(junctionForm.rep);
                        await context.PostAsync(res);

                    })

                    .Build();

        }



    }
}