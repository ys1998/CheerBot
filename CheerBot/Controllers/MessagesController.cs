using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using CheerBot.Controllers;
using PhraseDetection;




namespace CheerBot
{

    public static class Global
    {
        public static double total_score = 0;
        public static int flag = 0;
        public static int count = 0;
    }

   



    [BotAuthentication]
    public class MessagesController : ApiController
    {
       
        

        //introductory questions
        internal static IDialog<Intro> MakeRootDialog()

        {

            return Chain.From(() => FormDialog.FromForm(Intro.BuildForm));

        }

        //code to transfer control to Junction.cs
        internal static IDialog<Junction> DialogJunction()

        {

            return Chain.From(() => FormDialog.FromForm(Junction.BuildForm));

        }




        //this is the code required for this bot

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            if (activity.Type == ActivityTypes.Message)
            {

                
                StateClient sc = activity.GetStateClient();

                BotData userData = sc.BotState.GetPrivateConversationData(

                    activity.ChannelId, activity.Conversation.Id, activity.From.Id);

                var boolProfileComplete = userData.GetProperty<bool>("is_done");

                if (!boolProfileComplete)

                {

                    await Conversation.SendAsync(activity, MakeRootDialog);
                  


                }

                if (boolProfileComplete)

                {
                    var reply=new Activity();
                    







                    const string apiKey = "972b605ce9c14ec29316db2e7271c47c";
                    const string queryUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

                    var client = new HttpClient
                    {
                        DefaultRequestHeaders = {
                 {"Ocp-Apim-Subscription-Key", apiKey},
                 {"Accept", "application/json"}
             }
                    };



                    var sentimentInput = new BatchInput
                    {
                        documents = new List<DocumentInput> {
                new DocumentInput {
                    id = 1,
                    text = activity.Text,
                }
            }
                    };
                    var json = JsonConvert.SerializeObject(sentimentInput);
                    var sentimentPost = await client.PostAsync(queryUri, new StringContent(json, Encoding.UTF8, "application/json"));
                    var sentimentRawResponse = await sentimentPost.Content.ReadAsStringAsync();
                    var sentimentJsonResponse = JsonConvert.DeserializeObject<BatchResult>(sentimentRawResponse);
                    var sentimentScore = sentimentJsonResponse?.documents?.FirstOrDefault()?.score ?? 0;

                    Global.total_score += sentimentScore;
                    string message;
                    int response;
                    if (sentimentScore > 0.85)
                    {
                        message = $"Wow! That's great to hear! :o";
                        response = 5;

                    }
                    else if ((sentimentScore <= 0.85) && (sentimentScore > 0.6))
                    {
                        message = "That's good! :)";
                        response = 4;
                    }
                    else if ((sentimentScore <= 0.6) && (sentimentScore > 0.45))

                    {
                        message = "I see...";
                        response = 3;
                    }
                    else if ((sentimentScore <= 0.45) && (sentimentScore > 0.3))
                    {
                        message = "Hard luck...";
                        response = 2;
                    }
                    else if ((sentimentScore <= 0.3) && (sentimentScore > 0.15))
                    {
                        message = $"I'm sorry to hear that... :(";
                        response = 1;

                    }
                    else
                    {
                        message = "Oh...I am very sorry...";
                        response = 0;
                    }

                        reply = activity.CreateReply(message);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    //questions
                    if (Global.flag == 1)
                    {
                        reply = activity.CreateReply("Tell me whether you like sports, when you last went out to play and which sport you like.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    if (Global.flag == 2)
                    {
                        reply = activity.CreateReply("Have you felt sleepless, tired or anxious in the past few days?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    if (Global.flag == 3)
                    {
                        reply = activity.CreateReply("How much do you bother about yourself?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    if (Global.flag == 4)
                    {
                        reply = activity.CreateReply("Is relaxing or resting a concern for you?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    if (Global.flag == 5)
                    {
                        reply = activity.CreateReply("Are you short-tempered? How shortly do you get irritated?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }


                    if (Global.count == 6)
                    {
                        Global.total_score = Global.total_score / Global.count;
                        //assigning response value based on final score
                        if (Global.total_score> 0.85){ response = 5; }
                        else if ((Global.total_score <= 0.85) && (Global.total_score > 0.6))
                        {
                            
                            response = 4;
                        }
                        else if ((Global.total_score <= 0.6) && (Global.total_score > 0.45))
                        {
                            
                            response = 3;
                        }
                        else if ((Global.total_score <= 0.45) && (Global.total_score > 0.3))
                        {
                            
                            response = 2;
                        }
                        else if ((Global.total_score <= 0.3) && (Global.total_score > 0.15))
                        {
                           
                            response = 1;

                        }
                        else
                        {
                           
                            response = 0;
                        }
                        reply = activity.CreateReply("Here's something to cheer you up..."+"\nPlease do have a look!");
                        await connector.Conversations.ReplyToActivityAsync(reply);



                    //displaying inspirational quotes, number depending upon the response
                    
                        const string Key = "e56026fe3b1c4a64ae4325008324d2c8";
                        string surl = "https://api.cognitive.microsoft.com/bing/v5.0/images/search"
                                          + "?q=motivational quotes&offset=" + ((response + 1) * 5).ToString() + "&count=" + ((6 - response)*2).ToString() + "&safeSearch=Moderate";


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
                            reply = activity.CreateReply("Sorry...could not find any image.");
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }

                        reply = activity.CreateReply("Have a look at some motivational quotes!");
                        await connector.Conversations.ReplyToActivityAsync(reply);


                        for (int i = 0; i < (6 - response)*2; i++)
                        {
                            string Result = imageResult[i].contentUrl;
                            var replyMessage = activity.CreateReply();
                            replyMessage.Recipient = activity.From;
                            replyMessage.Type = ActivityTypes.Message;
                            replyMessage.Text = null;
                            replyMessage.Attachments = new System.Collections.Generic.List<Attachment>();
                            replyMessage.Attachments.Add(new Attachment()
                            {
                                ContentUrl = Result,
                                ContentType = "image/png"
                            });

                            //Reply to user message with image attachment
                            await connector.Conversations.ReplyToActivityAsync(replyMessage);
                        }


                        reply = activity.CreateReply("Hope you liked them! May you remain motivated!");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                       
                        reply = activity.CreateReply("Keep sharing your experiences :) !!!");
                        await connector.Conversations.ReplyToActivityAsync(reply);

                        reply = activity.CreateReply("If you wish to continue, type in 'hi' ; if you want to exit, type 'bye'");
                        await connector.Conversations.ReplyToActivityAsync(reply);


                    }
                    if(Global.count==7)
                    {
                        if ((activity.Text == "hi") || (activity.Text == "Hi") || (activity.Text == "HI"))
                        {
                            Global.count = -1; //to account for the count++ statement in the end, so that after this iteration count becomes 0
                            Global.flag = -1;
                        }
                        else
                        {
                            reply = activity.CreateReply("Bye! Meet you later!");
                            await connector.Conversations.ReplyToActivityAsync(reply);
                            System.Environment.Exit(0);
                        }
                    }
                    




                    

                    Global.count++;
                    Global.flag++;
                }


                else
                {
                    HandleSystemMessage(activity);
                }
            }


            var response1 = Request.CreateResponse(HttpStatusCode.OK);
            return response1;
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
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
            

            return null;
        }
    }
}