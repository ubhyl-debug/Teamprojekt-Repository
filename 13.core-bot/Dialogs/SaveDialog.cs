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
using System;
using System.Text;
using System.Text.Json;

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
    public class SaveDialog : CancelAndHelpDialog
    {   

       
        public SaveDialog(LuisXaiRecognizer luisRecognizer)
            : base(nameof(SaveDialog))
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
                SendPostAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SendPostAsync (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {       
            //Get Context
            ExplanationContext data_to_save = (ExplanationContext) stepContext.Options;

            string jsonData = JsonSerializer.Serialize(data_to_save);


            BOT_Api.jsonPostRequest(jsonData, "/explanation/saveNotepad");
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("The explanation was successfully saved to your Notepad."));

            return await stepContext.EndDialogAsync("dummy", cancellationToken);
        }


    }
}
