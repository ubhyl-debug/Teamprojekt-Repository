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
                SelectedActionStepAsync,
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



        private async Task<DialogTurnResult> SelectedActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
        
            var jContext = JObject.Parse((string)stepContext.Result);
            var actionType = (string) jContext["action"];
            Console.WriteLine(jContext);
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

                int counter = 0;

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("The ten most important features are:"), cancellationToken);
                var jObject = await BOT_Api.getJson("/explanation/featureimportance?count=10"); // JObject.Parse(content1);
              
                while (counter < 5)

                {

                    Console.WriteLine(jObject["values"]);
                    var output1 = (string)jObject["values"][counter];

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(output1));
                    
                    counter++;

                }
                
            }

            return await stepContext.NextAsync("", cancellationToken);
        }




        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                return await stepContext.BeginDialogAsync(nameof(DirectionOfInfluenceNumDialog),stepContext.Options, cancellationToken);
            }

                return await stepContext.EndDialogAsync(null,cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }



    }
}
