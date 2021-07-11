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
    public class WhatIfDialog : CancelAndHelpDialog
    
    {
             CardCreator c1 = new CardCreator();
        

        public WhatIfDialog()
            : base(nameof(WhatIfDialog))
        {   
              // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {   
                GetUserInputAsync,
                ShowResultStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
           
        }

        private async Task<DialogTurnResult> GetUserInputAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Erklärung was ist conditional SHAP CAT (nur für unexperienced)", inputHint: InputHints.IgnoringInput), cancellationToken);
            }

            

            var templateJson="";
            using (var stream = GetType().Assembly.GetManifestResourceStream("CoreBot.Cards.WhatIfCard.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                     templateJson =  reader.ReadToEnd();
                     reader.Close();
                }
            };

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJson);

           /** var myData = new
            {

                Title= "Direction of Influence (categorical Features)",
                Url= (string) jObject["url"],
                Save_data = "Direction of Infleunce (categorical Features)",
                Textexpl = "The plot shows ....."

            };

            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(myData);*/

            var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(templateJson),
            };
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



        private async Task<DialogTurnResult> ShowResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var jObject1 = BOT_Api.jsonPostRequest((string)stepContext.Result, "/explanation/whatif");

            //Send prediction card
            var myData1 = new
            {
                prediction=(string)jObject1["prediction"],
                higher= (Boolean) jObject1["higher"],
                difference="Difference:  " + (string) jObject1["difference"],
                deposit_type = (string) jObject1["booking"]["deposit_type"],
                agent=(string) jObject1["booking"]["agent"],
                country =(string) jObject1["booking"]["country"],
                special_requests =(string) jObject1["booking"]["total_of_special_requests"],
                lead_time=(string) jObject1["booking"]["lead_time"],
                customer_type=(string) jObject1["booking"]["customer_type"],
                parking_space=(string) jObject1["booking"]["required_car_parking_spaces"],
                previous_cancellations=(string) jObject1["booking"]["previous_cancellations"],
                arrival_date_week_number=(string) jObject1["booking"]["arrival_date_week_number"],
                booking_changes=(string) jObject1["booking"]["booking_changes"],
            };
            var cardAttachment1 = c1.getCardAttachment(myData1, "CoreBot.Cards.PredictionCard.json");
            await stepContext.Context.SendActivityAsync(
                    MessageFactory.Attachment(cardAttachment1));


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

                Title= "Conditional shap Values",
                Url= (string) jObject1["url"],
                Save_data = "Direction of Infleunce (categorical Features)",
                Textexpl = "The plot shows ....."

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
                    //Text = "", 
                }
            };

            
            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(TextPrompt), opts); 

            
        }
 
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                return await stepContext.BeginDialogAsync(nameof(LocalWaterfallExplDialog),stepContext.Options, cancellationToken);
            }

                return await stepContext.EndDialogAsync(null,cancellationToken);
        }

    }
}
