using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using static TRTrade.Utils;

namespace TRTrade
{
    internal class DB
    {
        public static void CreateTable()
        {
            try
            {
                SqlTable sqlTable = new SqlTable("Trade", new SqlColumn[]
                {
                new SqlColumn("ItemID", MySql.Data.MySqlClient.MySqlDbType.Int32)
                {
                    Primary = true,
                    AutoIncrement = true
                },
                new SqlColumn("UserID", MySql.Data.MySqlClient.MySqlDbType.Int32),
                new SqlColumn("Tag", MySql.Data.MySqlClient.MySqlDbType.Text),
                new SqlColumn("Price", MySql.Data.MySqlClient.MySqlDbType.Int64),
                new SqlColumn("AddDate", MySql.Data.MySqlClient.MySqlDbType.Text)
                });
                IDbConnection db = TShock.DB;
                IQueryBuilder queryBuilder2;
                if (DbExt.GetSqlType(TShock.DB) != SqlType.Sqlite)
                {
                    IQueryBuilder queryBuilder = new MysqlQueryCreator();
                    queryBuilder2 = queryBuilder;
                }
                else
                {
                    IQueryBuilder queryBuilder = new SqliteQueryCreator();
                    queryBuilder2 = queryBuilder;
                }
                new SqlTableCreator(db, queryBuilder2).EnsureTableStructure(sqlTable);
            }
            catch (Exception ex) { TShock.Log.Error(ex.Message); }

        }

        /// <summary>
        /// 向数据库中添加一条数据
        /// </summary>
        /// <param name="plr">玩家对象</param>
        /// <param name="item">物品对象</param>
        /// <param name="price">价格</param>
        /// <returns></returns>
        public static bool Add(TSPlayer plr, Item item, long price)
        {
            try { return DbExt.QueryReader(TShock.DB, $"INSERT INTO Trade (UserID,Tag,Price, AddDate) VALUES ('{plr.Account.ID}','{TShock.Utils.ItemTag(item)}','{price}','{DateTime.Now.ToString()}');").Read(); }
            catch (Exception ex) { TShock.Log.ConsoleError($"<交易插件> 添加商品失败.\n" + ex); return false; }
        }
        public static void Del(int id) => Task.Run(() =>
        {
            try
            {
                DbExt.Query(TShock.DB, $"DELETE FROM Trade WHERE ItemID='{id}';");
            }
            catch (Exception ex) { TShock.Log.ConsoleError($"<交易插件> 移除商品失败.\n" + ex); }
        });
        public static List<TItem> GetAll()
        {
            List<TItem> list = new();
            try
            {
                var reader = DbExt.QueryReader(TShock.DB, $"SELECT * FROM Trade;");
                while (reader.Read())
                {
                    var time = DateTime.MinValue;
                    DateTime.TryParse(reader.Get<string>("AddDate"), out time);
                    Item item = TShock.Utils.GetItemFromTag(reader.Get<string>("Tag"));
                    list.Add(new TItem(reader.Get<int>("ItemID"), reader.Get<int>("UserID"), item, reader.Get<long>("Price"), time));
                }
            }
            catch (Exception ex) { TShock.Log.ConsoleError($"<交易插件> 获取商品失败.\n" + ex); }
            return list;
        }

        public static TItem GetFromItemID(int id)
        {
            var reader = DbExt.QueryReader(TShock.DB, $"SELECT * FROM Trade WHERE ItemID='{id}';");
            if (reader.Read())
            {
                Item item = TShock.Utils.GetItemFromTag(reader.Get<string>("Tag"));
                return new TItem(reader.Get<int>("ItemID"), reader.Get<int>("UserID"), item, reader.Get<long>("Price"), DateTime.Parse(reader.Get<string>("AddDate")));
            }
            return null;
        }

        public static List<TItem> GetFromUserID(int id)
        {
            var reader = DbExt.QueryReader(TShock.DB, $"SELECT * FROM Trade WHERE UserID='{id}';");
            List<TItem> list = new();
            while (reader.Read())
            {
                Item item = TShock.Utils.GetItemFromTag(reader.Get<string>("Tag"));
                list.Add(new TItem(reader.Get<int>("ItemID"), reader.Get<int>("UserID"), item, reader.Get<long>("Price"), DateTime.Parse(reader.Get<string>("AddDate"))));
            }
            return list;
        }
    }
}
