using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace Luis
{
    public partial class XaiInteraction: IRecognizerConvert
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("alteredText")]
        public string AlteredText;

        public enum Intent {
            ConditionalShap,
            DirectionOfInfluence,
            FeatureImportance,
            LocalExplanation,
            None,
            SaveExplanation,
            WhatIf
        };
        [JsonProperty("intents")]
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] HighOrdinal;
            public string[] LowOrdinal;

            // Built-in entities
            public double[] number;
            public double[] ordinal;

            // Lists
            public string[][] feature;


            // Composites
            public class _InstanceImportanceRequest
            {
                public InstanceData[] Number;
                public InstanceData[] Ordinal;
            }
            public class ImportanceRequestClass
            {
                public string[] Number;
                public string[] Ordinal;
                [JsonProperty("$instance")]
                public _InstanceImportanceRequest _instance;
            }
            public ImportanceRequestClass[] ImportanceRequest;

            // Instance
            public class _Instance
            {
                public InstanceData[] HighOrdinal;
                public InstanceData[] ImportanceRequest;
                public InstanceData[] LowOrdinal;
                public InstanceData[] Number;
                public InstanceData[] Ordinal;
                public InstanceData[] feature;
                public InstanceData[] number;
                public InstanceData[] ordinal;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        [JsonProperty("entities")]
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<XaiInteraction>(
                JsonConvert.SerializeObject(
                    result,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Error = OnError }
                )
            );
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        private static void OnError(object sender, ErrorEventArgs args)
        {
            // If needed, put your custom error logic here
            Console.WriteLine(args.ErrorContext.Error.Message);
            args.ErrorContext.Handled = true;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
