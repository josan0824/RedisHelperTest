using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisHelperTest
{
    class UserInfoDto
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public string StaffName { get; set; }
        public string Password { get; set; }
        public System.DateTime LastLoginTime { get; set; }
    }
}
