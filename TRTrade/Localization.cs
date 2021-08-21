using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TRTrade
{
    class Localization
    {
        public static JObject Chinese = JObject.Parse(Properties.Resources.Chinese);
        public static JObject English = JObject.Parse(Properties.Resources.English);
        public static JObject Spanish = JObject.Parse(Properties.Resources.Spanish);

        public static string GetText(string name, bool withheader = true)
        {
            try
            {
                switch (TRTrade.Config.Language == Config.LanguageType.Auto ? TRTrade.Config.AutoLanguage : TRTrade.Config.Language)
                {
                    case Config.LanguageType.Chinese:
                        return $"{(withheader ? Chinese["PromptHeader"].ToString() : "")} {Chinese[name]}";
                    case Config.LanguageType.Spanish:
                        return $"{(withheader ? Spanish["PromptHeader"].ToString() : "")} {Spanish[name]}";
                    default:
                        return $"{(withheader ? English["PromptHeader"].ToString() : "")} {English[name]}";
                }
            }
            catch { return "null"; }
        }
    }
}
