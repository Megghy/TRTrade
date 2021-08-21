using BeanPoints;
using POBC2;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using Wolfje.Plugins.SEconomy;
using Wolfje.Plugins.SEconomy.Journal;
using static TRTrade.Localization;

namespace TRTrade
{
    public class Utils
    {
        public class TItem
        {
            public TItem(int id, int userid, Item item, long price, DateTime date)
            {
                ID = id;
                UserID = userid;
                Item = item;
                Price = price;
                AddDate = date;
            }
            /// <summary>
            /// 物品独有的ID
            /// </summary>
            public int ID;
            /// <summary>
            /// 商品所属的玩家ID
            /// </summary>
            public int UserID;
            /// <summary>
            /// 将出售的物品
            /// </summary>
            public Item Item;
            /// <summary>
            /// 物品价格
            /// </summary>
            public long Price;
            /// <summary>
            /// 
            /// </summary>
            public DateTime AddDate;
        }

        internal class EconomyFamework
        {
            public static bool BeanPoint_Change(string name, long money)
            {
                if (DbExt.Query(TShock.DB, $"UPDATE bpplayers SET Money = Money {(money < 0 ? "-" : "+")} @0 WHERE Name=@1", new object[] { money < 0 ? -money : money, name }) > 0)
                {
                    return true;
                }
                else
                {
                    TShock.Log.ConsoleError(GetText("Log_AccountNotFound", false).Replace("{name}", name));
                    return false;
                }
            }

            public static bool SEconomy_Change(string name, long money, Item item)
            {
                Money m = Money.Parse(money < 0L ? (-money).ToString() : money.ToString());
                IBankAccount selectedAccount = SEconomyPlugin.Instance.RunningJournal.GetBankAccountByName(name);
                if (selectedAccount != null)
                {
                    string itemtext = string.Empty;
                    if (item != null)
                    {
                        itemtext = item.Name;
                    }
                    SEconomyPlugin.Instance.WorldAccount.TransferTo(selectedAccount, money, BankAccountTransferOptions.AnnounceToReceiver, $"{(TRTrade.Config.Language == Config.LanguageType.Chinese || (TRTrade.Config.AutoLanguage == Config.LanguageType.Chinese && TRTrade.Config.Language == Config.LanguageType.Auto)  ? (money < 0 ? "购买商品" : "出售商品") : (money < 0 ? "buy product" : "sell product"))} {itemtext}", $"SE: {name} {(TRTrade.Config.Language == Config.LanguageType.Chinese ? (money < 0 ? "购买商品" : "出售商品") : (money < 0 ? "buy product" : "sell product"))} {itemtext}");
                    return true;
                }
                else
                {
                    TShock.Log.ConsoleError(GetText("Log_AccountNotFound", false).Replace("{name}", name));
                    return false;
                }
            }

            public static bool POBC_Change(string name, long money)
            {
                if (DbExt.Query(TShock.DB, $"UPDATE POBC SET Currency = Currency {(money < 0 ? "-" : "+")} @0 WHERE UserName=@1", new object[] { money < 0 ? -money : money, name }) > 0)
                {
                    return true;
                }
                else
                {
                    TShock.Log.ConsoleError(GetText("Log_AccountNotFound", false).Replace("{name}", name));
                    return false;
                }
            }

            public static long SEconomy_Balance(TSPlayer plr)
            {
                IBankAccount selectedAccount = SEconomyPlugin.Instance.GetPlayerBankAccount(plr.Name);
                if (selectedAccount != null) return selectedAccount.Balance.Value;
                else
                {
                    TShock.Log.ConsoleError(GetText("Log_CannotGetBalance", false).Replace("{plr.Name}", plr.Name));
                    return -1;
                }
            }

            public static long BeanPoint_Balance(TSPlayer plr)
            {
                using (QueryResult queryResult = DbExt.QueryReader(TShock.DB, "SELECT Money FROM bpplayers WHERE Name=@0", new string[] { plr.Name }))
                {
                    if (queryResult.Read())
                    {
                        return queryResult.Get<int>("Money");
                    }
                    else
                    {
                        TShock.Log.ConsoleError(GetText("Log_CannotGetBalance", false).Replace("{plr.Name}", plr.Name));
                        return -1;
                    }
                }
            }

            public static long POBC_Balance(TSPlayer plr)
            {
                using (QueryResult queryResult = DbExt.QueryReader(TShock.DB, "SELECT Currency FROM POBC WHERE UserName=@0", new string[] { plr.Name }))
                {
                    if (queryResult.Read())
                    {
                        return queryResult.Get<int>("Currency");
                    }
                    else
                    {
                        TShock.Log.ConsoleError(GetText("Log_CannotGetBalance", false).Replace("{plr.Name}", plr.Name));
                        return -1;
                    }
                }
            }
        }

        public static string GetDescription(TItem item)
        {
            return item == null ? null : GetText("ProductDescription", false).Replace("{TShock.Utils.ItemTag(item.Item) ?? new Item().Name}", TShock.Utils.ItemTag(item.Item) ?? new Item().Name).Replace("{item.Price}", item.Price.ToString()).Replace("{item.ID}", item.ID.ToString());
        }

