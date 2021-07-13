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
    public class LocalWaterfallExplDialog : CancelAndHelpDialog
    {

        public LocalWaterfallExplDialog()
            : base(nameof(LocalWaterfallExplDialog))
        {   
              // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {   
                ShowPlotAsync,
                SelectedActionStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowPlotAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The last steps showed that the model itself makes sense. This was called the global interpretability. Next I want to explain why one specific booking receives the prediction according to its feature values. This is called the local interpretability. The waterfall plot is able to visualize this explanation. ", inputHint: InputHints.IgnoringInput), cancellationToken);
            }

            var jObject = await BOT_Api.getJson("/explanation/local/waterfall?id=70100");

            Console.WriteLine("*****************************************WATERFALLPLOT" + (string)jObject["url"]);

            var myData = new
            {

                Title= "Waterfall",
                Url= (string) jObject["url"],
                Save_data = "Waterfall",
                Textexpl = "The plot shows ....."

            };

            var cardAttachment = CardCreator.getCardAttachment(myData,"CoreBot.Cards.PlotCard.json");
            
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
                BOT_Api.jsonPostRequest((string)stepContext.Result, "/explanation/saveNotepad");
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad."));
            }

            if (actionType=="NEXT")  {
                return await stepContext.NextAsync("", cancellationToken);
            }

            if (actionType =="HELP") {

                return await stepContext.NextAsync("Ende der HELP", cancellationToken);
            }

            return await stepContext.NextAsync("", cancellationToken);
        }
 
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                return await stepContext.BeginDialogAsync(nameof(WhatIfDialog),stepContext.Options, cancellationToken);
            }

                return await stepContext.EndDialogAsync(null,cancellationToken);
        }

    }
}
