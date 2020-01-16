using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisHelperTest
{
    class Program
    {
        //链接配置
        static ConfigurationOptions configurationOptions = ConfigurationOptions.Parse("localhost:6379");
        //链接redis
        static ConnectionMultiplexer redisClient = ConnectionMultiplexer.Connect(configurationOptions);
        //得到数据库的连接
        //static IDatabase db = redisClient.GetDatabase();
        // 可传入参数代表数据库
        static IDatabase db = redisClient.GetDatabase(1);

        static void Main(string[] args)
        {
            //ClearKey();
            //TestKey();
            //TestKeyExists();
            //TestString();   //测试String
            //TestList();     //测试List
            //TestHash();     //测试Hash
            TestSet();        //测试Set
            //TestSortedSet();    //测试SortedSet
            Console.ReadLine();
        }

        /// <summary>
        /// 清理所有的key
        /// </summary>
        public static void ClearKey()
        {
            db.KeyDelete("1:2:2");
        }

        /// <summary>
        /// 测试key
        /// </summary>
        /// <returns></returns>
        public static void TestKey()
        {
            string key1 = "1";
            //1:1和1:2会创建一个层级1去管理
            string key2 = "1:1";
            string key3 = "1:2";
            db.StringSet(key1, "1");
            db.StringSet(key2, "1.1");
            db.StringSet(key3, "1.2");
        }

        /// <summary>
        /// 检测key是否存在
        /// </summary>
        public static void TestKeyExists()
        {
            string key = "TestKeyExists";
            string key1 = "MyKey";
            db.StringSet(key, "TestKeyExists检测是否存在");
            //使用KeyExists检测key是否存在
            bool keyExists = db.KeyExists(key);
            bool keyExists1 = db.KeyExists(key1);
            Console.WriteLine("key ：" + key + " 是否存在：" + keyExists);
            Console.WriteLine("key ：" + key1 + " 是否存在：" + keyExists1);
        }

        /// <summary>
        /// 测试String
        /// String是最常见的一种数据类型，普通的key/value存储都可以归为此类
        /// </summary>
        /// <returns></returns>
        public static void TestString()
        {
            string key = "TestString";
            db.KeyDelete(key);

            //通过StringSet值来设置
            db.StringSet(key, "通过StringSet设置的值");

            //通过StringGet来得到值
            string value = db.StringGet(key);
            Console.WriteLine("key:" + key + ", value: " + value);

            //通过StringAppend来追加值
            db.StringAppend(key, "，我是追加的值");
            Console.WriteLine("key:" + key + ", StringAppend追加以后的value: " + value);

            //通过StringSet更新存在的key
            db.StringSet(key, "重新设置的值");
            Console.WriteLine("key:" + key + ", StringSet重新设置的value: " + value);

            //String还可以存取序列化的字符串
            UserInfoDto userInfoDto = new UserInfoDto
            {
                StaffName = "josan",
                Password = "1314"
            };
            string userInfoDtoJson = JsonConvert.SerializeObject(userInfoDto);
            db.StringSet("userInfoDto", userInfoDtoJson);
            string userInfoDtoStr = db.StringGet("userInfoDto");
            UserInfoDto userInfoDto2 = JsonConvert.DeserializeObject<UserInfoDto>(userInfoDtoStr);
            Console.WriteLine("存取序列化以后的值: name:" + userInfoDto2.StaffName + ",pwd:" + userInfoDto2.Password);

            //使用StringIncrement 增量、StringDecrement 减量（默认值同为1）
            double increment = 0;
            double decrement = 0;
            for (int i = 0; i < 3; i++)
            {
                //增量，每次增加2
                increment = db.StringIncrement("incrementKey", 2);
                Console.WriteLine("increment:" + increment + ", redis中的值: " + db.StringGet("incrementKey"));
            }
            for (int i = 0; i < 3; i++)
            {
                //减量，每次减少1
                decrement = db.StringDecrement("decrementKey");
                Console.WriteLine("decrement:" + decrement + ", redis中的值: " + db.StringGet("decrementKey"));
            }

            //使用xxxAsync方法，进行异步操作
            db.StringSetAsync("StringSetAsync", "StringSetAsyncValue");
            string StringGetAsyncValue = db.StringGet("StringSetAsync");
            Console.WriteLine("异步方法设置值 key:StringSetAsync ,value: " + StringGetAsyncValue);

            //使用KeyDelete(key)清理key对应的值
            //db.KeyDelete(key);
        }

        /// <summary>
        /// 测试List
        /// 简单的字符串列表，按照插入顺序排序，可以添加一个元素到列表的头部和尾部
        /// List是一个有序的列表，且可重复的
        /// </summary>
        /// <returns></returns>
        public static void TestList()
        {
            string key = "ListTest";
            db.KeyDelete(key);
            for (int i = 0; i < 10; i++)
            {
                //使用ListRightPush从底部插入
                db.ListRightPush(key, i);
            }
            for (int i = 10; i < 20; i++)
            {
                //使用ListLeftPush从顶部插入
                db.ListLeftPush(key, i);
            }

            //使用db.ListLength获取List长度
            long listLength = db.ListLength(key);
            Console.WriteLine("db.ListLength(key)获取List长度：" + listLength);

            //使用db.ListLeftPop获取List顶部数据，拿出以后不存在List中
            string leftValue = db.ListLeftPop(key);
            Console.WriteLine("db.ListLeftPop(key)拿出List顶部数据：" + leftValue);

            //使用db.ListRightPop获取List底部数据，拿出以后不存在List中
            string rightValue = db.ListRightPop(key);
            Console.WriteLine("db.ListRightPop(key)拿出List底部数据：" + rightValue);

            //使用db.ListRange可以获取到List某个区间的数组, 不传入值，默认start、stop为0，也就是List头部元素
            var ListValuesTop = db.ListRange(key);
            Console.WriteLine("ListRange不传值获取List头部元素：" + ListValuesTop[0]);

            var ListValues = db.ListRange(key, 0, db.ListLength(key));
            string listValuesStr = string.Empty;
            for (int i = 0; i < ListValues.Length; i++)
            {
                listValuesStr += ListValues[i] + " ";
            }
            Console.WriteLine("ListRange获取Lis范围区间：" + listValuesStr);

            //通过index获取List值，index = row - 1
            var ListGetByIndexValue =  db.ListGetByIndex(key, 8);
            Console.WriteLine("通过index获取值：" + ListGetByIndexValue);

            //通过ListRemove方式 ，使用key删除List中的数据
            db.ListRemove(key, "10");
        }


        /// <summary>
        /// 测试Hash
        /// 是一个string类型的field和value的映射表，特别适合用于存储对象
        /// 相当于将对象的每个字段存成单个string类型
        /// 相当于关系型数据库，key相当于表名，field相当于主键
        /// </summary>
        /// <returns></returns>
        public static void TestHash()
        {
            string key = "UserInfo";
            List<UserInfoDto> list = new List<UserInfoDto>();
            for (int i = 0; i < 3; i++)
            {
                UserInfoDto userInfoDto = new UserInfoDto()
                {
                    Id = i,
                    LastLoginTime = DateTime.Now,
                    Password = "password" + i.ToString(),
                    StaffId = "StaffId_" + i.ToString(),
                    StaffName = "StaffName_" + i.ToString()
                };
                //序列化
                string jsonStr = JsonConvert.SerializeObject(userInfoDto);
                
                //使用HashSet设置值，UserInfo是key，相当于表名，user+i是filed,相当于主键，jsonStr是value
                db.HashSet(key, "user" + i, jsonStr);
                
                //使用HashGet获取值
                string userInfoDtoStr = db.HashGet(key, "user" + i);
                Console.WriteLine("HashGet 获取Hash中的值 key： user" + i + ",value:" + userInfoDtoStr);
                
                //使用HashSet修改值，key存在的情况下，会覆盖之前的value
                db.HashSet(key, "user" + i, "updated:" + jsonStr);
                string userInfoDtoStr2 = db.HashGet(key, "user" + i);
                Console.WriteLine("使用HashSet修改了Value后，获取Hash中的值 key： user" + i + ",value:" + userInfoDtoStr2);
            }

            //使用HashKeys获取Hash所有的keys,这里的key，其实是field
            var keys = db.HashKeys(key);
            foreach (string keyname in keys)
            {
                Console.WriteLine("使用HashKeys得到Hash中所有的key,key:" + keyname);
            }

            //使用HashValues获取Hash所有的keys,这里的key，其实是field
            var values = db.HashValues(key);
            foreach (string value in values)
            {
                Console.WriteLine("使用HashValues得到Hash中所有的value,value:" + value);
            }
            //使用HashDelete，通过key和field删除Hash中的某个值
            db.HashDelete(key, "user1");
        }


        /// <summary>
        /// 测试Set
        /// Set是无序号的，且不能重复的
        /// </summary>
        /// <returns></returns>
        public static void TestSet()
        {
            string key = "TestSet";
            db.KeyDelete(key);
            for (int i = 0; i < 10; i++)
            {
                //使用db.SetAdd向Set中添加元素
                db.SetAdd(key, i);
            }

            //使用SetPop 随机取出并移除Set的一个值
            string popValue = db.SetPop(key);
            Console.WriteLine("使用SetPop 随机取出并移除Set的一个值：" + popValue);

            //使用SetRandomMember随机从Set中获取一值，但不移除
            string randomValue = db.SetRandomMember(key);
            Console.WriteLine("使用SetRandomMember 随机取出并移除Set的一个值：" + randomValue);

            ///使用SetMembers获取Set中所有的值
            var members = db.SetMembers(key);
            foreach (string member in members)
            {
                Console.WriteLine("使用SetMembers Set的值 member：" + member);
            }

            //使用SetRemove删除Set中的值
            db.SetRemove(key, 7);
            var members2 = db.SetMembers(key);
            foreach (string member in members2)
            {
                Console.WriteLine("使用SetRemove删除7以后，Set中的值 member：" + member);
            }
        }



        /// <summary>
        /// 有序Set
        /// SortedSet是有序的，且不能重复
        /// </summary>
        /// <returns></returns>
        public static void TestSortedSet()
        {
            string key = "SortedSet";
            db.KeyDelete(key);
            for (int i = 0; i< 3; i++)
            {
                //使用SortedSetAdd给SortedSet设置值，第三个参数score便是排序位置，会可能存在多个score相同，会整体排列在一起
                db.SortedSetAdd(key, "1这是第" + i, i);
                db.SortedSetAdd(key, "2这是第" + i, i);
            }

            //使用SortedSetRangeByScore 输入score的范围去获取值
            RedisValue[] values =  db.SortedSetRangeByScore(key,0,1, Exclude.None, Order.Descending);
            foreach (string value in values)
            {
                Console.WriteLine("使用SortedSetRangeByScore 根据score的范围去获取值value：" + value);
            }

            //使用SortedSetScore获取SoretedSet中某个值的score
            var score = db.SortedSetScore(key, "1这是第1");
            Console.WriteLine("使用SortedSetRangeByScore 根据输入value去获取值score：" + score);

            //使用SortedSetRemove去删除SoretedSet中的值
            db.SortedSetRemove(key, "1这是第1");

        }
    }
}
