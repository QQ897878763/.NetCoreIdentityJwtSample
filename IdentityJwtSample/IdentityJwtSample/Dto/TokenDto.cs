using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityJwtSample.Dto
{
    /// <summary>
    /// Token
    /// </summary>
    public class TokenDto
    {
        /// <summary>
        /// 刷新Token
        /// 用于刷新Token用
        /// </summary>
        public string RefRefreshToken { get; set; }

        /// <summary>
        /// Authorization Token值
        /// </summary>
        public string Token { get; set; }

        public int Id { get; set; }
    }
}
