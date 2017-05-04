using System;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;

namespace CheerBot.Controllers
{
    [Serializable]
    public class Intro
    {
        // these are the fields that will hold the data we will gather with the form
        [Prompt("Could you tell me your name? {||}")]
        public string Name;

        [Prompt("What is your gender? {||}")]
        public Gender Gender;

        // This method 'builds' the form and will be called by the MakeRootDialog method of the MessagesController.cs 
        public static IForm<Intro> BuildForm()
        {
            return new FormBuilder<Intro>()
                    .Message("Hi there! Welcome to CheerBot!")
                    .OnCompletion(async (context, introForm) =>
                    {
                        // set BotUserData
                        context.PrivateConversationData.SetValue<bool>(
                            "is_done", true);
                        context.PrivateConversationData.SetValue<string>(
                            "Name", introForm.Name);
                        context.PrivateConversationData.SetValue<string>(
                            "Gender", introForm.Gender.ToString());
                        await context.PostAsync("So, how's life going on?");
                    })
                    .Build();
        }
    }
    // this enum provides the possible values for gender
    [Serializable]
    public enum Gender
    {
        Male = 1, Female = 2, Other = 3
    };
}