// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs.Choices;
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
    public class DirectionOfInfluenceCatDialog : CancelAndHelpDialog
    {

        public DirectionOfInfluenceCatDialog()
            : base(nameof(DirectionOfInfluenceCatDialog))
        {   
              // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {   AskForFeatureAsync,
                ShowPlotAsync,
                SelectedActionStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskForFeatureAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            if (stepContext.Options == "unexperienced") {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The data set also includes categorical variables for which the strength of the correlation cannot be calculated as before. Therefore, a plot is created for these variables showing the average negative and positive SHAP values by category. In the next step, select which features you are interested in.", inputHint: InputHints.IgnoringInput), cancellationToken);
            }
            var cardAttachment = CardCreator.getCardAttachment(null,"CoreBot.Cards.SelectFeaturesCard.json" );
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

        private async Task<DialogTurnResult> ShowPlotAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            Console.WriteLine("*****************************TESTE ADAPTIVE CARD******************");
            Console.WriteLine((string)stepContext.Result);

            var jContext = JObject.Parse((string)stepContext.Result);

            var dict = JsonConvert.DeserializeObject<Dictionary<string,Boolean>>((string)stepContext.Result);

            // Count of true values to display last message as prompt.
                var count = 0;
            foreach(var element in dict) {
                if(element.Value){
                    count++;
                }
            }

            if (count == 0) {
                if (stepContext.Options == "unexperienced") {
                return await stepContext.BeginDialogAsync(nameof(ConditionalShapDialog),stepContext.Options, cancellationToken);
            }

                return await stepContext.EndDialogAsync(null,cancellationToken);
            }
            var count2 = 0;
            foreach (var item in dict)
            {
               
               if (item.Value) 
               {    
                   count2++;
                    var jObject = await BOT_Api.getJson("/explanation/directionofinfluence/cat?feature=" + item.Key);

                    var myData = new
                    {

                        Title= "Direction of Influence (" + item.Key +" )",
                        Url= (string) jObject["url"],
                        Save_data ="Direction of Influence (" + item.Key +" )",
                        Textexpl = "The plot shows ....."

                    };

                    var cardAttachment = CardCreator.getCardAttachment(myData,"CoreBot.Cards.PlotCard.json" );
                   // Create the text prompt
                    var response = MessageFactory.Attachment(cardAttachment);

                    // Display text
                    if (count2 < count) {
                    await stepContext.Context.SendActivityAsync(response, cancellationToken);
                    }
                    else {
                        //last plot
                        // Create the text prompt
                        var opts = new PromptOptions
                        {   
                
                        Prompt = new Activity
                        {   Attachments = new List<Attachment>() { cardAttachment },
                            Type = ActivityTypes.Message
                        }
                        };

            
                        // Display a Text Prompt and wait for input
                        return await stepContext.PromptAsync(nameof(TextPrompt), opts);  

                    } 
                }

                   
 
            }

             return await stepContext.NextAsync(nameof(TextPrompt), cancellationToken);
             
        
        }
            

            

            

            
        



        private async Task<DialogTurnResult> SelectedActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
        
            var jContext = JObject.Parse((string)stepContext.Result);
            var actionType = (string) jContext["action"];
            Console.WriteLine(jContext);
            if (actionType == "SAVE") {
          
                BOT_Api.jsonPostRequest((string)stepContext.Result, "/explanation/saveNotepad");

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
              
                while (counter < 10)

                {


                    Console.WriteLine(jObject["values"]);
                    var output1 = jObject["values"][counter];

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text((counter + 1) + ". " + output1));

                    counter++;

                }
                return await stepContext.NextAsync("Ende der HELP", cancellationToken);
            }

            return await stepContext.NextAsync("", cancellationToken);
        }
 
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options == "unexperienced") {
                return await stepContext.BeginDialogAsync(nameof(ConditionalShapDialog),stepContext.Options, cancellationToken);
            }

                return await stepContext.EndDialogAsync(null,cancellationToken);
        }

    }
}
