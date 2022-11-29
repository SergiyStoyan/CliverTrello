//********************************************************************************************
//Author: Sergiy Stoyan
//        systoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Cliver
{
    public partial class Trello : IDisposable
    {
        ~Trello()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (webClient != null)
                {
                    webClient.Dispose();
                    webClient = null;
                }
            }
        }

        public static void ObtainApiKey()
        {
            System.Diagnostics.Process.Start("https://trello.com/app-key");
        }

        public static void ObtainUserToken(string apiKey)
        {
            Dictionary<string, string> names2value = new Dictionary<string, string> { { "expiration", "never" }
                 ,{ "name",Log.ProgramName }
                 ,{ "scope", "read" }
                 ,{ "response_type", "token" }
                 ,{ "key", apiKey }
                };
            System.Diagnostics.Process.Start("https://trello.com/1/authorize?" + WebRoutines.GetUrlQuery(names2value));
        }

        public Trello(string appKey, string serverToken, int networkErrorTryMaxCount, int networkErrorRetryDelaySecs)
        {
            this.appKey = appKey;
            this.serverToken = serverToken;
            webClient.BaseAddress = "https://api.trello.com/1/";
            webClient.Encoding = System.Text.Encoding.UTF8;
            trelloTrier.RetryMaxCount = networkErrorTryMaxCount;
            trelloTrier.RetryDelaySecs = networkErrorRetryDelaySecs;
        }
        Cliver.WebClient webClient = new WebClient();
        string appKey;
        string serverToken;
        TrelloTrier trelloTrier = new TrelloTrier();

        public class TrelloTrier : Trier
        {
            override public int RetryMaxCount { get; set; }
            public int RetryDelaySecs;

            override protected bool proceedOnException(Exception e)
            {
                List<Type> ignoredExceptionTypes = new List<Type> {
                    typeof(System.Net.WebException)//internet connection problem 
                    , typeof(Newtonsoft.Json.JsonReaderException) //it may sometimes happen: Unexpected character encountered while parsing value: R. Path '', line 0, position 0. [Newtonsoft.Json.JsonReaderException]
                };
                return ignoredExceptionTypes?.Find(a => e.GetType() == a) != null;
            }

            override protected void onRetry(Exception e)
            {
                Log.Warning("Retrying (" + retryCount + ")... Sleeping " + RetryDelaySecs + " secs", e);
                //MainForm.This.Table.SetProgressTask("Sleeping " + Settings.General.NetworkErrorRetryDelaySecs + " secs...", System.Drawing.Color.LightPink);
                System.Threading.Thread.Sleep(RetryDelaySecs * 1000);
                //MainForm.This.Table.SetProgressTask(null);
            }
        }

        public JToken Get(string address, Dictionary<string, string> keys2value = null, Action<JToken> validate = null)
        {
            //Log.Debug(Log.GetThisMethodInfo(address, keys2value));
            webClient.QueryString.Clear();
            webClient.QueryString.Add("key", appKey);
            webClient.QueryString.Add("token", serverToken);
            if (keys2value != null)
                foreach (string k in keys2value.Keys)
                    webClient.QueryString.Add(k, keys2value[k]);
            //webClient.Headers[System.Net.HttpRequestHeader.Authorization] = "OAuth oauth_consumer_key=\"" + appKey + "\", oauth_token=\"" + serverToken + "\"";
            return trelloTrier.Perform(() =>
            {
                string s = webClient.DownloadString(address);
                Log.Debug("Response:\r\n" + s);
                JToken t = JToken.Parse(s);
                if (t == null)
                    throw new System.Net.WebException("trello returned NULL result.");
                validate?.Invoke(t);
                return t;
            }
             );
        }

        public string Download(string address, string file)
        {
            webClient.QueryString.Clear();
            webClient.Headers[System.Net.HttpRequestHeader.Authorization] = "OAuth oauth_consumer_key=\"" + appKey + "\", oauth_token=\"" + serverToken + "\"";
            return trelloTrier.Perform(() =>
            {
                webClient.DownloadFile(address, file);
                if (webClient.Response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception("WebClient response " + webClient.Response.StatusDescription + " while downloading " + address, webClient.Exception);
                return file;
            }
            );
        }
    }
}