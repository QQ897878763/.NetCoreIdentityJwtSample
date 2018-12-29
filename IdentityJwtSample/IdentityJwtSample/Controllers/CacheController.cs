using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheManager.Core;
using IdentityJwtSample.Dto;
using IdentityJwtSample.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace IdentityJwtSample.Controllers
{



    [Route("api/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {

        private MemoryCache _cache;
        public CacheController(MyMemoryCache memoryCache)
        {
            _cache = memoryCache.Cache;
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult CacheTryGetValueSet()
        {
            DateTime cacheEntry;

            // Look for cache key.
            if (!_cache.TryGetValue(CacheKeys.Entry, out cacheEntry))
            {
                // Key not in cache, so get data.
                cacheEntry = DateTime.Now;

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1024);

                // Save data in cache.
                _cache.Set(CacheKeys.Entry, cacheEntry, cacheEntryOptions);
            }

            return new JsonResult(cacheEntry);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult CacheGet()
        {
            var cacheEntry = _cache.Get<DateTime?>(CacheKeys.Entry);
            return new JsonResult(cacheEntry);
        }
    }
}
