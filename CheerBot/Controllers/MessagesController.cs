using BingSearchResponse;
using CheerBot.Controllers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;


namespace CheerBot
{

    public static class Global
    {
        // variable to store the total "sentiment score" of the user
        public static double total_score = 0;

        // List of (List of strings) to store detected keyphrases from "sad" answers
        public static List<List<string>> KeyPhrases = new List<List<string>>();

        // components to store questions and ask them in a random order (independent of number of questions)
        public static Random rnd = new Random();              //for randomizing the order of questions
        public static int q_length = 5;                       //total number of questions in the database
        public static int no_of_ques_to_be_asked = q_length;  //change this field as per requirement
        public static string[] questions =                    //questions database
        {
            "Tell me whether you like sports, when you last went out to play and which sport you like.",
            "Have you felt sleepless, tired or anxious in the past few days?",
            "How much do you bother about yourself?",
            "Is relaxing or resting a concern for you?",
            "Are you short-tempered? How shortly do you get irritated?"
        };
        public static string PopQuestion()             //function to randomly pop a question from the database
        {
            if ((questions.Length != 0) && ((q_length - no_of_ques_to_be_asked) == questions.Length))
            { questions[0] = "done"; return "That's all what I had to ask!"; }
            else if (questions.Length == 0)
            { questions = new string[] { "done" }; return "That's all what I had to ask!"; }
            else
            {
                int pos = rnd.Next(0, questions.Length);
                string res = questions[pos];
                string[] temp = new string[questions.Length - 1];
                for (int i = 0, k = 0; i < questions.Length; i++)
                {
                    if (i != pos) { temp[k] = questions[i]; k++; }

                }
                questions = temp;
                return res;
            }
        }

        // Components representing the bot's response 
        public static string[] BotResponse = {
            "I am sorry to hear that...",
            "Hard luck...",
            "I see...",
            "That's good!",
            "Wow! That's great to hear!"
        };
        //get the bot's response based on detected sentiment
        public static string GetBotResponse(double SentimentScore)
        {
            double width = 1.0 / BotResponse.Length;
            int pos = (int)(SentimentScore / width);
            return BotResponse[pos];
        }

        //function to return the number of responses based on final score
        public static int GetNResponse(double finalScore)
        {
            return (BotResponse.Length - (int)(finalScore * BotResponse.Length)) + 1;
        }

        //function to update the sentiment based on the expected sentiment
        public static double updateSentiment(double sentimentScore)
        {
            return sentimentScore;                                             // CHANGES TO BE MADE HERE
        }
    }


    [BotAuthentication]
    public class MessagesController : ApiController
    {

        //introductory questions
        internal static IDialog<Intro> MakeRootDialog()
        {
            return Chain.From(() => FormDialog.FromForm(Intro.BuildForm));
        }

        /* Code controlling the Bot */

