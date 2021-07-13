using Microsoft.BotBuilderSamples;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Web;  // also add a reference to System.web.dll for HttpUtility class to be found
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

//imports für Adaptive cards
using AdaptiveCards.Templating;
using AdaptiveCards;


public class CardCreator
{
    

    public static Attachment getCardAttachment (object data, string card)
    {       
            
         var templateJson="";
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(card))
            {
                using (var reader = new StreamReader(stream))
                {
                     templateJson =  reader.ReadToEnd();
                     reader.Close();
                }
            };

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJson);

            var myData = data;

            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(myData);

            var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };

            return cardAttachment;
    }

}