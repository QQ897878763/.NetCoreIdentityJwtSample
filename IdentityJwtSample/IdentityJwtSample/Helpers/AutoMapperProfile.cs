using IdentityJwtSample.Dto;
using IdentityJwtSample.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace IdentityJwtSample.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
            CreateMap<TokenDto, User>();
            CreateMap<User, TokenDto>();
        }
    }
}
