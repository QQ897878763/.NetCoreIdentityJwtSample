using IdentityJwtSample.Dto;
using IdentityJwtSample.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityJwtSample.Services
{
    public interface IUserService : IDependency
    {
        TokenDto RefreshToken(TokenDto oldTokenDto);
        TokenDto NewAuthenticate(string username, string password);
        TokenDto Authenticate(string username, string password);
        IEnumerable<User> GetAll();
        User GetById(int id);
        User Create(User user, string password);
        void Update(User user, string password = null);
        void Delete(int id);
    }
}
