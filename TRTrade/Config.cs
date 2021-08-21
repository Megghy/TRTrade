using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.Hooks;

namespace TRTrade
{
    public class Config
    {
        public void Load(ReloadEventArgs args = null)
        {
            if (!File.Exists(Path.Combine(TShock.SavePath, "TRTrade.json")))
            {
                BroadcastTextDescription =  Localization.GetText("Config_BroadcastTextDescription", false);
                EnablePEFeatureDescription = Localization.GetText("Config_EnablePEFeatureDescription", false);
                TaxRateDescription = Localization.GetText("Config_TaxRateDescription", false);
                TypeDescription = Localization.GetText("Config_TypeDescription", false);
                BroadcastText = Localization.GetText("Config_DefaultBroadcastText", false);
                LanguageDescription = Localization.GetText("Config_LanguageDescription", false);
                FileTools.CreateIfNot(Path.Combine(TShock.SavePath, "TRTrade.json"), JsonConvert.SerializeObject(TRTrade.Config, Formatting.Indented));
            }
            try
            {
                TRTrade.Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(TShock.SavePath, "TRTrade.json")));
                TRTrade.Config.AutoLanguage = System.Threading.Thread.CurrentThread.CurrentCulture.Name.ToLower().Substring(0, 2) switch
                {
                    "zh" => LanguageType.Chinese,
                    "es" => LanguageType.Spanish,
                    _ => LanguageType.English,
                };
                TShock.Log.ConsoleInfo(Localization.GetText("Log_LoadConfig", false).Replace("{TRTrade.Config.Type}", TRTrade.Config.Type.ToString()).Replace("{TRTrade.Config.TaxRate}", TRTrade.Config.TaxRate.ToString()));
            }
            catch (Exception ex){ TShock.Log.Error(ex.Message); TShock.Log.ConsoleError(Localization.GetText("Log_LoadConfigFail")); }
        }
        public long AfterTax(long money)
        {
            return (long)(money * (1 - Rate()));
        }
        public double Rate()
        {
            if (TaxRate < 0)
            {
                return 0;
            }
            else if (TaxRate >= 100)
            {
                return 1;
            }
            else
            {
                return TaxRate / 100;
            }
        }
        public enum MoneyType
        {
            SEconomy,
            BeanPoint,
            POBC
        }
        public enum LanguageType
        {
            Auto,
            Chinese,
            English,
            Spanish
        }
        public LanguageType AutoLanguage;
        [JsonProperty]
        public bool EnablePEFeature = false;
        [JsonProperty]
        public string EnablePEFeatureDescription;
        [JsonProperty]
        public MoneyType Type = MoneyType.SEconomy;
        [JsonProperty]
        public string TypeDescription;
        [JsonProperty]
        public double TaxRate = 0;
        [JsonProperty]
        public string TaxRateDescription;
        [JsonProperty]
        public bool Broadcast = true;
        [JsonProperty]
        public string BroadcastTextDescription;
        [JsonProperty]
        public string BroadcastText;
        [JsonProperty]
        public LanguageType Language = LanguageType.Auto;
        [JsonProperty]
        public string LanguageDescription;
    }
}
