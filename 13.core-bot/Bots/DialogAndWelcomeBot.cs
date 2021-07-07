// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AdaptiveCards.Templating;
using System.Net.Http;
using Newtonsoft.Json.Linq;


namespace Microsoft.BotBuilderSamples.Bots
{
    public class DialogAndWelcomeBot<T> : DialogBot<T>
        where T : Dialog
    {
        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeCard = CreateAdaptiveCardAttachment();
                    var response = MessageFactory.Attachment(welcomeCard, ssml: "Welcome to Bot Framework!");
                    await turnContext.SendActivityAsync(response, cancellationToken);
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }

        // Load attachment from embedded resource.
       private Attachment CreateAdaptiveCardAttachment()
        {
            var templateJson="";
            using (var stream = GetType().Assembly.GetManifestResourceStream("CoreBot.Cards.testCard.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                     templateJson =  reader.ReadToEnd();
                     reader.Close();
                }
            };

            // Create a Template instance from the template payload
            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJson);
            // You can use any serializable object as your data
            var myData = new
            {

                Title = "Feature Importance",
                Url = "https://quickchart.io/chart/render/zm-f26c07ef-18b2-41a8-a005-b8fc3fa7fee6?data1=1.799,0.972,0.718,0.547,0.327,0.273,0.208,0.195,0.181,0.135,&labels=deposit_type,agent,country,total_of_special_requests,lead_time,customer_type,required_car_parking_spaces,previous_cancellations,arrival_date_week_number,booking_changes"
            };

            // "Expand" the template - this generates the final Adaptive Card payload
                string cardJson = template.Expand(myData);

            var cardResourcePath = "CoreBot.Cards.welcomeCard_new.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
        private async Task<string> fetchData()
            {
             using var client = new HttpClient();
                var content = (string) await client.GetStringAsync("http://127.0.0.1:8085/explanation/booking?id=76189");
                var jObject = JArray.Parse(content);
                //Console.WriteLine(content);
                string output = (string)jObject[0]["booking"]["index"];
                return output;
            }
    }
}
