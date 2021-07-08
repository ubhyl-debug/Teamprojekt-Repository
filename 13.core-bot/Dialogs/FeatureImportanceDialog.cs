// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

//imports for Get requests

using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Text;

//imports für Adaptive cards
using AdaptiveCards.Templating;
using AdaptiveCards;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class FeatureImportanceDialog : CancelAndHelpDialog
    {
        private const string DestinationStepMsgText = "";
        private const string OriginStepMsgText = "";
        private HttpClient client =new HttpClient();


        public FeatureImportanceDialog()
            : base(nameof(FeatureImportanceDialog))
        {   
              // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {   
                ShowFeatureImportanceAsync,
                DestinationStepAsync,
                TestCardStepAsync,
                HandleResponseAsync,
                TestCardStepAsync2, 
                OriginStepAsync,
                TravelDateStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowFeatureImportanceAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The feature importance plot can give useful information about which features are considered most important to predict the ouput of the model. This helps to understand if the model is based on important features in the business context.", inputHint: InputHints.IgnoringInput), cancellationToken);
            }


            // Create Adpative Card with Plot
            //var jsonData = (string) await client.GetStringAsync("https://49fa66626805.ngrok.io/explanation/featureimportance?count=10");
            //var jObject = JObject.Parse(jsonData);

            var jObject = await BOT_Api.getJson("/explanation/featureimportance?count=10");

            var templateJson="";
            using (var stream = GetType().Assembly.GetManifestResourceStream("CoreBot.Cards.PlotCard.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                     templateJson =  reader.ReadToEnd();
                     reader.Close();
                }
            };

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJson);

            var myData = new
            {

                Title= "Feature Importance Plot ",
                Url= (string) jObject["url"],
                Save_data = "Feature importance plot",
                Textexpl = "Ordered after the highest mean SHAP-Value (doesn't matter if negative or positive). The mean SHAP-Value is calculated by summing up all observations and dividing by the count of observations."

            };

            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(myData);

            var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            // Create the text prompt
            var opts = new PromptOptions
            {   
                
                Prompt = new Activity
                {   Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message,
                    Text = "", // You can comment this out if you don't want to display any text. Still works.
                }
            };

            
            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(TextPrompt), opts);    
        }



        private async Task<DialogTurnResult> DestinationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
        
            var jContext = JObject.Parse((string)stepContext.Result);
            var actionType = (string) jContext["action"];

            if (actionType == "SAVE") {
                // Send a POST request to the backend
                Dictionary<string,string> dict = new Dictionary<string, string>();
                dict.Add("url",(string) jContext["url"] );
                dict.Add("title",(string) jContext["title"] );
                dict.Add("text",(string) jContext["text"] );
                BOT_Api.saveToNotepad(dict);

                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad."));
            }

            if (actionType=="NEXT")  {
                return await stepContext.NextAsync("", cancellationToken);
            }

            if (actionType =="HELP") {

            }

            return await stepContext.NextAsync("", cancellationToken);
        }

        private async Task<DialogTurnResult> TestCardStepAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var templateJson="";
            using (var stream = GetType().Assembly.GetManifestResourceStream("CoreBot.Cards.testCard.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                     templateJson =  reader.ReadToEnd();
                     reader.Close();
                }
            };

            // Create a Template instance from the template payload
            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJson);
            // You can use any serializable object as your data
            using var client = new HttpClient();
            var content = (string) await client.GetStringAsync("https://49fa66626805.ngrok.io/explanation/featureimportance?count=10");
            var jObject = JObject.Parse(content);
            string output = (string)jObject["feature_importance_plot"];
            Console.WriteLine("********************" + (string) output);
            string save_data_str = "{ 'title': 'Feature Importance', 'url': '" +  (string)jObject["feature_importance_plot"] + "'}";
            var myData = new
            {

                Title= "Feature importance",
                Url= output,
                Save_data = save_data_str,
                Textexpl = "Ordered after the highest mean SHAP-Value (doesn't matter if negative or positive). The mean SHAP-Value is calculated by summing up all observations and dividing by the count of observations."

            };
            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(myData);
            System.Console.WriteLine("***************************TEST**************************************");
             var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            // Create the text prompt
            var opts = new PromptOptions
            {   
                
                Prompt = new Activity
                {   Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message,
                    Text = "waiting for user input...", // You can comment this out if you don't want to display any text. Still works.
                }
            };

            

             // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(TextPrompt), opts);

           /* return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions{ 
                Prompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(cardJson)

                })}, 
                cancellationToken);*/
        }
        
        private async Task<DialogTurnResult> HandleResponseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                    // Do something with step.result
                    // Adaptive Card submissions are objects, so you likely need to JObject.Parse(step.result)
                    System.Console.WriteLine("*****************TEST***********************************");
                    var jsonResponse = JObject.Parse((String)(stepContext.Result));
                    Console.WriteLine(jsonResponse);
                    if ((String)jsonResponse["test"]=="test") {
                        var values = new Dictionary<string, string>
                        {
                            { "thing1", "hello" },
                            { "thing2", "world" }
                        };
                        var client = new HttpClient();
                        var content = new FormUrlEncodedContent(values);

                        var response = await client.PostAsync("", content);

        var responseString = await response.Content.ReadAsStringAsync();
                    }
                    System.Console.WriteLine(stepContext.Result);
                    await stepContext.Context.SendActivityAsync($"INPUT: {stepContext.Result}");
                    System.Console.WriteLine("*****************TEST***********************************");
                    return await stepContext.NextAsync();
        }
        
        private async Task<DialogTurnResult> TestCardStepAsync2 (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var templateJson="";
            using (var stream = GetType().Assembly.GetManifestResourceStream("CoreBot.Cards.testCard.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                     templateJson =  reader.ReadToEnd();
                     reader.Close();
                }
            };

            // Create a Template instance from the template payload
            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJson);
            // You can use any serializable object as your data
            using var client = new HttpClient();
            var content = (string) await client.GetStringAsync("http://127.0.0.1:8085/explanation/global/featureimportance");
            var jObject = JObject.Parse(content);
            string output = (string)jObject["feature_importance_plot"];
            var myData = new
            {

                Title= "Feature importance",
                Url= output,
                Textexpl = "Ordered after the highest mean SHAP-Value (doesn't matter if negative or positive). The mean SHAP-Value is calculated by summing up all observations and dividing by the count of observations."


            };
            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(myData);
            System.Console.WriteLine("***************************TEST**************************************");
             var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
             
             var response = MessageFactory.Attachment(cardAttachment, ssml: "Welcome to Bot Framework!");    
            await stepContext.Context.SendActivityAsync(response, cancellationToken);
            return  await stepContext.NextAsync();

        }
        private async Task<DialogTurnResult> OriginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            var jsonResponse = JObject.Parse((String)(stepContext.Result));
                    if ((String)jsonResponse["test"]=="test") {
                        Console.WriteLine("*****************************");
                    }
            var bookingDetails = (BookingDetails)stepContext.Options;

            bookingDetails.Destination = (string)stepContext.Result;

            if (bookingDetails.Origin == null)
            {
                var promptMessage = MessageFactory.Text(OriginStepMsgText, OriginStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.Origin, cancellationToken);
        }

        private async Task<DialogTurnResult> TravelDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;

            bookingDetails.Origin = (string)stepContext.Result;

            if (bookingDetails.TravelDate == null || IsAmbiguous(bookingDetails.TravelDate))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.TravelDate, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.TravelDate, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;

            bookingDetails.TravelDate = (string)stepContext.Result;

            var messageText = $"Please confirm, I have you traveling to: {bookingDetails.Destination} from: {bookingDetails.Origin} on: {bookingDetails.TravelDate}. Is this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var bookingDetails = (BookingDetails)stepContext.Options;

                return await stepContext.EndDialogAsync(bookingDetails, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }



    }
}
