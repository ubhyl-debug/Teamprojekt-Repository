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
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class SimilarBookingsDialog : CancelAndHelpDialog
    {       
        public SimilarBookingsDialog()
            : base(nameof(SimilarBookingsDialog))
        {   
              // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {   
                ShowCardAsync,
                InputActionStepAsync,
                SelectedActionStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowCardAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Erkärung was bringt similar bookings?", inputHint: InputHints.IgnoringInput), cancellationToken);
            }


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
                
            return await stepContext.NextAsync();    
        }


        private async Task<DialogTurnResult> InputActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter your next action."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Additional explanation", "Save information","Continue with global explanation"}),
                    Style = ListStyle.SuggestedAction,
                }, cancellationToken);
        }


        private async Task<DialogTurnResult> SelectedActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = ((FoundChoice)stepContext.Result).Value;

            if (choice == "Save information") {
                // Send a POST request to the backend
       
                //BOT_Api.saveToNotepad(dict);

                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad."));
            }

            if (choice=="Continue with global explanation")  {
                return await stepContext.NextAsync("", cancellationToken);
            }

            if (choice =="Additional explanation") {

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("The similar bookings are:"), cancellationToken);
                
         
                
            }

            return await stepContext.NextAsync("", cancellationToken);
        }




        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                return await stepContext.BeginDialogAsync(nameof(FeatureImportanceDialog),stepContext.Options, cancellationToken);
            }

                return await stepContext.EndDialogAsync(null,cancellationToken);
        }



    }
}
