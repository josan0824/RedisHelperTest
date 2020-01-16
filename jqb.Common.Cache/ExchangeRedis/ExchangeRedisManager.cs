using StackExchange.Redis;

namespace jqb.Common.Cache
{
    /// <summary>
    /// ExchangeRedis管理类
    /// </summary>
    public class ExchangeRedisManager
    {
        //锁对象
        private static object _lockObj = new object();

        //ExchangeRedis连接对象
        private static ConnectionMultiplexer _redis;

        //私有化构造函数
        private ExchangeRedisManager()
        {

        }

        /// <summary>
        /// 获取ConnectionMultiplexer对象
        /// </summary>
        /// <param name="redisHost">Redis访问地址</param>
        public static ConnectionMultiplexer GetConnectionMultiplexer(string redisHost)
        {
            if (_redis == null)
            {
                lock (_lockObj)
                {
                    if (_redis == null)
                    {
                        _redis = GetManager(redisHost);
                    }
                    return _redis;
                }
            }

            return _redis;
        }

        /// <summary>
        /// 获取当前redis对象的数据库连接对象
        /// </summary>
        /// <param name="dbIndex">数据存放地址</param>
        /// <param name="redisHost">redis访问地址</param>
        /// <returns></returns>
        public static IDatabase GetDatabase(int dbIndex, string redisHost)
        {
            if (_redis == null)
            {
                //获取redis访问对象
                _redis = GetConnectionMultiplexer(redisHost);
            }
            return _redis.GetDatabase(dbIndex);
        }

        /// <summary>
        /// 外部传参获取Redis管理对象
        /// </summary>
        /// <param name="redisHost">redis服务地址连接字符串，多个以逗号隔开</param>
        /// <returns></returns>
        private static ConnectionMultiplexer GetManager(string redisHost)
        {
            if (string.IsNullOrEmpty(redisHost))
            {
                return null;
            }
            return ConnectionMultiplexer.Connect(redisHost);
        }

        /// <summary>
        /// 通过配置对象的方式获取Redis管理对象
        /// </summary>
        /// <param name="redisHost>redis服务地址连接字符串，多个以逗号隔开</param>
        /// <returns></returns>
        private static ConnectionMultiplexer GetManagerByConfig(string redisHost)
        {
            //Redis配置文件
            ConfigurationOptions redisConfig = ConfigurationOptions.Parse(redisHost);

            //如果配置对象不为空，则创建一个redis管理对象并返回
            if (redisConfig != null)
            {
                return ConnectionMultiplexer.Connect(redisConfig);
            }

            return null;
        }
    }
}
