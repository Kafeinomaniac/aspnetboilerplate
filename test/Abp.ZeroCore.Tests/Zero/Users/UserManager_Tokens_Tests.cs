﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.ZeroCore.SampleApp.Core;
using Shouldly;
using Xunit;

namespace Abp.Zero.Users
{
    public class UserManager_Tokens_Tests : AbpZeroTestBase
    {
        private readonly AbpUserManager<Role, User> _abpUserManager;
        private readonly IRepository<UserToken, long> _userTokenRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public UserManager_Tokens_Tests()
        {
            _abpUserManager = Resolve<AbpUserManager<Role, User>>();
            _userTokenRepository = Resolve<IRepository<UserToken, long>>();
            _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
        }

        [Fact]
        public async Task Should_Add_TokenValidityKey()
        {
            using (var uow = Resolve<IUnitOfWorkManager>().Begin())
            {
                var user = await _abpUserManager.GetUserByIdAsync(AbpSession.GetUserId());
                var tokenValidityKey = Guid.NewGuid().ToString();
                await _abpUserManager.AddTokenValidityKeyAsync(user, tokenValidityKey, DateTime.UtcNow.AddDays(1));
                var isTokenValidityKeyValid = await _abpUserManager.IsTokenValidityKeyValidAsync(user, tokenValidityKey);

                isTokenValidityKeyValid.ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Should_Not_Valid_Expired_TokenValidityKey()
        {
            using (var uow = Resolve<IUnitOfWorkManager>().Begin())
            {
                var user = await _abpUserManager.GetUserByIdAsync(AbpSession.GetUserId());
                var tokenValidityKey = Guid.NewGuid().ToString();
                await _abpUserManager.AddTokenValidityKeyAsync(user, tokenValidityKey, DateTime.UtcNow);
                var isTokenValidityKeyValid = await _abpUserManager.IsTokenValidityKeyValidAsync(user, tokenValidityKey);

                isTokenValidityKeyValid.ShouldBeFalse();
            }
        }

        [Fact]
        public async Task Should_Remove_Expired_TokenValidityKeys()
        {
            using (_unitOfWorkManager.Begin())
            {
                var user = await _abpUserManager.GetUserByIdAsync(AbpSession.GetUserId());

                await _abpUserManager.AddTokenValidityKeyAsync(user, Guid.NewGuid().ToString(), DateTime.UtcNow);
                await _abpUserManager.AddTokenValidityKeyAsync(user, Guid.NewGuid().ToString(), DateTime.UtcNow.AddDays(1));
                await _abpUserManager.AddTokenValidityKeyAsync(user, Guid.NewGuid().ToString(), DateTime.UtcNow.AddDays(1));
                _unitOfWorkManager.Current.SaveChanges();

                var allTokens = _userTokenRepository.GetAllList(t => t.UserId == user.Id);
                allTokens.Count.ShouldBe(3);
            }

            using (_unitOfWorkManager.Begin())
            {
                var user = await _abpUserManager.GetUserByIdAsync(AbpSession.GetUserId());

                await _abpUserManager.IsTokenValidityKeyValidAsync(user, Guid.NewGuid().ToString());
                _unitOfWorkManager.Current.SaveChanges();

                var allTokens = _userTokenRepository.GetAllList(t => t.UserId == user.Id);
                allTokens.Count.ShouldBe(2);
            }
        }
    }
}
