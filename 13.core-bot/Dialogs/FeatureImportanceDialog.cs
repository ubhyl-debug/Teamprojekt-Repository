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

//For user Prompt
using Microsoft.Bot.Builder.Dialogs.Choices;

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



        private async Task<DialogTurnResult> SelectedActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   

            Console.WriteLine("*****************************TESTE ADAPTIVE CARD******************");
            Console.WriteLine((string)stepContext.Result);
            var jContext = JObject.Parse((string)stepContext.Result);
            var actionType = (string) jContext["action"];
        
            if (actionType == "SAVE") {
                BOT_Api.jsonPostRequest((string)stepContext.Result, "/explanation/saveNotepad");
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad."));
            }

            if (actionType=="NEXT")  {
                return await stepContext.NextAsync("", cancellationToken);
            }

            if (actionType =="HELP") {

                stepContext.Context.SendActivityAsync(MessageFactory.Text("This is the place for the Feature ImportanceHelpDialog"));
                return await stepContext.BeginDialogAsync(nameof(FeatureImportanceHelpDialog), stepContext.Options, cancellationToken);
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




    }
}
