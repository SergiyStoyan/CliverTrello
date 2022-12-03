//********************************************************************************************
//Author: Sergiy Stoyan
//        systoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Cliver
{
    partial class Trello
    {
        public string GetTrelloBoardId(string boardName)
        {
            Log.Debug(Log.GetThisMethodName() + "...");

            JToken o = Get("search", new Dictionary<string, string> { { "query", boardName } }
                , (JToken t) =>
                {
                    if (t["boards"] == null)
                        throw new System.Net.WebException("trello returned a malformed result.");
                }
            );
            JArray bs = (JArray)o["boards"];
            return (string)bs.FirstOrDefault(a => (string)a["name"] == boardName)["id"];
        }
    }
}
