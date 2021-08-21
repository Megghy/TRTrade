using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using static TRTrade.Utils;
using static TRTrade.Localization;
using Newtonsoft.Json.Linq;

namespace TRTrade
{
    [ApiVersion(2, 1)]
    public class TRTrade : TerrariaPlugin
    {
        public override string Author => "Megghy";

        public override string Description => "提供玩家间交易的功能.";

        public override string Name => "TRTrade";

        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public TRTrade(Main game) : base(game) { }

        internal static Config Config = new();

        public override void Initialize()
        {
            DB.CreateTable(); //创建表
            Config.Load();
            ServerApi.Hooks.GamePostInitialize.Register(this, PostInitialize);
        }

        protected override void Dispose(bool disposing)
        {
            TShockAPI.Hooks.GeneralHooks.ReloadEvent -= Config.Load;
            base.Dispose(disposing);
        }

        private void PostInitialize(EventArgs args)
        {
            TShockAPI.Hooks.GeneralHooks.ReloadEvent += Config.Load;

            Commands.ChatCommands.Add(new Command(
                permissions: new List<string> { "trade.use" },
                cmd: CMD,
                "trade", "jy"));
        }

        private void CMD(CommandArgs args)
        {
            var plr = args.Player;
            if (!plr.IsLoggedIn && plr != TSPlayer.Server)
            {
                plr.SendErrorMessage(GetText("Prompt_NotLoggin"));
                return;
            }
            else if (!Main.ServerSideCharacter)
            {
                plr.SendErrorMessage(GetText("Prompt_NotEnableSSC"));
                return;
            }
            var cmd = args.Parameters;
            int num = 1;
            string _cmd = cmd.Count == 0 ? "list" : int.TryParse(cmd[0], out num) ? "listnum" : cmd[0];
            switch (_cmd)
            {
                case "help":
                case "帮助":
                    Help(cmd, plr);
                    break;
                case "add":
                case "sell":
                case "出售":
                    Add(cmd, plr);
                    break;
                case "list":
                case "lb":
                case "列表":
                    List(cmd, plr);
                    break;
                case "user":
                case "yh":
                case "用户":
                    User(cmd, plr);
                    break;
                case "buy":
                case "gm":
                case "购买":
                    Buy(cmd, plr);
                    break;
                case "my":
                case "wd":
                case "我的":
                    My(cmd, plr);
                    break;
                case "search":
                case "ss":
                case "搜索":
                    Search(cmd, plr);
                    break;
                case "del":
                case "delete":
                case "remove":
                case "sc":
                case "删除":
                    if (plr.HasPermission("trade.admin")) Del(cmd, plr);
                    else plr.SendErrorMessage(GetText("Prompt_PermissionDenied"));
                    break;
                case "listnum":
                    List(new List<string>() { "思考", num.ToString() }, plr);
                    break;
                default:
                    plr.SendErrorMessage(GetText("Prompt_InvalidCommand"));
                    break;
            }
        }
        private void Help(List<string> cmd, TSPlayer plr)
        {
            List<string> help = new List<string>()
            {
                GetText("Text_Help_Title", false),
                Config.EnablePEFeature ? GetText("Text_Help_Add_EnablePEFeature", false) : GetText("Text_Help_Add_Normal", false),
                GetText("Text_Help_List", false),
                GetText("Text_Help_SearchUser", false),
                GetText("Text_Help_SearchGoods", false),
                GetText("Text_Help_Buy", false),
                GetText("Text_Help_MyGoods", false),
                GetText("Text_Help_MyGoods_TackBack", false),
                ""
            };
            List<string> help_admin = new List<string>()
            {
                GetText("Text_Help_AdminCommandTitle", false),
                GetText("Text_Help_Del", false)
            };
            if (plr.HasPermission("trade.admin")) help.AddRange(help_admin);
            if (!PaginationTools.TryParsePageNumber(cmd, 1, plr, out int pageNumber))
            {
                plr.SendErrorMessage(GetText("Prompt_InvalidPageNumber"));
                return;
            }
            PaginationTools.SendPage(plr, pageNumber, help, new PaginationTools.Settings
            {
                HeaderFormat = GetText("Prompt_Help_PageHeader"),
                FooterFormat = GetText("Prompt_Help_PageFooter", false).SFormat(new object[]
                    {
                        Commands.Specifier
                    }),
                MaxLinesPerPage = 8
            });
        }
        private void Add(List<string> cmd, TSPlayer plr)
        {
            if (Config.EnablePEFeature)
            {
                if (cmd.Count < 2)
                {
                    plr.SendErrorMessage(GetText("Text_Help_Add_EnablePEFeature"));
                    return;
                }
                Item sellitem = plr.TPlayer.inventory[0].Clone();
                long price;
                if (!plr.IsLoggedIn)
                {
                    plr.SendErrorMessage(GetText("Prompt_NotLoggin"));
                    return;
                }
                if (sellitem.netID == 0)
                {
                    plr.SendErrorMessage(GetText("Prompt_Add_UnknownItem"));
                    return;
                }
                else if (!long.TryParse(cmd[1], out price))
                {
                    plr.SendErrorMessage(GetText("Prompt_Add_WrongPriceFormat") + GetText("Text_Help_Add_EnablePEFeature", false));
                    return;
                }
                else if (price < 0)
                {
                    plr.SendErrorMessage(GetText("Prompt_Add_NegativePrice"));
                    return;
                }
                plr.TPlayer.inventory[0].SetDefaults(0);
                plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, 0); //移除玩家背包内的物品
                plr.AddTrade(sellitem, price);
                plr.SendSuccessMessage(GetText("Prompt_Add_Success").Replace("{TShock.Utils.ItemTag(sellitem)}", TShock.Utils.ItemTag(sellitem)).Replace("{price}", price.ToString()).Replace("{Config.TaxRate}", Config.TaxRate.ToString()));
                TShock.Log.ConsoleInfo(GetText("Log_Add", false).Replace("{plr.Name}", plr.Name).Replace("{sellitem.stack}", sellitem.stack.ToString()).Replace("{sellitem.Name}", sellitem.Name).Replace("{price}", price.ToString()));
            }
            else
            {
                if (cmd.Count < 3)
                {
                    plr.SendErrorMessage(GetText("Text_Help_Add_Normal"));
                    return;
                }
                Item sellitem = TShock.Utils.GetItemFromTag(cmd[1]);
                long price;
                if (!plr.IsLoggedIn)
                {
                    plr.SendErrorMessage(GetText("Prompt_NotLoggin"));
                    return;
                }
                if (sellitem == null)
                {
                    plr.SendErrorMessage(GetText("Prompt_Add_UnknownItem"));
                    return;
                }
                else if (!long.TryParse(cmd[2], out price))
                {
                    plr.SendErrorMessage(GetText("Prompt_Add_WrongPriceFormat") + GetText("Text_Help_Add_Normal", false));
                    return;
                }
                else if (price < 0)
                {
                    plr.SendErrorMessage(GetText("Prompt_Add_NegativePrice"));
                    return;
                }
                for (int i = 0; i < 59; i++)
                {
                    if ((plr.TPlayer.inventory[i].netID == sellitem.netID &&
                            plr.TPlayer.inventory[i].prefix == sellitem.prefix &&
                            plr.TPlayer.inventory[i].stack == sellitem.stack))
                    {
                        plr.AddTrade(sellitem, price);
                        plr.TPlayer.inventory[i].SetDefaults(0);
                        plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, i); //移除玩家背包内的物品
                        plr.SendSuccessMessage(GetText("Prompt_Add_Success").Replace("{TShock.Utils.ItemTag(sellitem)}", TShock.Utils.ItemTag(sellitem)).Replace("{price}", price.ToString()).Replace("{Config.TaxRate}", Config.TaxRate.ToString()));
                        TShock.Log.ConsoleInfo(GetText("Log_Add", false).Replace("{plr.Name}", plr.Name).Replace("{sellitem.stack}", sellitem.stack.ToString()).Replace("{sellitem.Name}", sellitem.Name).Replace("{price}", price.ToString()));
                        return;
                    }
                }
                plr.SendErrorMessage(GetText("Prompt_Add_ItemNotFound") + TShock.Utils.ItemTag(sellitem));
            }
        }
        private void Del(List<string> cmd, TSPlayer plr)
        {
            if (cmd.Count >= 2 && int.TryParse(cmd[1], out int id))
            {
                var item = DB.GetFromItemID(id);
                if (item == null)
                {
                    plr.SendErrorMessage(GetText("Prompt_IDNotFound").Replace("{id}", id.ToString()));
                    return;
                }
                else
                {
                    DB.Del(item.ID);
                    plr.SendSuccessMessage(GetText("Prompt_Del_Success") + TShock.Utils.ItemTag(item.Item));
                    TShock.Log.ConsoleInfo(GetText("Log_Del", false).Replace("{plr.Name}", plr.Name).Replace("{item.Item.stack}", item.Item.stack.ToString()).Replace("{item.Price}", item.Price.ToString()).Replace("{item.Item.Name}", item.Item.Name));
                }
            }
            else
            {
                plr.SendErrorMessage(GetText("Prompt_WrongFormat") + GetText("Text_Help_Del", false));
            }
        }
        private void User(List<string> cmd, TSPlayer plr)
        {
            if (cmd.Count > 1)
            {
                var tempplr = TShock.UserAccounts.GetUserAccountsByName(cmd[1]);
                if (tempplr.Any())
                {
                    List<TItem> items = new List<TItem>();
                    tempplr.ForEach(p =>
                    {
                        items.AddRange(DB.GetFromUserID(p.ID));
                    });
                    if (!PaginationTools.TryParsePageNumber(cmd, 2, plr, out int pageNumber))
                    {
                        plr.SendErrorMessage(GetText("Prompt_InvalidPageNumber"));
                        return;
                    }
                    PaginationTools.SendPage(plr, pageNumber, GetDescriptions(items), new PaginationTools.Settings
                    {
                        HeaderFormat = GetText("Prompt_SearchUser_PageHeader").Replace("{cmd[1]}", cmd[1]),
                        FooterFormat = GetText("Prompt_SearchUser_PageFooter", false).SFormat(new object[]
                            {
                                Commands.Specifier
                            }),
                        NothingToDisplayString = GetText("Prompt_SearchUser_NoResult"),
                        MaxLinesPerPage = 8
                    });
                }
                else
                {
                    plr.SendErrorMessage(GetText("Prompt_SearchUser_UserNotFound"));
                }
            }
            else
            {
                plr.SendErrorMessage(GetText("Prompt_WrongFormat") + GetText("Text_Help_SearchUser"));
            }
        }
        private async void List(List<string> cmd, TSPlayer plr)
        {
            await Task.Run(() =>
            {
                if (!PaginationTools.TryParsePageNumber(cmd, 1, plr, out int pageNumber))
                {
                    plr.SendErrorMessage(GetText("Prompt_InvalidPageNumber"));
                    return;
                }
                PaginationTools.SendPage(plr, pageNumber, GetDescriptions(DB.GetAll()), new PaginationTools.Settings
                {
                    HeaderFormat = GetText("Prompt_Goods_PageHeader"),
                    FooterFormat = GetText("Prompt_Goods_PageFooter", false).SFormat(new object[]
                        {
                        Commands.Specifier
                        }),
                    NothingToDisplayString = GetText("Prompt_SearchGoods_NotFound"),
                    MaxLinesPerPage = 8
                });
            });
        }
        private void Buy(List<string> cmd, TSPlayer plr)
        {
            if (cmd.Count < 2)
            {
                plr.SendErrorMessage(GetText("Prompt_WrongFormat") + GetText("Text_Help_Buy", false));
                return;
            }
            if (int.TryParse(cmd[1], out int id))
            {
                var item = DB.GetFromItemID(id);
                if (item == null)
                {
                    plr.SendErrorMessage(GetText("Prompt_Add_UnknownItem") + GetText("Text_Help_Buy", false));
                    return;
                }
                else if (item.UserID == plr.Account.ID)
                {
                    plr.SendErrorMessage(GetText("Prompt_Buy_BuyOwnProduct") + GetText("Text_Help_MyGoods_TackBack", false));
                    return;
                }
                else if (plr.GetBalance() < item.Price)
                {
                    plr.SendErrorMessage(GetText("Prompt_Buy_InsufficientBalance").Replace("{item.Price}", item.Price.ToString()) + $" [c/8DF9D8:{plr.GetBalance()}].");
                    return;
                }
                else if (!plr.InventorySlotAvailable)
                {
                    plr.SendErrorMessage(GetText("Prompt_Buy_InsufficientSlot"));
                    return;
                }
                var seller = TShock.UserAccounts.GetUserAccountByID(item.UserID);
                if (seller == null)
                {
                    plr.SendErrorMessage(GetText("Prompt_Buy_SellerNotFound"));
                    return;
                }
                if (seller.Name.GiveMoney(Config.AfterTax(item.Price), item.Item) && plr.TakeMoney(item.Price, item.Item) && plr.GiveItemCheck(item.Item.netID, item.Item.Name, item.Item.stack, item.Item.prefix))
                {
                    DB.Del(item.ID);
                    plr.SendSuccessMessage(GetText("Prompt_Buy_Success") + TShock.Utils.ItemTag(item.Item));
                    var sellerlist = TShock.Players.Where(p => p != null && p.Account != null && p.Account.ID == item.UserID).ToList();
                    if (sellerlist.Any()) sellerlist[0].SendSuccessMessage(GetText("Prompt_Buy_Success_SendToSeller").Replace("{item.Item.Name}", item.Item.Name).Replace("{Config.AfterTax(item.Price)}", Config.AfterTax(item.Price).ToString()).Replace("{plr.Name}", plr.Name));
                    TShock.Log.ConsoleInfo(GetText("Log_Buy", false).Replace("{seller.Name}", seller.Name).Replace("{Config.AfterTax(item.Price)}", Config.AfterTax(item.Price).ToString()).Replace("{plr.Name}", plr.Name).Replace("{item.Item.Name}", item.Item.Name).Replace("{item.Item.stack}", item.Item.stack.ToString()).Replace("{item.Price}", item.Price.ToString()));
                    string text = Config.BroadcastText;
                    text = text.Replace("{name}", item.Item.Name).Replace("{seller}", seller.Name).Replace("{buyer}", plr.Name).Replace("{price}", item.Price.ToString());
                    if (Config.Broadcast) TSPlayer.All.SendMessage(text, Microsoft.Xna.Framework.Color.White);
                    return;
                }
                plr.SendErrorMessage(GetText("Prompt_InternalError"));
            }
            else
            {
                plr.SendErrorMessage(GetText("Prompt_WrongFormat") + GetText("Text_Help_Buy", false));
            }
        }
        private void My(List<string> cmd, TSPlayer plr)
        {
            if (cmd.Count >= 2)
            {
                switch (cmd[1])
                {
                    case "back":
                    case "takeback":
                    case "qh":
                    case "取回":
                        if (cmd.Count >= 3 && int.TryParse(cmd[2], out int id))
                        {
                            var item = DB.GetFromItemID(id);
                            if (item == null)
                            {
                                plr.SendErrorMessage(GetText("Prompt_IDNotFound"));
                                return;
                            }
                            else if (item.UserID != plr.Account.ID)
                            {
                                plr.SendErrorMessage(GetText("Prompt_MyGoods_TakeBack_NotBelongToMe"));
                                return;
                            }
                            else if (!plr.InventorySlotAvailable)
                            {
                                plr.SendErrorMessage(GetText("Prompt_MyGoods_TakeBack_InsufficientSlot"));
                                return;
                            }
                            if (plr.GiveItemCheck(item.Item.netID, item.Item.Name, item.Item.stack, item.Item.prefix))
                            {
                                DB.Del(item.ID);
                                plr.SendSuccessMessage(GetText("Prompt_MyGoods_TakeBack_Success") + TShock.Utils.ItemTag(item.Item));
                                TShock.Log.ConsoleInfo(GetText("Log_TakeBack", false).Replace("{plr.Name}", plr.Name).Replace("{item.Item.stack}", item.Item.stack.ToString()).Replace("{item.Price}", item.Price.ToString()).Replace("{item.Item.Name}", item.Item.Name));
                                return;
                            }
                            plr.SendErrorMessage(GetText("Prompt_InternalError"));
                        }
                        break;
                    default:

                        break;
                }
            }
            else
            {
                List<string> description = new();
                Dictionary<int, List<string>> dic = new();
                description = new List<string>();
                DB.GetFromUserID(plr.Account.ID).ForEach(i => description.Add(Utils.GetDescription(i)));
                if (!PaginationTools.TryParsePageNumber(cmd, 1, plr, out int pageNumber))
                {
                    plr.SendErrorMessage(GetText("Prompt_InvalidPageNumber"));
                    return;
                }
                PaginationTools.SendPage(plr, pageNumber, description, new PaginationTools.Settings
                {
                    HeaderFormat = GetText("Prompt_MyGoods_PageHeader"),
                    FooterFormat = GetText("Prompt_MyGoods_PageFooter", false).SFormat(new object[]
                        {
                            Commands.Specifier
                        }),
                    NothingToDisplayString = GetText("Prompt_NoResult"),
                    MaxLinesPerPage = 8
                });
            }
        }
        private void Search(List<string> cmd, TSPlayer plr)
        {
            if (cmd.Count >= 2)
            {
                string text = cmd[1];
                int.TryParse(text, out int id);
                List<TItem> result = new List<TItem>();
                DB.GetAll().ForEach(item =>
                {
                    if (item.Item.Name.ToLower().Contains(text.ToLower()) || item.Item.netID == id)
                    {
                        result.Add(item);
                    }
                });
                if (!result.Any())
                {
                    plr.SendErrorMessage(GetText("Prompt_SearchGoods_NotFound").Replace("{text}", text)) ;
                }
                else
                {
                    plr.SendMessage(string.Join("\n", Utils.GetDescriptions(result)), Microsoft.Xna.Framework.Color.White);
                }
            }
        }
    }
}