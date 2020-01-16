using ProtoBuf;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace jqb.Common.Cache
{
    /// <summary>
    /// ExchangeRedis访问Redis帮助类
    /// </summary>
    public class ExchangeRedisHelper
    {
        //redis数据访问对象
        private IDatabase db = null;

        //锁对象key
        private string lockKey = "lockkey";
        //锁对象token值
        private string lockToken = "lockkeytoken";
        private readonly ServerEnumerationStrategy serverEnumerationStrategy = new ServerEnumerationStrategy();

        //不设置过期时间，默认一星期过期
        private int _defaultExpireSeconds = 604800;

        #region 初始化Redis
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public ExchangeRedisHelper()
        {
            //实例化db
            db = ExchangeRedisManager.GetDatabase(0, GetRedisHost());
        }

        /// <summary>
        /// 构造函数
        
            /// </summary>
        /// <param name="dbIndex">数据库db访问索引</param>
        public ExchangeRedisHelper(int dbIndex = 0)
        {
            //实例化db
            db = ExchangeRedisManager.GetDatabase(dbIndex, GetRedisHost());
        }

        /// <summary>
        /// 获取Redis连接字符串
        /// </summary>
        /// <returns></returns>
        private string GetRedisHost()
        {
            //Redis连接字符串格式:127.0.0.1:6380,redis1:6380,allowAdmin=true,ssl=true,password=
            try
            {
                string redisHost = ConfigurationManager.AppSettings["RedisHostList"];
                return redisHost;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Redis连接配置RedisHostList未配置!");
            }
        }

        /// <summary>
        /// 获取当前Redis所在的数据库
        /// </summary>
        /// <returns>IDatabase对象实例</returns>
        public IDatabase GetDatabase()
        {
            return db;
        }
        
        #endregion


        #region 新增(修改)键值方法
        /// <summary>
        /// 写入指定的key(默认一周过期，可以自己设置)
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <param name="value">存取的值,可以是字符串,也可以是二进制数据</param>
        /// <param name="expireSeconds">过期时间(秒)</param>
        /// <returns></returns>
        public bool Set(string key, string value, int expireSeconds = 0)
        {
            if (expireSeconds <= 0)
            {
                //设置了过期时间
                expireSeconds = _defaultExpireSeconds;
            }

            return db.StringSet(key, value, TimeSpan.FromSeconds(expireSeconds));
        }

        /// <summary>
        /// 写入指定的key泛型对象(二进制)(默认一周过期，可以自己设置)
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <param name="obj">泛型对象</param>
        /// <param name="expireSeconds">过期时间(秒)</param>
        /// <returns></returns>
        public bool SetToBinary<T>(string key, T obj, int expireSeconds = 0)
        {
            //对象序列化成二进制数组
            var byteValue = Serialize(obj);
            if (expireSeconds <= 0)
            {
                //设置了过期时间
                expireSeconds = _defaultExpireSeconds;
            }
            return db.StringSet(key, byteValue, TimeSpan.FromSeconds(expireSeconds));

        }

        /// <summary>
        /// 写入指定的key(永不过期,非特殊情况不要使用)
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <param name="value">存取的值,可以是字符串,也可以是二进制数据</param>
        /// <returns></returns>
        public bool SetNoExpire(string key, string value)
        {
            return db.StringSet(key, value);
        }

        /// <summary>
        /// 写入指定的key泛型对象(二进制)(永不过期,非特殊情况不要使用)
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <param name="obj">泛型对象</param>
        /// <returns></returns>
        public bool SetToBinaryNoExpire<T>(string key, T obj)
        {
            //对象序列化成二进制数组
            var byteValue = Serialize(obj);
            return db.StringSet(key, byteValue);
        }

        /// <summary>
        /// 批量存值
        /// </summary>
        /// <param name="keysStr">批量key数组</param>
        /// <param name="valuesStr">批量value数组</param>
        /// <returns></returns>
        public bool SetMany(string[] keysStr, string[] valuesStr)
        {
            var count = keysStr.Length;
            var keyValuePair = new KeyValuePair<RedisKey, RedisValue>[count];
            for (int i = 0; i < count; i++)
            {
                keyValuePair[i] = new KeyValuePair<RedisKey, RedisValue>(keysStr[i], valuesStr[i]);
            }
            //批量保存键值对
            return db.StringSet(keyValuePair);
        }
        #endregion

        #region 获取指定key的值+Get
        /// <summary>
        /// 获取指定key的值
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <returns></returns>
        public string Get(string key)
        {
            return db.StringGet(key);
        }

        /// <summary>
        /// 获取指定key的对象(字符串)
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <returns></returns>
        public T GetFromBinary<T>(string key)
        {
            //获取当前键存取的值
            var value = db.StringGet(key);
            if (value.IsNullOrEmpty)
            {
                return default(T);
            }
            return DeSerialize<T>(value);
        }
        #endregion

        #region ProtoBuf序列化对象及反序列化
        /// <summary>
        /// 对象序列化成二进制
        /// </summary>
        /// <typeparam name="T">序列化对象类型</typeparam>
        /// <param name="obj">对象的值</param>
        /// <returns></returns>
        public static byte[] Serialize<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                //ProtoBuf序列化
                Serializer.SerializeWithLengthPrefix<T>(ms, obj, PrefixStyle.Base128);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 二进制对象反序列化成对象
        /// </summary>
        /// <typeparam name="T">反序列化对象类型</typeparam>
        /// <param name="bytes">二进制对象数组</param>
        /// <returns></returns>
        public static T DeSerialize<T>(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return Serializer.DeserializeWithLengthPrefix<T>(ms, PrefixStyle.Base128);
            }
        }
        #endregion

        #region 判断是否存在指定key+ContainsKey
        /// <summary>
        /// 判断是否存在指定key
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return db.KeyExists(key);
        }
        #endregion

        #region 重命名指定Key为新Key+RenameKey
        /// <summary>
        /// 修改指定Key为新Key
        /// </summary>
        /// <param name="oldKey">旧key</param>
        /// <param name="newKey">新key</param>
        /// <returns></returns>
        public bool RenameKey(string oldKey, string newKey)
        {
            bool result = false;
            if (this.ContainsKey(oldKey) && !this.ContainsKey(newKey))
            {
                result = db.KeyRename(oldKey, newKey);
            }

            return result;
        }
        #endregion

        #region 合并枚举类型的key
        /// <summary>
        /// 合并枚举类型的key
        /// </summary>
        /// <param name="type">缓存类型</param>
        /// <param name="key">缓存key</param>
        /// <returns></returns>
        public string GetMergeKey(CacheKeyType type, string key="")
        {
            var tempKey = type.ToString("G");
            if (!string.IsNullOrEmpty(key))
            {
                tempKey=$"{tempKey}:{key}";
            }
            return tempKey;
        }
        #endregion

        #region 删除指定的key+Remove
        /// <summary>
        /// 删除指定的key
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            if (ContainsKey(key))
                return db.KeyDelete(key);
            return false;
        }

        #endregion

        #region 发布订阅消息相关方法
        /// <summary>
        /// 消息发布(生产者发布消息)
        /// </summary>
        /// <param name="redisChannel">发布的渠道</param>
        /// <param name="message">发布的消息内容</param>
        /// <returns></returns>
        public long PublishMsg(string redisChannel, string message)
        {
            //获取生产者
            ISubscriber sub = GetSubscriber();
            return sub.Publish(redisChannel, message);
        }

        /// <summary>
        /// 获取生产者对象
        /// </summary>
        /// <returns></returns>
        private ISubscriber GetSubscriber()
        {
            //redis管理对象
            var redis = ExchangeRedisManager.GetConnectionMultiplexer(GetRedisHost());
            //关闭消息有序传递，使消息并行传递
            redis.PreserveAsyncOrder = false;
            //返回生产者
            return redis.GetSubscriber();
        }

        /// <summary>
        /// 订阅(消费者订阅消息并消费)
        /// </summary>
        /// <param name="redisChannel">订阅的渠道</param>
        /// <param name="actionHandler">订阅消息处理handler</param>
        /// <returns></returns>
        public void Subscriber(string redisChannel, Action<RedisChannel, RedisValue> actionHandler)
        {
            //获取生产者
            ISubscriber sub = GetSubscriber();
            //生产者订阅消息
            sub.Subscribe(redisChannel, actionHandler);
        }

        /// <summary>
        /// 取消消息订阅
        /// </summary>
        /// <param name="redisChannel">订阅的渠道</param>
        /// <param name="actionHandler">订阅消息处理handler</param>
        /// <returns></returns>
        public void Unsubscribe(string redisChannel, Action<RedisChannel, RedisValue> actionHandler)
        {
            //获取生产者
            ISubscriber sub = GetSubscriber();
            //生产者取消订阅事件
            sub.Unsubscribe(redisChannel, actionHandler);
        }
        #endregion

        #region 队列(左边放，右边取)
        /// <summary>
        /// 数据写入到指定的队列中
        /// </summary>
        /// <param name="queueName">队列名</param>
        /// <param name="value">redisValue对象</param>
        /// <returns></returns>
        public bool PushToQueue(string queueName, string value)
        {
            long result = db.ListLeftPush(queueName, value);
            return result > 0;
        }

        /// <summary>
        /// 从指定的队列中获取数据
        /// </summary>
        /// <param name="queueName">队列名</param>
        /// <returns></returns>
        public object PopFromQueue(string queueName)
        {
            object result = db.ListRightPop(queueName);
            return result;
        }

        /// <summary>
        /// 获取指定队列的长度
        /// </summary>
        /// <param name="queueName">队列名</param>
        /// <returns></returns>
        public long GetListLength(string queueName)
        {
            return db.ListLength(queueName);
        }

        #endregion

        #region 栈(左边放，左边取)
        /// <summary>
        /// 数据写入到指定的栈中
        /// </summary>
        /// <param name="stackName">栈名</param>
        /// <param name="value">redisValue对象</param>
        /// <returns></returns>
        public bool PushToStack(string stackName, RedisValue value)
        {
            long result = db.ListLeftPush(stackName, value);
            return result > 0;
        }

        /// <summary>
        /// 从指定的栈中获取数据
        /// </summary>
        /// <param name="stackName">栈名</param>
        /// <returns></returns>
        public object PopFromStack(string stackName)
        {
            object result = db.ListLeftPop(stackName);
            return result;
        }

        #endregion

        #region 加锁设置某个Key的值
        /// <summary>
        /// 加锁设置某个Key的值
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <param name="value">存取的值,可以是字符串,也可以是二进制数据</param>
        /// <param name="tryCount">失败尝试次数(建议30次)</param>
        /// <param name="expireTime">锁过期时间(建议2秒)</param>
        /// <returns></returns>
        public bool SetByLock(string key, string value, int tryCount, TimeSpan expireTime)
        {
            bool result = false;
            int count = 0;
            while (count < tryCount)
            {
                //当前程序锁一分钟
                if (db.LockTake(lockKey, lockToken, expireTime))
                {
                    try
                    {
                        //设置新的值
                        this.Set(key, value);
                        result = true;
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        //释放锁
                        db.LockRelease(lockKey, lockToken);
                    }
                    //跳出当前while循环
                    break;
                }
                else
                {
                    count++;
                    //当前线程可适当休眠一段时间
                    continue;
                }
            }
            return result;
        }
        #endregion

        #region 给指定的key设置过期时间+SetExpireTime
        /// <summary>
        /// 给指定的key设置过期时间
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <param name="dateTime">指定的时间过期</param>
        /// <returns></returns>
        public bool SetExpireTime(string key, DateTime dateTime)
        {
            bool result = false;
            if (ContainsKey(key))
            {
                result = db.KeyExpire(key, dateTime);
            }

            return result;
        }
        #endregion

        #region 给指定的key设置多久之后过期+SetExpireTime
        /// <summary>
        /// 给指定的key设置多久之后过期
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <param name="expiry">设置多久之后过期</param>
        /// <returns></returns>
        public bool SetExpireTime(string key, TimeSpan expiry)
        {
            bool result = false;
            if (ContainsKey(key))
            {
                result = db.KeyExpire(key, expiry);
            }

            return result;
        }
        #endregion

        #region 获取key的过期时间(返回秒)+GetExpireSeconds
        /// <summary>
        /// 获取key的过期时间(返回秒)
        /// </summary>
        /// <param name="key">Redis存放对象的key</param>
        /// <returns></returns>
        public double GetExpireSeconds(string key)
        {
            double result = -1;
            if (ContainsKey(key))
            {
                var expireTime = db.KeyTimeToLive(key);
                result = expireTime.HasValue ? expireTime.Value.TotalSeconds : -1;
            }

            return result;
        }
        #endregion

        #region 原子性操作
        public long HashIncrement(string setKey, string memberValue)
        {
            return db.HashIncrement(setKey, memberValue);
        }

        public long HashIncrement(string setKey, string memberValue, long value)
        {
            return db.HashIncrement(setKey, memberValue, value);
        }

        public long HashDecrement(string setKey, string memberValue)
        {
            return db.HashDecrement(setKey, memberValue);
        }

        public long HashDecrement(string setKey, string memberValue, long value)
        {
            return db.HashDecrement(setKey, memberValue, value);
        }

        public double SortedSetIncrement(string setKey, string memberValue, double value)
        {
            return db.SortedSetIncrement(setKey, memberValue, value);
        }

        public double SortedSetDecrement(string setKey, string memberValue, double value)
        {
            return db.SortedSetDecrement(setKey, memberValue, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setKey"></param>
        /// <returns></returns>
        public long StringIncrement(string setKey)
        {
            return db.StringIncrement(setKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long StringIncrement(string setKey, long value)
        {
            return db.StringIncrement(setKey, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setKey"></param>
        /// <returns></returns>
        public long StringDecrement(string setKey)
        {
            return db.StringDecrement(setKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setKey"></param>
        /// <returns></returns>
        public long StringDecrement(string setKey, long value)
        {
            return db.StringDecrement(setKey, value);
        }
        #endregion

        #region 有序列表SortedSet相关操作

        #region SortedSet新增member值+SortedSetAdd
        /// <summary>
        /// SortedSet新增member值
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <param name="memberValue">SortedSet中member的value字段</param>
        /// <param name="score">memberValue对应的score</param>
        /// <returns></returns>
        public bool SortedSetAdd(string setKey, string memberValue, double score)
        {
            return db.SortedSetAdd(setKey, memberValue, score);
        }
        #endregion

        #region 获取有序列表对应key的score+GetSortedSetScore
        /// <summary>
        /// 获取有序列表对应key的score
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <param name="memberValue">SortedSet中member的值</param>
        /// <returns></returns>
        public double? GetSortedSetScore(string setKey, string memberValue)
        {
            return db.SortedSetScore(setKey, memberValue);
        }
        #endregion

        #region 获取有序列表key的长度+GetSortedSetLength
        /// <summary>
        /// 获取有序列表key的长度
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <returns></returns>
        public long GetSortedSetLength(string setKey)
        {
            return db.SortedSetLength(setKey);
        }
        #endregion

        #region 获取有序列表key指定范围的值Value(升序)+GetSortedSetRange
        /// <summary>
        /// 获取有序列表key指定范围的值Value(升序)
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <param name="start">开始位置</param>
        /// <param name="end">结束位置</param>
        /// <returns></returns>
        public RedisValue[] GetSortedSetRange(string setKey, long start = 0, long end = -1)
        {
            return db.SortedSetRangeByRank(setKey, start, end);
        }
        #endregion

        #region 获取有序列表key指定范围的值Value(降序)+GetSortedSetRange
        /// <summary>
        /// 获取有序列表key指定范围的值Value(降序)
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <param name="start">开始位置</param>
        /// <param name="end">结束位置</param>
        /// <returns></returns>
        public string[] GetSortedSetRangeDesc(string setKey, long start = 0, long end = -1)
        {
            return db.SortedSetRangeByRank(setKey, start, end, Order.Descending).ToStringArray();
        }
        #endregion

        #region 获取有序列表key所有的值Value(升序)+GetAllSortedSet
        /// <summary>
        /// 获取有序列表key所有的值Value(升序)
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <returns></returns>
        public string[] GetAllSortedSet(string setKey)
        {
            return db.SortedSetRangeByRank(setKey, 0, -1).ToStringArray();
        }
        #endregion

        #region 获取有序列表key所有的值Value(降序)+GetAllSortedSet
        /// <summary>
        /// 获取有序列表key所有的值Value(降序)
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <returns></returns>
        public string[] GetAllSortedSetDesc(string setKey)
        {
            return db.SortedSetRangeByRank(setKey, 0, -1, Order.Descending).ToStringArray();
        }
        #endregion

        #region 获取有序列表key中value对应的索引+GetSortedSetRange
        /// <summary>
        /// 获取有序列表key指定范围的值Value
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <param name="memberValue">SortedSet中member的值</param>
        /// <returns></returns>
        public long GetSortedSetIndex(string setKey, string memberValue)
        {
            long? index = db.SortedSetRank(setKey, memberValue);

            return index.HasValue ? index.Value + 1 : -1;
        }
        #endregion

        #region 获取有序列表key中value对应的索引(降序)+GetSortedSetRange
        /// <summary>
        /// 获取有序列表key指定范围的值Value(降序)
        /// </summary>
        /// <param name="setKey">SortedSet的key</param>
        /// <param name="memberValue">SortedSet中member的值</param>
        /// <returns></returns>
        public long GetSortedSetIndexDesc(string setKey, string memberValue)
        {
            long? index = db.SortedSetRank(setKey, memberValue, Order.Descending);
            return index.HasValue ? index.Value + 1 : -1;
        }
        #endregion

        #endregion

        #region Hash操作

        #region 检查Hash表中某个字段值是否存在
        /// <summary>
        /// 检查Hash表中某个字段值是否存在
        /// </summary>
        /// <param name="hashKey">Hash列表的key</param>
        /// <param name="hashField">Hash表中具体的字段</param>
        /// <returns></returns>
        public bool HashExists(string hashKey, string hashField)
        {
            return db.HashExists(hashKey, hashField);
        }
        #endregion

        #region 获取Hash表中的某个字段的值
        /// <summary>
        /// 获取Hash表中的某个字段的值
        /// </summary>
        /// <param name="hashKey">Hash列表的key</param>
        /// <param name="hashField">Hash表中具体的字段</param>
        /// <returns></returns>
        public RedisValue GetHashValue(string hashKey, string hashField)
        {
            return db.HashGet(hashKey, hashField);
        }

        /// <summary>
        /// 获取Hash所有值
        /// </summary>
        /// <param name="hashKey"></param>
        /// <returns></returns>
        public HashEntry[] HashGetAll(string hashKey)
        {
            return db.HashGetAll(hashKey);
        }

        /// <summary>
        /// 获取Hash的长度
        /// </summary>
        /// <param name="hashKey"></param>
        /// <returns></returns>
        public long HashLength(string hashKey)
        {
            return db.HashLength(hashKey);
        }
        #endregion

        #region 设置Hash表中某个字段的值
        /// <summary>
        /// 设置Hash表中某个字段的值
        /// </summary>
        /// <param name="hashKey">Hash列表的key</param>
        /// <param name="hashField">Hash表中具体的字段</param>
        /// <param name="hashValue">Hash值</param>
        /// <returns></returns>
        public bool SetHashValue(string hashKey, string hashField, RedisValue hashValue)
        {
            return db.HashSet(hashKey, hashField, hashValue);
        }
        #endregion

        #region 删除Hash表中某个字段
        /// <summary>
        /// 删除Hash表中某个字段
        /// </summary>
        /// <param name="hashKey">Hash列表的key</param>
        /// <param name="hashField">Hash表中具体的字段</param>
        /// <returns></returns>
        public bool RemoveHashField(string hashKey, string hashField)
        {
            return db.HashDelete(hashKey, hashField);
        }
        #endregion

        #endregion

        #region List操作

        #region List新增值+ListAdd
        /// <summary>
        /// List新增值
        /// </summary>
        /// <param name="setKey"> List的key</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public long ListAdd(string setKey, string value)
        {
            return db.ListRightPush(setKey, value);
        }
        #endregion

        #region List删除+ListRemove
        /// <summary>
        /// List删除某个key中的指定值
        /// </summary>
        /// <param name="setKey"> List的key</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public long ListRemove(string setKey, string value)
        {
            return db.ListRemove(setKey, value);
        }
        #endregion

        #region 获取list所有值+GetListAllItems
        /// <summary>
        /// 获取list所有值
        /// </summary>
        /// <param name="setKey"> List的key</param>
        /// <returns></returns>
        public string[] GetListAllItems(string setKey)
        {
            return db.ListRange(setKey, 0, -1).ToStringArray();
        }
        #endregion
        /// <summary>
        /// 获取指定标志的Keys
        /// (注意)必须要按照正则表达式查询
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public IEnumerable<RedisKey> GetKeys(string pattern)
        {
            var keys = new List<RedisKey>();
            var multiplexer = db.Multiplexer;
            var servers = ServerIteratorFactory.GetServers(multiplexer, serverEnumerationStrategy).ToArray();
            if (!servers.Any())
            {
                throw new Exception("No server found to serve the KEYS command.");
            }
            if (servers.Count() == 1)
            {
                //如果server的数量为1直接返回keys对象
                var dbKeys = servers.FirstOrDefault().Keys(db.Database, pattern);
                return dbKeys;
            }
            else
            {
                foreach (var server in servers)
                {
                    var dbKeys = server.Keys(db.Database, pattern);
                    keys.AddRange(dbKeys);
                }

            }
            return keys;
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public bool RemoveKeys(RedisKey[] keys)
        {

            return db.KeyDelete(keys) > 0;

        }
        #endregion

    }
}
