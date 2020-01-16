using jqb.Common.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisHelperTest
{
    class RedisCommonTest
    {
        //创建访问Redis帮助类的实例
        private static ExchangeRedisHelper redisHelprer = new ExchangeRedisHelper();
        //过期时间
        private const int REDIS_EXPIRE_SECONDS = 24 * 60 * 60;

        static void Main(string[] args)
        {
            GetUserName("zhangsan");
            GetUserName("lisi");
            Console.ReadLine();

        }

        /// <summary>
        /// 得到用户信息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetUserName(string name)
        {
            //合并key
            string key = redisHelprer.GetMergeKey(CacheKeyType.UserInfo, name);
            Console.WriteLine("合并key:" + key);
            //从redis从获取值
            string nameValue = redisHelprer.GetFromBinary<string>(key);
            Console.WriteLine("获取到的值key:" + key + ",value:" + nameValue);
            if (string.IsNullOrEmpty(nameValue))
            {
                nameValue = "my name is：" + name;
                //设置值，最后一个参数为过期时间
                redisHelprer.SetToBinary(key, nameValue, REDIS_EXPIRE_SECONDS);
            }
            nameValue = redisHelprer.GetFromBinary<string>(key);
            Console.WriteLine("获取到的值key:" + key + ",value:" + nameValue);
            return nameValue;
        }
    }
}
