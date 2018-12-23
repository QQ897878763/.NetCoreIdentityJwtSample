using AutoMapper;
using IdentityJwtSample.Dto;
using IdentityJwtSample.Entities;
using IdentityJwtSample.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IdentityJwtSample.Services
{
    /// <summary>
    /// 用户服务
    /// </summary>
    public class UserService : IUserService
    {
        /*
            这里不使用数据库对用户信息进行持久化，直接将数据存到内存中。真实场景即实现数据表的增删改查即可。
         */
        private readonly AppSettings _appSettings;

        private List<User> _users = new List<User>
        {
            new User { Id = 1,  Username = "test", Password = "test" }
        };

        public UserService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public IEnumerable<User> GetAll()
        {
            return _users;
        }
        public User GetById(int id)
        {
            return _users.Find(p => p.Id == id);
        }


        public User Create(User user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is required");

            if (_users.Any(x => x.Username == user.Username))
                throw new Exception("Username \"" + user.Username + "\" is already taken");

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            _users.Add(user);
            return user;
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public void Update(User userParam, string password = null)
        {
            var user = _users.Find(p => p.Id == userParam.Id);

            if (user == null)
                throw new Exception("User not found");

            if (userParam.Username != user.Username)
            {
                // username has changed so check if the new username is already taken
                if (_users.Any(x => x.Username == userParam.Username))
                    throw new Exception("Username " + userParam.Username + " is already taken");
            }

            // update user properties            
            user.Username = userParam.Username;

            // update password if it was entered
            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }

            _users.Remove(user);
            _users.Add(user);
        }

        public void Delete(int id)
        {
            var user = _users.Find(p => p.Id == id);
            if (user != null)
            {
                _users.Remove(user);
            }
        }

        /// <summary>
        /// 验证身份
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public TokenDto Authenticate(string username, string password)
        {
            var user = _users.SingleOrDefault(x => x.Username == username && x.Password == password);

            if (user == null)
                return null;

            TokenDto output = CreateToken(user);
            return output;
        }

        private TokenDto CreateToken(User user)
        {
            // JwtSecurityTokenHandler可以创建Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            DateTime tokenExpires = DateTime.UtcNow.AddMinutes(7);
            DateTime refRefreshTokenExpires = tokenExpires.AddMinutes(-1);
            var tokenDescriptor = new SecurityTokenDescriptor
            {

                Subject = new ClaimsIdentity(new Claim[]
                {
                    //添加申明，申明可以自定义，可以无限扩展，对于后续的身份验证通过后的授权特别有用...
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim("RefRefreshTokenExpires",refRefreshTokenExpires.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7), //过期时间这里写死为7天
                IssuedAt = DateTime.Now, //Token发布时间
                Audience = "AuthTest", //接收令牌的受众
                //根据配置文件的私钥值设置Token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            TokenDto output = Mapper.Map<TokenDto>(user);
            output.RefRefreshToken = Guid.NewGuid().ToString();
            output.RefRefreshTokenExpires = refRefreshTokenExpires;
            output.Token = user.Token;
            return output;
        }

        /// <summary>
        /// 刷新令牌
        /// 必须在当前Token有效期内刷新，可以由前端主动发起刷新请求，
        /// 也可以在后端每次接口验证时候根据Token的过期时间与当前时间判断，如果时间相近到一个最小值则主动刷新Token。这种方式
        /// 不好的地方在于对每个请求的响应值里都必须添加Token和RefRefreshToken(因为后台刷新后前端需要保存，不然下次请求带的Token后端会验证失败) 
        /// 推荐的做法是服务端新增一个RefRefreshTokenExpires(刷新Token有效期)给前端，目的是在前端在请求返回401后验证当前时间是否小于
        /// RefRefreshTokenExpires(刷新Token有效期)，如果小于则调用刷新Token这个API获取到服务端颁发的新Token(用户无感知方式授权)，
        /// 否则让用户重新登录(用户有感知方式授权);
        /// </summary>
        /// <param name="oldTokenDto"></param>
        /// <returns></returns>
        public TokenDto RefreshToken(TokenDto oldTokenDto)
        {
            TokenDto output = new TokenDto();
            //如果有刷新Token值对应的用户则刷新Token以及RefRefreshToken
            var user = _users.FirstOrDefault(p => p.RefRefreshToken == oldTokenDto.RefRefreshToken
                                            && oldTokenDto.RefRefreshTokenExpires > DateTime.Now);
            if (user != null)
            {
                output = CreateToken(user);
            }

            return output;
        }



        public TokenDto NewAuthenticate(string username, string password)
        {
            User user = _users.SingleOrDefault(x => x.Username == username && x.Password == password);
            if (user == null)
                return null;
            //校验密码
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;
            TokenDto output = Mapper.Map<TokenDto>(user);
            return output;
        }


        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("密码为空");

            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");

            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }
            return true;
        }
    }
}
