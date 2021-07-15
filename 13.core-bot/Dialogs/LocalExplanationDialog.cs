// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

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
    public class LocalExplanationDialog : CancelAndHelpDialog
    {   

        public LocalExplanationDialog()
            : base(nameof(LocalExplanationDialog))
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
                ShowWaterfallAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowWaterfallAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {       
            //Get Context
            var id = await BOT_Api.getJson("/explanation/getlastprediction");
            var jObject= await BOT_Api.getJson("/explanation/local/waterfall?id=" + (string) id["booking"][0]["booking_normal"]["index"]);


            var myData = new 
            {

                Title= "Waterfall plot for Booking " + (string) id["booking"][0]["booking_normal"]["index"],
                Url= (string) jObject["url"],
                Textexpl = "Plot shows...."
            };

            ExplanationContext data =  new ExplanationContext {
                    UserExperience = "experienced",
                    url = (string) jObject["url"],
                    title = "Waterfall plot for Booking " + (string) id["booking"][0]["booking_normal"]["index"],
                    text = "Plot shows...."
                    };

            
            
            
            var cardAttachment = CardCreator.getCardAttachment(myData, "CoreBot.Cards.PlotCardExperienced.json");
            // Create the text prompt
            var response = MessageFactory.Attachment(cardAttachment);
            
            await stepContext.Context.SendActivityAsync(response, cancellationToken);
            
    
            //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            return await stepContext.EndDialogAsync(data,cancellationToken); 
        }


    }
}