        public static List<string> GetDescriptions(List<TItem> item)
        {
            List<string> description = new List<string>();
            Dictionary<int, List<string>> dic = new Dictionary<int, List<string>>();
            item.ForEach(i =>
            {
                var tempaccount = TShock.UserAccounts.GetUserAccountByID(i.UserID);
                string name = tempaccount != null ? tempaccount.Name : "Unknown";
                if (!dic.ContainsKey(i.UserID))
                {
                    dic.Add(i.UserID, new List<string>());
                    dic[i.UserID].Add(GetText("ProductDescription_Seller", false).Replace("{name}", name));
                }
                dic[i.UserID].Add(GetDescription(i));
            });
            dic.ForEach(d => description.AddRange(d.Value));
            return description;
        }
    }
    public static class Extensions
    {
        static bool CheckLoaded()
        {
            switch (TRTrade.Config.Type)
            {
                case Config.MoneyType.BeanPoint:
                    return ServerApi.Plugins.Where(p => p.Plugin.Name == "BeanPoints").Any();
                case Config.MoneyType.POBC:
                    return ServerApi.Plugins.Where(p => p.Plugin.ToString() == "POBC2.POBCSystem").Any();
                case Config.MoneyType.SEconomy:
                    return ServerApi.Plugins.Where(p => p.Plugin.ToString() == "Wolfje.Plugins.SEconomy.SEconomyPlugin").Any();
                default:
                    return false;
            }
        }
        /// <summary>
        /// 以此玩家的身份添加一件商品
        /// </summary>
        /// <param name="plr">玩家</param>
        /// <param name="item">物品</param>
        /// <param name="price">价格</param>
        /// <returns></returns>
        public static bool AddTrade(this TSPlayer plr, Item item, long price)
        {
            return DB.Add(plr, item, price);
        }

        public static bool TakeMoney(this TSPlayer plr, long money, Item item = null)
        {
            if (!CheckLoaded())
            {
                TShock.Log.ConsoleError(GetText("Log_EconomyFrameworkNotFound", false).Replace("{TRTrade.Config.Type}", TRTrade.Config.Type.ToString()));
                return false;
            }
            return TRTrade.Config.Type switch
            {
                Config.MoneyType.SEconomy => Utils.EconomyFamework.SEconomy_Change(plr.Name, -money, item),
                Config.MoneyType.BeanPoint => Utils.EconomyFamework.BeanPoint_Change(plr.Name, -money),
                Config.MoneyType.POBC => Utils.EconomyFamework.POBC_Change(plr.Name, -money),
                _ => false,
            };
        }
        public static bool GiveMoney(this TSPlayer plr, long money, Item item = null)
        {
            if (!CheckLoaded())
            {
                TShock.Log.ConsoleError(GetText("Log_EconomyFrameworkNotFound", false).Replace("{TRTrade.Config.Type}", TRTrade.Config.Type.ToString()));
                return false;
            }
            switch (TRTrade.Config.Type)
            {
                case Config.MoneyType.SEconomy:
                    return Utils.EconomyFamework.SEconomy_Change(plr.Name, money, item);
                case Config.MoneyType.BeanPoint:
                    return Utils.EconomyFamework.BeanPoint_Change(plr.Name, money);
                case Config.MoneyType.POBC:
                    return Utils.EconomyFamework.POBC_Change(plr.Name, money);
            }
            return false;
        }
        public static bool GiveMoney(this string name, long money, Item item = null)
        {
            if (!CheckLoaded())
            {
                TShock.Log.ConsoleError(GetText("Log_EconomyFrameworkNotFound", false).Replace("{TRTrade.Config.Type}", TRTrade.Config.Type.ToString()));
                return false;
            }
            switch (TRTrade.Config.Type)
            {
                case Config.MoneyType.SEconomy:
                    return Utils.EconomyFamework.SEconomy_Change(name, money, item);
                case Config.MoneyType.BeanPoint:
                    return Utils.EconomyFamework.BeanPoint_Change(name, money);
                case Config.MoneyType.POBC:
                    return Utils.EconomyFamework.POBC_Change(name, money);
            }
            return false;
        }

        public static long GetBalance(this TSPlayer plr)
        {
            if (!CheckLoaded())
            {
                TShock.Log.ConsoleError(GetText("Log_EconomyFrameworkNotFound", false).Replace("{TRTrade.Config.Type}", TRTrade.Config.Type.ToString()));
                return -1;
            }
            switch (TRTrade.Config.Type)
            {
                case Config.MoneyType.SEconomy:
                    return Utils.EconomyFamework.SEconomy_Balance(plr);
                case Config.MoneyType.BeanPoint:
                    return Utils.EconomyFamework.BeanPoint_Balance(plr);
                case Config.MoneyType.POBC:
                    return Utils.EconomyFamework.POBC_Balance(plr);
            }
            return -1;
        }
    }
}