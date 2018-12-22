using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityJwtSample.Dto
{
    public class BaseApiResult
    {
        public bool IsSucess { get; set; }

        public string Message { get; set; } = "成功";
    }

    public class BaseApiResult<T> : BaseApiResult
    {
        public T Data { get; set; }
    }
}
