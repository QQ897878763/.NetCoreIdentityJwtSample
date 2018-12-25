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
        /// 刷新Token有效期
        /// 使用Token访问API提示401后判断该值是否大于当前时间，如果大于则表示可以调用刷新Token API
        /// </summary>
        public string RefRefreshTokenExpires { get; set; }

        /// <summary>
        /// Authorization Token值
        /// </summary>
        public string Token { get; set; }

        public int Id { get; set; }
    }
}
