// Helps the user to understand the functions of the chatbot.


using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

//imports for Get requests

using System.Net.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Text.Json;
//imports für Adaptive cards
using AdaptiveCards.Templating;
using AdaptiveCards;

using System.IO;
using System.Collections.Generic;
using System.Net;

//For user Prompt
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Linq;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class UnexperiencedDialog : CancelAndHelpDialog
    {
        public UnexperiencedDialog()
            : base(nameof(UnexperiencedDialog))
        {   
              // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new DirectionOfInfluenceCatDialog());
            AddDialog(new ConditionalShapDialog());
            AddDialog(new LocalWaterfallExplDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {   
                IntroStepAsync,
                SimilarBookingsAsync,
                ShowFeatureImportanceAsync,
                DirectionOfInfluenceNum,
                DirectionOfInfluenceCat,
                PlotDirectionOfInfluenceCatAsync,
                ConditionalShapAsync,
                NextStepAsync,
                LocalWaterfallAsync,
                WhatIfAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken){

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Oh, you are a Newbie…  ...no problem!", inputHint: InputHints.IgnoringInput), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("If you want to know what I can do for you, please click on the question mark on the top-right", inputHint: InputHints.IgnoringInput), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Due to the fact that you don’t know what I can do, I will explain everything I know step by step to you.", inputHint: InputHints.IgnoringInput), cancellationToken);
            
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("These are the explanations I can provide. You can choose first one to get a step by step explanation or skip to the other explanations."),
                    Choices = ChoiceFactory.ToChoices(new List<string> {"Similar bookings", "Feature Importance","Direction of Influence (numerical)","Direction of Influence (categorical)","Conditional SHAP-values","local Waterfall explanation", "What-if-explanation"}),
                    Style = ListStyle.SuggestedAction,
                
                }, cancellationToken); 
        }
        private async Task<DialogTurnResult> SimilarBookingsAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
             // 1. Check input of last prompt
            var choice = ((FoundChoice)stepContext.Result).Value;


            // skip Dialogs according to selection
            switch (choice) {
                case "Feature Importance":
                    return await stepContext.NextAsync();
                case "Direction of Influence (numerical)":
                    stepContext.ActiveDialog.State["stepIndex"] = 2;
                    return await stepContext.NextAsync();
                case "Direction of Influence (categorical)" :
                    stepContext.ActiveDialog.State["stepIndex"] = 3;
                    return await stepContext.NextAsync();
                case "Conditional SHAP-values":
                    stepContext.ActiveDialog.State["stepIndex"] = 5;
                    return await stepContext.NextAsync();
                case "local Waterfall explanation":
                    stepContext.ActiveDialog.State["stepIndex"] = 7;
                    return await stepContext.NextAsync();
                case "What-if-explanation":
                    stepContext.ActiveDialog.State["stepIndex"] = 8;
                    return await stepContext.NextAsync();
                
            }
            
            
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("The similar bookings help you to understand the accuracy of the AI. With this information, you can build your own opinion about how you interpret the predicted risk.", inputHint: InputHints.IgnoringInput), cancellationToken);
            var jObject = await BOT_Api.getJson("/explanation/similarbookings");
            
            var myData = new
            {

                
                    count_of_similar_bookings= (string) jObject["count_of_similar_bookings"],
                    count_of_cancelled_bookings= jObject["count_of_canceled_bookings"],
                    count_of_not_cancelled_bookings = jObject["count_of_not_canceled_bookings"],

            };

            var cardAttachment = CardCreator.getCardAttachment(myData, "CoreBot.Cards.SimilarBookingsCard.json");
                
            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(
                cardAttachment,inputHint: InputHints.AcceptingInput),cancellationToken);
                
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter your next action."),
                    Choices = ChoiceFactory.ToChoices(new List<string> {"Save information","Continue with global explanation"}),
                    Style = ListStyle.SuggestedAction,
                
                }, cancellationToken);
        }
        private async Task<DialogTurnResult> ShowFeatureImportanceAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // 1. Check input of last prompt
            var choice = ((FoundChoice)stepContext?.Result)?.Value;
          
        
            if (choice == "Save information") {
                    //ExplanationContext data_to_save = (ExplanationContext) stepContext.Values["plot_data"];
                    //string jsonData = JsonSerializer.Serialize(data_to_save);


                //BOT_Api.jsonPostRequest(jsonData, "/explanation/saveNotepad");
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad. I continue with the next step."));
            }

            
            
            await stepContext.Context.SendActivityAsync(
            MessageFactory.Text("I sorted the features by their importance. This plot can give useful information about which features are considered as most important in the risk-prediction of the model.", inputHint: InputHints.IgnoringInput), cancellationToken);
            
            var jObject = await BOT_Api.getJson("/explanation/featureimportance?count=10");

            var myData = new
            {

                Title= "Feature Importance Plot ",
                Url= (string) jObject["url"],
                Save_data = "Feature importance plot",
                Textexpl = "Ordered after the highest mean SHAP-Value (doesn't matter if negative or positive). The mean SHAP-Value is calculated by summing up all observations and dividing by the count of observations."
            };

            
            var cardAttachment = CardCreator.getCardAttachment(myData, "CoreBot.Cards.PlotCard.json");
            // Create the text prompt

             await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);

             stepContext.Values["plot_data"]=new ExplanationContext {
                    url = (string) jObject["url"],
                    title = "Feature Importance Plot ",
                    text = "Ordered after the highest mean SHAP-Value (doesn't matter if negative or positive). The mean SHAP-Value is calculated by summing up all observations and dividing by the count of observations."
             };
            
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Save plot to Notepad", "Next step", "Skip to Direction of Influence (cat)"}),
                    Style = ListStyle.SuggestedAction,
                }, cancellationToken);

        }

        private async Task<DialogTurnResult> DirectionOfInfluenceNum(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            
            string actionType= (string)((FoundChoice)stepContext?.Result)?.Value;
        
            if (actionType == "Save plot to Notepad") {
                    ExplanationContext data_to_save = (ExplanationContext) stepContext.Values["plot_data"];
                    string jsonData = JsonSerializer.Serialize(data_to_save);


                BOT_Api.jsonPostRequest(jsonData, "/explanation/saveNotepad");
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad. I continue with the next step."));
            }

            if (actionType=="Skip to Direction of Influence (cat)")  {
                return await stepContext.NextAsync(null, cancellationToken);
            }

        
            await stepContext.Context.SendActivityAsync(
            MessageFactory.Text("Let’s continue with the next step! With the direction of influence, you can interpret in which direction the features influence the prediction. For example, let’s have a look at the feature booking_changes. The more the customer changes something in the booking, the lower the risk level. The higher the lead time the higher the risk of cancellation.", inputHint: InputHints.IgnoringInput), cancellationToken);
            

            var jObject = await BOT_Api.getJson("/explanation/directionofinfluence/num");


            var myData = new
            {

                Title= "Direction of Influence (numerical Features)",
                Url= (string) jObject["url"],
                Save_data = "Direction of Infleunce (numerical Features)",
                Textexpl = "The plot shows the correlation coefficient between the SHAP values and the feature values, which can take values between 0 and 1."

            };

            var cardAttachment = CardCreator.getCardAttachment(myData,"CoreBot.Cards.PlotCard.json" );

             await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);

             stepContext.Values["plot_data"]=new ExplanationContext {
                    url = (string) jObject["url"],
                    title = "Direction of influence",
                    text = "........"
             };
            
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Save plot to Notepad", "Next step","Skip to Conditional shap"}),
                    Style = ListStyle.SuggestedAction,
                }, cancellationToken);

          
        }

        private async Task<DialogTurnResult> DirectionOfInfluenceCat(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            
            string res = (string)((FoundChoice)stepContext?.Result)?.Value;
            
           
            if (res == "Save plot to Notepad") {
                    ExplanationContext data_to_save = (ExplanationContext) stepContext.Values["plot_data"];
                    string jsonData = JsonSerializer.Serialize(data_to_save);
                    BOT_Api.jsonPostRequest(jsonData, "/explanation/saveNotepad");
                    await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("The explanation was successfully saved to your Notepad. I continue with the next step."));
            }

            if (res=="Skip to Conditional shap")  {
                return await stepContext.NextAsync(); //ACHTUNG HIER ÜBERNÄCHSTER SCHRITT
            }

        
            await stepContext.Context.SendActivityAsync(
            MessageFactory.Text("The dataset also includes categorical variables for which the strength of the correlation cannot be calculated numerical. Therefore, a plot is created for these variables showing the average negative and positive SHAP values by category. In the next step, select which features you are interested in.", inputHint: InputHints.IgnoringInput), cancellationToken);
            
            var jObject = await BOT_Api.getJson("/explanation/directionofinfluence/num");

            var cardAttachment = CardCreator.getCardAttachment(null,"CoreBot.Cards.SelectFeaturesCard.json" );
             // Create the text prompt
            var opts = new PromptOptions
            {   
                
                Prompt = new Activity
                {   Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message,
                }
            };

            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(TextPrompt), opts);  
        }

        private async Task<DialogTurnResult> PlotDirectionOfInfluenceCatAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var jContext = JObject.Parse((string)stepContext.Result);

            var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,Boolean>>((string)stepContext.Result);

         
            foreach (var item in dict)
            {       
                if (item.Value) 
               {    
                    var jObject = await BOT_Api.getJson("/explanation/directionofinfluence/cat?feature=" + item.Key);
                    var myData = new
                    {

                        Title= "Direction of Influence (" + item.Key +" )",
                        Url= (string) jObject["url"],
                        Textexpl = "This plot shows the mean-positive- and the mean-negative-SHAP-values. The x-axis shows every possible value/influence of the feature " + item.Key + ". For every possible value there is shown the mean-positive- and the mean-negative-SHAP-value. Whenever the plot shows a strong expression, it has a strong influence on the prediction of the risk level."

                    };
                    var cardAttachment = CardCreator.getCardAttachment(myData,"CoreBot.Cards.PlotCard.json" );
                   // Create the text prompt
                    var response = MessageFactory.Attachment(cardAttachment);
                    // Display text
                   
                    await stepContext.Context.SendActivityAsync(response, cancellationToken);
               }
            }        
                      
                return await stepContext.PromptAsync(nameof(ChoicePrompt),new PromptOptions
                    {
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Next step","Skip to local Explanation"}),
                    Style = ListStyle.SuggestedAction,
                    }, cancellationToken);
                 
            
        }
                   
         private async Task<DialogTurnResult> ConditionalShapAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {       
            return await stepContext.BeginDialogAsync(nameof(ConditionalShapDialog),null,cancellationToken);    
                     
        }

        private async Task<DialogTurnResult> NextStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {       
                stepContext.Values["plot_data"] = stepContext.Result;
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Save plot to Notepad","Next step","Skip to What if"}),
                    Style = ListStyle.SuggestedAction,
                    }, cancellationToken);
        }
        private async Task<DialogTurnResult> LocalWaterfallAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            string actionType= (string)((FoundChoice)stepContext?.Result)?.Value;
        
            if (actionType == "Save plot to Notepad") {
                    ExplanationContext data_to_save = (ExplanationContext) stepContext.Values["plot_data"];
                    string jsonData = JsonSerializer.Serialize(data_to_save);


                BOT_Api.jsonPostRequest(jsonData, "/explanation/saveNotepad");
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad. I continue with the next step."));
            }

            if (actionType=="Skip to What if")  {
                return await stepContext.NextAsync("", cancellationToken);
            }


            await stepContext.Context.SendActivityAsync(
            MessageFactory.Text("In the other steps, you could judge whether the overall model makes sense. Now we want to have a look at one specific booking."));
            //Get Context
            var id = await BOT_Api.getJson("/explanation/getlastprediction");
            var jObject= await BOT_Api.getJson("/explanation/local/waterfall?id=" + (string) id["booking"][0]["booking_normal"]["index"]);


            var myData = new 
            {

                Title= "Waterfall plot for Booking " + (string) id["booking"][0]["booking_normal"]["index"],
                Url= (string) jObject["url"],
                Textexpl = "Therefore, each feature has a step-by-step influence. It starts at the bottom at ??? (....)=. Depending on the direction and the extent, the prediction is based on the sum of the features."
            };

            ExplanationContext data =  new ExplanationContext {
                    UserExperience = "experienced",
                    url = (string) jObject["url"],
                    title = "Waterfall plot for Booking " + (string) id["booking"][0]["booking_normal"]["index"],
                    text = "Therefore, each feature has a step-by-step influence. It starts at the bottom at ??? (....)=. Depending on the direction and the extent, the prediction is based on the sum of the features." 
                    };

            
            
            
            var cardAttachment = CardCreator.getCardAttachment(myData, "CoreBot.Cards.PlotCardExperienced.json");
            // Create the text prompt
            var response = MessageFactory.Attachment(cardAttachment);
            
            await stepContext.Context.SendActivityAsync(response, cancellationToken);
            
    
            
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Save plot to Notepad", "Next step"}),
                    Style = ListStyle.SuggestedAction,
                }, cancellationToken);

          
        }

        private async Task<DialogTurnResult> WhatIfAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            string actionType= (string)((FoundChoice)stepContext?.Result)?.Value;
        
            if (actionType == "Save plot to Notepad") {
                    ExplanationContext data_to_save = (ExplanationContext) stepContext.Values["plot_data"];
                    string jsonData = JsonSerializer.Serialize(data_to_save);


                BOT_Api.jsonPostRequest(jsonData, "/explanation/saveNotepad");
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad. I continue with the next step."));
            }

            return await stepContext.BeginDialogAsync(nameof(WhatIfDialog));
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
                Console.WriteLine("******************JETZT AM ENDE");
                Object res = new Object();
                    res="TESTEN DIALOG CONTEXT";
                return await stepContext.EndDialogAsync(res,cancellationToken);
        }




    }
}