        //function which is called whenever an "activity" is encountered 
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            if (activity.Type == ActivityTypes.Message)
            {
                StateClient sc = activity.GetStateClient();
                BotData userData = sc.BotState.GetPrivateConversationData(activity.ChannelId, activity.Conversation.Id, activity.From.Id);
                var boolProfileComplete = userData.GetProperty<bool>("is_done");
                if (!boolProfileComplete)
                {
                    await Conversation.SendAsync(activity, MakeRootDialog);
                }
                else
                {
                    var reply = new Activity();

                    //configuring the API's components - key, url
                    const string apiKey = "0299fdb1d6d84142a31908ad1b2a45e3";
                    const string queryUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";
                    var client = new HttpClient
                    {
                        DefaultRequestHeaders = {
                            { "Ocp-Apim-Subscription-Key", apiKey },
                            { "Accept", "application/json" } }
                    };

                    //detecting the sentiment in user's answer
                    var sentimentInput = new BatchInput
                    {
                        documents = new List<DocumentInput> { new DocumentInput { id = 1, text = activity.Text, } }
                    };
                    var json = JsonConvert.SerializeObject(sentimentInput);
                    var sentimentPost = await client.PostAsync(queryUri, new StringContent(json, Encoding.UTF8, "application/json"));
                    var sentimentRawResponse = await sentimentPost.Content.ReadAsStringAsync();
                    var sentimentJsonResponse = JsonConvert.DeserializeObject<BatchResult>(sentimentRawResponse);
                    var sentimentScore = sentimentJsonResponse?.documents?.FirstOrDefault()?.score ?? 0;

                    //updating the sentimentScore depending upon the expected sentiment
                    sentimentScore = Global.updateSentiment(sentimentScore);

                    //sending the bot's response depending on the detected sentiment
                    Global.total_score += sentimentScore;
                    string message = Global.GetBotResponse(sentimentScore);
                    reply = activity.CreateReply(message);
                    await connector.Conversations.ReplyToActivityAsync(reply);

                    // DETECTING KEYPHRASES IF SENTIMENT-SCORE IS LOWER THAN NORMAL //

                    if (sentimentScore < 0.3)
                    {
                        //configuring keyPhrase Detection API's components - key, url
                        const string kpd_queryUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases";
                        var kpd_client = new HttpClient
                        {
                            DefaultRequestHeaders = {
                            { "Ocp-Apim-Subscription-Key", apiKey },
                            { "Accept", "application/json" } }
                        };

                        var keyphraseInput = new KPD_Input
                        {
                            documents = new List<KPD_DocumentInput> { new KPD_DocumentInput { language = "en", id = 1, text = activity.Text, } }
                        };

                        // sending activity.Text for analysis
                        var kpd_json = JsonConvert.SerializeObject(keyphraseInput);
                        var kpd_Post = await kpd_client.PostAsync(kpd_queryUri, new StringContent(kpd_json, Encoding.UTF8, "application/json"));
                        var kpd_RawResponse = await kpd_Post.Content.ReadAsStringAsync();
                        KPD_Result kpd_res = JsonConvert.DeserializeObject<KPD_Result>(kpd_RawResponse);

                        // storing keyPhrases of a given activity in a List
                        List<string> phrases = new List<string>();
                        for (int i = 0; i < kpd_res.documents.Count; i++)
                        {
                            for (int j = 0; j < kpd_res.documents[i].keyPhrases.Count; j++)
                            { phrases.Add(kpd_res.documents[i].keyPhrases[j]); }
                        }

                        // search for jokes and one-liners based on detected keyphrases
                        if (phrases[0] != "")
                        {
                            // storing above List in the Global List
                            Global.KeyPhrases.Add(phrases);

                            reply = activity.CreateReply(string.Format("I perceive that you are sad due to \'{0}\'.", string.Join("\', \'", phrases.ToArray())));
                            await connector.Conversations.ReplyToActivityAsync(reply);
                            reply = activity.CreateReply("Time to cheer up! :)");
                            await connector.Conversations.ReplyToActivityAsync(reply);

                            //configuring BingWebSearch and BingImageSearch API - key, url, url_parameters
                            const string Key = "0cd4df3677d64499b05eda5a4912510a";
                            List<string> web_url = new List<string>();
                            List<string> surl = new List<string>();

                            // DISPLAYING JOKES (MAX. 5)

                            List<Value> websearchResult = new List<Value>();
                            for (int i = 0; i < Math.Min(5, phrases.Count); i++)
                            {
                                web_url.Add("https://api.cognitive.microsoft.com/bing/v5.0/search?q=jokes+on+" + phrases.ToArray()[i] + "&count="+((int)Math.Floor(5.0/Math.Min(5,phrases.Count))).ToString()+"&safeSearch=Strict");
                            }
                            for (int i = 0; i < Math.Min(3, phrases.Count); i++)
                            {
                                HttpClient client1 = new HttpClient();
                                client1.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Key); //authentication header to pass the API key
                                client1.DefaultRequestHeaders.Add("Accept", "application/json");
                                string bingRawResponse = null;
                                RootObject bingJsonResponse = null;
                                bingRawResponse = await client1.GetStringAsync(web_url.ToArray()[i]);
                                bingJsonResponse = JsonConvert.DeserializeObject<RootObject>(bingRawResponse);
                                websearchResult.AddRange(bingJsonResponse.webPages.value);
                            }
                            if (websearchResult.Count == 0)
                            {
                                //added code to handle the case where results are null or zero
                                reply = activity.CreateReply("Sorry, no jokes today.");
                                await connector.Conversations.ReplyToActivityAsync(reply);
                            }
                            else
                            {
                                // create replies with joke_links
                                string[] number_response = { "The first link to some hilarious jokes -", "Here's the second one -", "Have some more - the third link -", "I think the fourth one would be better -", "And finally, the last one -"};

                                reply = activity.CreateReply("Read these jokes to lighten a bit.");
                                await connector.Conversations.ReplyToActivityAsync(reply);

                                for (int i = 0; i < websearchResult.Count; i++)
                                {
                                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(number_response[i]));
                                    string joke_link = "";
                                    if (websearchResult[i].displayUrl.Contains("http") == false)
                                    {
                                        joke_link = websearchResult[i].name + "\n\n" + "http://" + websearchResult[i].displayUrl + "\n\n" + websearchResult[i].snippet;
                                    }
                                    else
                                    {
                                        joke_link = websearchResult[i].name + "\n\n" + websearchResult[i].displayUrl + "\n\n" + websearchResult[i].snippet;
                                    }
                                    //Reply to user message with image attachment
                                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(joke_link));
                                }
                            }
 
                            
                            // DISPLAYING ONE-LINERS/MEMES aka SORROW-BUSTERS AS IMAGES (MAX. 3)


                            List<ImageResult> imageResult = new List<ImageResult>();
                            for (int i = 0; i < Math.Min(3, phrases.Count); i++)
                            {
                                surl.Add("https://api.cognitive.microsoft.com/bing/v5.0/images/search?q=" + phrases.ToArray()[i] + "+funny+memes&offset=5&count=1&safeSearch=Strict");
                            }
                            for (int i = 0; i < Math.Min(3, phrases.Count); i++)
                            {
                                HttpClient client1 = new HttpClient();
                                client1.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Key); //authentication header to pass the API key
                                client1.DefaultRequestHeaders.Add("Accept", "application/json");
                                string bingRawResponse = null;
                                BingImageSearchResponse bingJsonResponse = null;
                                bingRawResponse = await client1.GetStringAsync(surl.ToArray()[i]);
                                bingJsonResponse = JsonConvert.DeserializeObject<BingImageSearchResponse>(bingRawResponse);
                                imageResult.AddRange(bingJsonResponse.value);
                            }
                            if (imageResult.Count == 0)
                            {
                                //added code to handle the case where results are null or zero
                                reply = activity.CreateReply("It seems that I have run out of sorrow-busters...");
                                await connector.Conversations.ReplyToActivityAsync(reply);
                            }
                            else
                            {
                                // create a reply with one-liner images as attachments
                                string[] number_response = { "Here's the first one", "Then the second...", "And finally the third." };

                                reply = activity.CreateReply("Some sorrow-busters.");
                                await connector.Conversations.ReplyToActivityAsync(reply);

                                for (int i = 0; i < imageResult.Count; i++)
                                {
                                    string Result = imageResult.ToArray()[i].contentUrl;
                                    var replyMessage = activity.CreateReply();
                                    replyMessage.Recipient = activity.From;
                                    replyMessage.Type = ActivityTypes.Message;
                                    replyMessage.Text = number_response[i];
                                    replyMessage.Attachments = new List<Attachment>();
                                    replyMessage.Attachments.Add(new Attachment()
                                    {
                                        ContentUrl = Result,
                                        ContentType = "image/png"

                                    });
                                    //Reply to user message with image attachment
                                    await connector.Conversations.ReplyToActivityAsync(replyMessage);
                                }


                            }

                        }

                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////

