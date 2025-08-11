#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wjybxx.BigCat.MVC;

namespace Wjybxx.BigCat.Tests
{
/// <summary>
/// 测试默认的数据解析器
/// </summary>
public class DataModelResolverTest
{
    private static readonly DataModelResolver dataModelResolver = new DataModelResolver();
    private static Player player;
    private static AggregationModel aggregationModel;

    [OneTimeSetUp]
    public static void SetUp() {
        player = new Player();
        {
            Item item1 = new Item() { cid = 1001, name = "物品1", number = 1, bind = false };
            Item item2 = new Item() { cid = 1002, name = "物品2", number = 1, bind = false };
            player.bagModel.itemList.Add(item1);
            player.bagModel.itemList.Add(item2);

            player.bagModel.itemDic.Add(item1.cid, item1);
            player.bagModel.itemDic.Add(item2.cid, item2);
        }
        aggregationModel = new AggregationModel();
        aggregationModel.LogicModel = player;
    }

    [Test]
    public void TestAbsolutelyPath() {
        object parentModel = player.bagModel;
        object dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "");
        Assert.AreSame(parentModel, dataModel);

        // 测试List
        dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/itemList/0");
        {
            Item item = dataModel as Item;
            Assert.IsNotNull(item);
            Assert.AreEqual(1001, item.cid);
        }
        // 测试List带uiIndex
        dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/itemList/{uiIndex}", 0);
        {
            Item item = dataModel as Item;
            Assert.IsNotNull(item);
            Assert.AreEqual(1001, item.cid);
        }

        // 测试字典
        dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/itemDic/1001");
        {
            Item item = dataModel as Item;
            Assert.IsNotNull(item);
            Assert.AreEqual(1001, item.cid);
        }
        // 测试失败情况
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "/root/bagModel/itemList/-1"));
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/item_list/0"));
        
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/itemList/-1"));
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/itemList/1001"));

        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/itemDic/-1"));
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "/logic/bagModel/itemDic/0"));
    }

    [Test]
    public void TestRelativePath() {
        object parentModel = player.bagModel;
        object dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "");
        Assert.AreSame(parentModel, dataModel);

        // 测试List
        dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "itemList/0");
        {
            Item item = dataModel as Item;
            Assert.IsNotNull(item);
            Assert.AreEqual(1001, item.cid);
        }
        // 测试List带uiIndex
        dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "itemList/{uiIndex}", 0);
        {
            Item item = dataModel as Item;
            Assert.IsNotNull(item);
            Assert.AreEqual(1001, item.cid);
        }

        // 测试字典
        dataModel = dataModelResolver.Resolve(aggregationModel, parentModel, "itemDic/1001");
        {
            Item item = dataModel as Item;
            Assert.IsNotNull(item);
            Assert.AreEqual(1001, item.cid);
        }
        // 测试失败情况
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "item_list/0"));
        
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "itemList/-1"));
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "itemList/1001"));

        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "itemDic/-1"));
        Assert.Catch<Exception>(() => dataModelResolver.Resolve(aggregationModel, parentModel, "itemDic/0"));
    }

    private class Player
    {
        public BagModel bagModel = new BagModel();
        public QuestModel questModel = new QuestModel();
    }

    private class BagModel
    {
        public readonly List<Item> itemList = new List<Item>();
        public readonly Dictionary<int, Item> itemDic = new Dictionary<int, Item>();
    }

    private class Item
    {
        public int cid;
        public string name;
        public int number = 1;
        public bool bind;
    }

    private class QuestModel
    {
        public readonly Dictionary<int, QuestData> questDic = new Dictionary<int, QuestData>();
    }

    private class QuestData
    {
        public int cid;
        public string name;
        public long expireTime;
    }

    private class AggregationModel : IAggregationModel
    {
        public object ViewModel { get; set; }
        public object ViewManagers { get; }
        public object LogicModel { get; set; }
        public object LogicManagers { get; }
    }
}
}