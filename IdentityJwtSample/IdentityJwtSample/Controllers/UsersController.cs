﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityJwtSample.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AutoMapper;
using IdentityJwtSample.Helpers;
using Microsoft.AspNetCore.Authorization;
using IdentityJwtSample.Dto;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using IdentityJwtSample.Entities;

namespace IdentityJwtSample.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly IUserService _userService;

        public UsersController(IUserService userService, IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _appSettings = appSettings.Value;
        }


        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register([FromBody]UserDto userDto)
        {
            BaseApiResult output = new BaseApiResult();
            try
            {
                var user = Mapper.Map<User>(userDto);
                _userService.Create(user, userDto.Password);
                output.IsSucess = true;
                output.Message = "注册成功!";
            }
            catch (Exception ex)
            {
                output.Message = $"注册失败!原因:{ex.Message}";
            }
            return new JsonResult(output);
        }

        /// <summary>
        /// 获取授权
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("Authenticate")]
        public IActionResult Authenticate([FromBody]UserDto userDto)
        {
            BaseApiResult<TokenDto> output = new BaseApiResult<TokenDto>();
            try
            {
                TokenDto tokenData = _userService.Authenticate(userDto.Username, userDto.Password);
 
                output.IsSucess = true;
                output.Data = tokenData ?? throw new Exception($"未找到名称为[{userDto.Username}]的用户信息");
                output.Message = "获取授权成功!";
            }
            catch (Exception ex)
            {
                output.Message = ex.Message;
            }
            return new JsonResult(output);
        }

        /// <summary>
        /// 刷新令牌
        /// </summary>
        /// <param name="oldTokenDto"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Token")]
        public IActionResult RefreshToken([FromBody]TokenDto oldTokenDto)
        {
            BaseApiResult<TokenDto> output = new BaseApiResult<TokenDto>();
            try
            {
                output.Data = _userService.RefreshToken(oldTokenDto);
                if (output.Data != null)
                    output.IsSucess = true;
                else {
                    output.Message = "刷新Token失败!清检查是否已过刷新时间!";
                }
            }
            catch (Exception ex)
            {
                output.Message = ex.Message;
            }
            return new JsonResult(output);
        }

        /// <summary>
        /// 获取所有用户信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAll()
        {
            BaseApiResult<IList<UserDto>> output = new BaseApiResult<IList<UserDto>>();
            try
            {
                var users = _userService.GetAll();
                var userDtos = Mapper.Map<IList<UserDto>>(users);
                output.Data = userDtos;
                output.IsSucess = true;
            }
            catch (Exception ex)
            {
                output.Message = ex.Message;
            }
            return new JsonResult(output);
        }

        /// <summary>
        /// 根据ID获取制定用户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{Id}")]
        public IActionResult GetById(int id)
        {
            BaseApiResult<UserDto> output = new BaseApiResult<UserDto>();
            try
            {
                var user = _userService.GetById(id);
                var userDto = Mapper.Map<UserDto>(user);
                output.Data = userDto;
                output.IsSucess = true;
            }
            catch (Exception ex)
            {
                output.Message = ex.Message;
            }
            return new JsonResult(output);
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [HttpPut("{Id}")]
        public IActionResult Update(int id, [FromBody]UserDto userDto)
        {
            BaseApiResult output = new BaseApiResult();
            var user = Mapper.Map<User>(userDto);
            user.Id = id;
            try
            {
                _userService.Update(user, userDto.Password);
                output.IsSucess = true;
            }
            catch (Exception ex)
            {
                output.Message = ex.Message;
            }
            return new JsonResult(output);
        }

        /// <summary>
        /// 删除用户信息
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpDelete("{Id}")]
        public IActionResult Delete(int id)
        {
            BaseApiResult output = new BaseApiResult();
            try
            {
                _userService.Delete(id);
                output.IsSucess = true;
            }
            catch (Exception ex)
            {
                output.Message = ex.Message;
            }
            return new JsonResult(output);
        }
    }
}