                    //asking a random question from the database to the user
                    reply = activity.CreateReply(Global.PopQuestion());
                    await connector.Conversations.ReplyToActivityAsync(reply);

                    //terminating case - when all questions have been asked
                    if (Global.questions[0] == "done")
                    {
                        //averaging the total score
                        Global.total_score = Global.total_score / Global.q_length;

                        //assigning "number of responses" value based on final score
                        int n_response = Global.GetNResponse(Global.total_score);
                        reply = activity.CreateReply("Here's something to cheer you up..." + "\nPlease do have a look!");
                        await connector.Conversations.ReplyToActivityAsync(reply);

                        // DISPLAYING INSPIRATIONAL QUOTES, NUMBER DEPENDING UPON THE RESPONSE

                        //configuring BingImageSearch API - key, url, url_parameters
                        const string Key = "0cd4df3677d64499b05eda5a4912510a";
                        string surl = "https://api.cognitive.microsoft.com/bing/v5.0/images/search"
                                            + "?q=motivational quotes&offset=" + ((n_response + 1) * 5).ToString() + "&count=" + (n_response * 2).ToString() + "&safeSearch=Moderate";

                        HttpClient client1 = new HttpClient();
                        client1.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Key); //authentication header to pass the API key
                        client1.DefaultRequestHeaders.Add("Accept", "application/json");
                        string bingRawResponse = null;
                        BingImageSearchResponse bingJsonResponse = null;

                        bingRawResponse = await client1.GetStringAsync(surl);
                        bingJsonResponse = JsonConvert.DeserializeObject<BingImageSearchResponse>(bingRawResponse);

                        ImageResult[] imageResult = bingJsonResponse.value;
                        if (imageResult == null || imageResult.Length == 0)
                        {
                            //added code to handle the case where results are null or zero
                            reply = activity.CreateReply("Sorry...could not find any image-quote.");
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                        else
                        {
                            reply = activity.CreateReply("Have a look at some motivational quotes!");
                            await connector.Conversations.ReplyToActivityAsync(reply);

                            // create a reply with image-quotes as attachments
                            var replyMessage = activity.CreateReply();
                            replyMessage.Recipient = activity.From;
                            replyMessage.Type = ActivityTypes.Message;
                            replyMessage.Text = "Hope you like them! Remain motivated!";
                            replyMessage.Attachments = new List<Attachment>();

                            for (int i = 0; i < n_response * 2; i++)
                            {
                                string Result = imageResult[i].contentUrl;
                                replyMessage.Attachments.Add(new Attachment()
                                {
                                    ContentUrl = Result,
                                    ContentType = "image/png"
                                });
                            }
                            //Reply to user message with image attachment
                            await connector.Conversations.ReplyToActivityAsync(replyMessage);
                        }

                        reply = activity.CreateReply("Keep sharing your experiences :) !!!");
                        await connector.Conversations.ReplyToActivityAsync(reply);

                        reply = activity.CreateReply("Bye! Meet you later!");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        Environment.Exit(0);
                    }
                }
            }
            else
            {
                //to handle events other than messages
                await connector.Conversations.ReplyToActivityAsync(HandleSystemMessage(activity));
            }

            var response1 = Request.CreateResponse(HttpStatusCode.OK);
            return response1;
        }

        private List<string> getKeyPhrases(string text)
        {
            throw new NotImplementedException();
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // When the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
                // When the user pings/invokes the bot 
            }
            return null;
        }
    }
}