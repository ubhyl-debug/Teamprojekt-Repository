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
    public class FeatureImportanceHelpDialog : CancelAndHelpDialog
    {
        
        private HttpClient client =new HttpClient();


        public FeatureImportanceHelpDialog()
            : base(nameof(FeatureImportanceHelpDialog))
        {   
              // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {   
                ShowHelpAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowHelpAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            await stepContext.Context.SendActivityAsync(
            MessageFactory.Text("You have selected the Help Dialog for the Feature-Importance-Plot. Please type in the information you want to know to get a more detailed understanding.",inputHint: InputHints.IgnoringInput), cancellationToken);
        

            return await stepContext.NextAsync("",cancellationToken);
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
