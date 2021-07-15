// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Net.Http;

//For user Prompt
using Microsoft.Bot.Builder.Dialogs.Choices;
using Luis;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly LuisXaiRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
    

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(LuisXaiRecognizer luisRecognizer, BookingDialog bookingDialog, FeatureImportanceDialog featureImportanceDialog, DirectionOfInfluenceNumDialog directionOfInfluenceNumDialog,
        DirectionOfInfluenceCatDialog directionOfInfluenceCatDialog, LocalWaterfallExplDialog localWaterfallExplDialog, ILogger<MainDialog> logger, 
        ConditionalShapDialog conditionalShapDialog, WhatIfDialog whatIfDialog, SimilarBookingsDialog similarBookingsDialog, FeatureImportanceHelpDialog featureImportanceHelpDialog,
        FeatureImportanceDialog1 featureImportanceDialog1, SaveDialog saveDialog, LocalExplanationDialog localExplanationDialog, UnexperiencedDialog unexperiencedDialog)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(bookingDialog);
            AddDialog(featureImportanceDialog);
            AddDialog(featureImportanceDialog1);
            AddDialog(directionOfInfluenceNumDialog);
            AddDialog(directionOfInfluenceCatDialog);
            AddDialog(localWaterfallExplDialog);
            AddDialog(conditionalShapDialog);
            AddDialog(whatIfDialog);
            AddDialog(saveDialog);
            AddDialog(similarBookingsDialog);
            AddDialog(localExplanationDialog);
            AddDialog(featureImportanceHelpDialog);
            AddDialog(unexperiencedDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                UserExperienceAsync,
                CheckUserExperienceAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
            
        }

        private async Task<DialogTurnResult> UserExperienceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   

            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }
    

            if (stepContext.Options != null ) {
                return await stepContext.NextAsync();
            }

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the user's response is received.
           
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter your level of experience."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Experienced", "Unexperienced"}),
                    Style = ListStyle.SuggestedAction,
                }, cancellationToken);

        }

        private async Task<DialogTurnResult> CheckUserExperienceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            
            var choice = ((FoundChoice)stepContext?.Result)?.Value;
            if (choice == "Unexperienced")  {
                Console.WriteLine("************Unexperienced Dialog started.....********************");
            return await stepContext.BeginDialogAsync(nameof(UnexperiencedDialog), "unexperienced", cancellationToken);
            }
           
            if (stepContext.Options == null) { 
                var promptMessage1 = MessageFactory.Text("What can i help you with today?",null, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage1 }, cancellationToken);
            }

            var promptMessage = MessageFactory.Text("What else can i help you with?",null, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

            
        }


        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {    
            Console.WriteLine("*********STEPCONTEXT:" + stepContext.Result);
            var luisResult2 = await _luisRecognizer.RecognizeAsync<XaiInteraction>(stepContext.Context, cancellationToken);


            switch(luisResult2.TopIntent().intent)
            {   

                case  XaiInteraction.Intent.SaveExplanation:
                        //User wants to save explanation
                    return await stepContext.BeginDialogAsync(nameof(SaveDialog), stepContext.Options, cancellationToken);

                case  XaiInteraction.Intent.FeatureImportance:


                    //User wants to get Feature Importance
                    string feature = luisResult2.FeatureImportanceParams.selectedFeature;
                    string number = luisResult2.FeatureImportanceParams.number;
                    string ordinal = luisResult2.FeatureImportanceParams.ordinal;
                    string ordinalInt = luisResult2.FeatureImportanceParams.ordinalInt;

                    Console.WriteLine("CHECK: LUIS RESPONSE");
                    Console.WriteLine(feature);
                    Console.WriteLine(number);
                    Console.WriteLine(ordinal);

                    var featureImportanceDetails = new FeatureImportanceDetails(){
                        Feature = feature,
                        number = number,
                        ordinal = ordinal,

                    };

                    return await stepContext.BeginDialogAsync(nameof(FeatureImportanceDialog1), featureImportanceDetails, cancellationToken);
                
                case XaiInteraction.Intent.LocalExplanation:
                    return await stepContext.BeginDialogAsync(nameof(LocalExplanationDialog), null, cancellationToken);
                    //User wants local explanation --> show waterfall plt



                default:
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult2.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    return await stepContext.NextAsync("NO_INTENT");
 

            }
        }

     

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   

            if (!(stepContext.Result is string)) {   
            Console.WriteLine(((ExplanationContext)stepContext.Result).title);
            var received_data = (((ExplanationContext)stepContext.Result));
            return await stepContext.ReplaceDialogAsync(InitialDialogId, received_data, cancellationToken);
            }

            return await stepContext.ReplaceDialogAsync(InitialDialogId, "NO_DATA", cancellationToken);
            // Restart the main dialog with a different message the second time around
            
        }
    }
}
