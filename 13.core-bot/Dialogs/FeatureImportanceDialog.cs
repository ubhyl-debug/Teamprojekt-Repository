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
using System.Linq;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class FeatureImportanceDialog : CancelAndHelpDialog
    {
        public FeatureImportanceDialog()
            : base(nameof(FeatureImportanceDialog))
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
                ShowFeatureImportanceAsync,
                SelectedActionStepAsync,
                ChooseDialogAsync,
                ChooseDialog2Async,
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
                },
                Choices = ChoiceFactory.ToChoices(new List<string>{"NEXT", "HELP", "SAVE"}),
                        // Don't render the choices outside the card
                        Style = ListStyle.None,
            };

            
            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(ChoicePrompt), opts);  


        }



        private async Task<DialogTurnResult> SelectedActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   

            Console.WriteLine("*****************************TESTE ADAPTIVE CARD******************");
            //Console.WriteLine((string)stepContext.Result);
            string res = (string)((FoundChoice)stepContext.Result).Value;
            Console.WriteLine("******" + res);
            //var jContext = JObject.Parse(res);
            //var actionType = (string) jContext["action"];

                var actionType = res;
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


        private async Task<DialogTurnResult> ChooseDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)

        {
                List<string> operationList = new List<string> { "Next (Direction of Influence (numerical features))", "Direction of Influence (categorical features)", "Conditional SHAP Values", "Skip to local Explanation" };
                // Create card

                var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                // Use LINQ to turn the choices into submit actions
                Actions = operationList.Select(choice => new AdaptiveSubmitAction
                    {
                        Title = choice,
                        Data = choice, // This will be a string
                    }).ToList<AdaptiveAction>(),

                };
                // Prompt
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        // Convert the AdaptiveCard to a JObject
                        Content = JObject.FromObject(card),
                    }),
                    Choices = ChoiceFactory.ToChoices(operationList),
                        // Don't render the choices outside the card
                        Style = ListStyle.None,
                },
                cancellationToken);

        }



            private async Task<DialogTurnResult> ChooseDialog2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)

            {
                  
                stepContext.Values["Operation"] = ((FoundChoice) stepContext.Result).Value;

                string operation = (string)stepContext.Values["Operation"];

                switch (operation)
                {
                    case "Next (Direction of Influence (numerical features))":
                        return await stepContext.NextAsync(null, cancellationToken);
                    case "Direction of Influence (categorical features)":
                        return await stepContext.ReplaceDialogAsync(nameof(DirectionOfInfluenceNumDialog), null, cancellationToken);
                    case "Conditional SHAP Values":
                        return await stepContext.ReplaceDialogAsync(nameof(ConditionalShapDialog), null, cancellationToken);
                    case "Skip to local Explanation":
                        return await stepContext.ReplaceDialogAsync(nameof(LocalWaterfallExplDialog), null, cancellationToken);
                    default:
                        await stepContext.Context.SendActivityAsync(
                         MessageFactory.Text("Sorry, I didn't get that. I continue with the next step.", inputHint: InputHints.IgnoringInput), cancellationToken);
                        return await stepContext.NextAsync(null, cancellationToken);

                }
            }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            if (stepContext.Options == "unexperienced") {
                return await stepContext.BeginDialogAsync(nameof(DirectionOfInfluenceNumDialog),stepContext.Options, cancellationToken);
            }
                Object res = new Object();
                    res="TESTEN DIALOG CONTEXT";
                return await stepContext.EndDialogAsync(res,cancellationToken);
        }




    }
}
