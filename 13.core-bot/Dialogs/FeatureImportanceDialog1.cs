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
    public class FeatureImportanceDialog1 : CancelAndHelpDialog
    {   

        private readonly LuisXaiRecognizer _luisRecognizer;
        public FeatureImportanceDialog1(LuisXaiRecognizer luisRecognizer)
            : base(nameof(FeatureImportanceDialog1))
        {   

            _luisRecognizer = luisRecognizer;
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
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowFeatureImportanceAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {       
            //Get Context
            var featureImportanceDetails = (FeatureImportanceDetails) stepContext.Options;
            var jObject= await BOT_Api.getJson("/explanation/featureimportance?count=10");

            

            Console.WriteLine("CHECK: LUIS RESPONSE");
                    Console.WriteLine(featureImportanceDetails.Feature);
                    Console.WriteLine(featureImportanceDetails.number);
                    Console.WriteLine(featureImportanceDetails.ordinal);

            if (featureImportanceDetails.Feature != null) {
                //User requested one feature

                jObject= await BOT_Api.getJson("/explanation/featureimportance?feature=" + featureImportanceDetails.Feature);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                "The feature " + jObject["feature"] + " is the " + jObject["position"] + " most important feature out of a total of 27 features.", inputHint: InputHints.IgnoringInput), cancellationToken);
                return await stepContext.NextAsync();
            }  
            else if (featureImportanceDetails.number != null && featureImportanceDetails.ordinal == "high") jObject = await BOT_Api.getJson("/explanation/featureimportance?count=" + featureImportanceDetails.number);
            else if (featureImportanceDetails.number != null && featureImportanceDetails.ordinal == "low") jObject = await BOT_Api.getJson("/explanation/featureimportance?count=" + featureImportanceDetails.number);
            else if (featureImportanceDetails.ordinalInt != null) {
                jObject= await BOT_Api.getJson("/explanation/featureimportance?position=" + featureImportanceDetails.ordinalInt);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                "The feature " + jObject["feature"] + " is the " + jObject["position"] + " most important feature out of a total of 27 features.", inputHint: InputHints.IgnoringInput), cancellationToken);
                return await stepContext.NextAsync();
            }
            else {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("I just understood that you want to know the feature importance, but didn't got your specified parameters. I'll display the default plot.", inputHint: InputHints.IgnoringInput), cancellationToken);
            }

            var myData = new 
            {

                Title= "Feature Importance Plot ",
                Url= (string) jObject["url"],
                Save_data = "Feature importance plot",
                Textexpl = "Ordered after the highest mean SHAP-Value (doesn't matter if negative or positive). The mean SHAP-Value is calculated by summing up all observations and dividing by the count of observations."
            };

            ExplanationContext data =  new ExplanationContext {
                    UserExperience = "experienced",
                    url = (string) jObject["url"],
                    title = "Feature Importance Plot ",
                    text = "Ordered after the highest mean SHAP-Value (doesn't matter if negative or positive). The mean SHAP-Value is calculated by summing up all observations and dividing by the count of observations."
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
