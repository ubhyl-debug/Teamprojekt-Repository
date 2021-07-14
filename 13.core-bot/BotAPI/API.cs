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

public class BOT_Api
{
    public static HttpClient client = new HttpClient();
    public static string ngrokendpoint = " https://47e424da9721.ngrok.io";

    public static async Task<JObject> getJson (string route)
    {       
            
            var jsonString = (string) await client.GetStringAsync(ngrokendpoint + route);
            var jObject = JObject.Parse(jsonString);
            return jObject;
    }


    public static void saveToNotepad (Dictionary<string,string> dict)
    {
        var url = ngrokendpoint + "/explanation/saveNotepad";
        var httpRequest = (HttpWebRequest)WebRequest.Create(url);
        httpRequest.ContentType = "application/x-www-form-urlencoded";
        httpRequest.Method = "POST";

        var data ="";
        foreach( KeyValuePair<string, string> kvp in dict )
        {
            data = data + "&" + kvp.Key + "=" + kvp.Value;
            Console.WriteLine(data);
        }

        using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }

        var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }

        Console.WriteLine(httpResponse.StatusCode);
    }
    

    public static JObject jsonPostRequest(String jsonString, String route) {
        var httpWebRequest = (HttpWebRequest)WebRequest.Create(ngrokendpoint + route);
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            string json =  jsonString;
            streamWriter.Write(json);
        }

        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        {
            var result = streamReader.ReadToEnd();
            return JObject.Parse(result);
        }

        
    }

 
}
