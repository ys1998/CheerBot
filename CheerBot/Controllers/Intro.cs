
using System;

using Microsoft.Bot.Builder.Dialogs;

using Microsoft.Bot.Builder.FormFlow;

namespace CheerBot.Controllers

{

    [Serializable]

    public class Intro

    {
        

        // these are the fields that will hold the data

        // we will gather with the form

        [Prompt("Could you tell me your name? {||}")]

        public string Name;

        

        [Prompt("What is your gender? {||}")]

        public Gender Gender;

        // This method 'builds' the form 

        // This method will be called by code we will place

        // in the MakeRootDialog method of the MessagesControlller.cs file



        public static IForm<Intro> BuildForm()

        {

            return new FormBuilder<Intro>()

                    .Message("Hi there! Welcome to CheerBot!")

                    .OnCompletion(async (context, introForm) =>

                    {

                    // Set BotUserData

                    context.PrivateConversationData.SetValue<bool>(

                            "is_done", true);

                        context.PrivateConversationData.SetValue<string>(

                            "Name", introForm.Name);

                        context.PrivateConversationData.SetValue<string>(

                            "Gender", introForm.Gender.ToString());

                    

                    await context.PostAsync("So, how's life going on? Open up to me - share your experience!!!");

                    })

                    .Build();

        }

    }

    // This enum provides the possible values for the 

    // Gender property in the ProfileForm class

    // Notice we start the options at 1 

    [Serializable]

    public enum Gender

    {

        Male = 1, Female = 2, Other=3

    };

}