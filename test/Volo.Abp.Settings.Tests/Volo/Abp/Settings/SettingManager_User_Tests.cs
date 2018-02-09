﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.Session;
using Xunit;

namespace Volo.Abp.Settings
{
    public class SettingManager_User_Tests: AbpSettingsTestBase
    {
        private Guid _currentUserId;

        private readonly ISettingManager _settingManager;

        public SettingManager_User_Tests()
        {
            _settingManager = GetRequiredService<ISettingManager>();
        }

        protected override void AfterAddApplication(IServiceCollection services)
        {
            var currentUser = Substitute.For<ICurrentUser>();
            currentUser.Id.Returns(ci => _currentUserId);
            services.AddSingleton(currentUser);
        }

        [Fact]
        public async Task Should_Get_From_Store_For_Given_User()
        {
            (await _settingManager.GetOrNullForUserAsync("MySetting2", AbpIdentityTestDataBuilder.User1Id)).ShouldBe("user1-store-value");
            (await _settingManager.GetOrNullForUserAsync("MySetting2", AbpIdentityTestDataBuilder.User2Id)).ShouldBe("user2-store-value");
        }

        [Fact]
        public async Task Should_Fallback_To_Default_Store_Value_When_No_Value_For_Given_User()
        {
            (await _settingManager.GetOrNullForUserAsync("MySetting2", Guid.NewGuid())).ShouldBe("default-store-value");
        }

        [Fact]
        public async Task Should_Not_Fallback_To_Default_Store_Value_When_No_Value_For_Given_User_But_Specified_Fallback_As_False()
        {
            (await _settingManager.GetOrNullForUserAsync("MySetting2", Guid.NewGuid(), fallback: false)).ShouldBeNull();
        }

        [Fact]
        public async Task Should_Get_From_Store_For_Current_User()
        {
            _currentUserId = AbpIdentityTestDataBuilder.User1Id;
            (await _settingManager.GetOrNullAsync("MySetting2")).ShouldBe("user1-store-value");

            _currentUserId = AbpIdentityTestDataBuilder.User2Id;
            (await _settingManager.GetOrNullAsync("MySetting2")).ShouldBe("user2-store-value");
        }

        [Fact]
        public async Task Should_Fallback_To_Default_Store_Value_When_No_Value_For_Current_User()
        {
            _currentUserId = Guid.NewGuid();
            (await _settingManager.GetOrNullAsync("MySetting2")).ShouldBe("default-store-value");
        }

        [Fact]
        public async Task Should_Get_From_Store_For_Current_User_With_GetOrNullForCurrentUserAsync()
        {
            _currentUserId = AbpIdentityTestDataBuilder.User1Id;
            (await _settingManager.GetOrNullForCurrentUserAsync("MySetting2")).ShouldBe("user1-store-value");

            _currentUserId = AbpIdentityTestDataBuilder.User2Id;
            (await _settingManager.GetOrNullForCurrentUserAsync("MySetting2")).ShouldBe("user2-store-value");
        }

        [Fact]
        public async Task Should_Fallback_To_Default_Store_Value_When_No_Value_For_Current_User_With_GetOrNullForCurrentUserAsync()
        {
            _currentUserId = Guid.NewGuid();
            (await _settingManager.GetOrNullAsync("MySetting2")).ShouldBe("default-store-value");
        }

        [Fact]
        public async Task Should_Not_Fallback_To_Default_Store_Value_When_No_Value_For_Current_User_But_Specified_Fallback_As_False()
        {
            _currentUserId = Guid.NewGuid();
            (await _settingManager.GetOrNullForCurrentUserAsync("MySetting2", fallback: false)).ShouldBeNull();
        }

        [Fact]
        public async Task Should_Get_All_From_Store_For_Given_User()
        {
            var settingValues = await _settingManager.GetAllForUserAsync(AbpIdentityTestDataBuilder.User1Id);
            settingValues.ShouldContain(sv => sv.Name == "MySetting1" && sv.Value == "42");
            settingValues.ShouldContain(sv => sv.Name == "MySetting2" && sv.Value == "user1-store-value");
            settingValues.ShouldContain(sv => sv.Name == "SettingNotSetInStore" && sv.Value == "default-value");
        }

        [Fact]
        public async Task Should_Get_All_From_Store_For_Given_User_Without_Fallback()
        {
            var settingValues = await _settingManager.GetAllForUserAsync(AbpIdentityTestDataBuilder.User1Id, fallback: false);
            settingValues.Count.ShouldBe(1);
            settingValues.ShouldContain(sv => sv.Name == "MySetting2" && sv.Value == "user1-store-value");
        }

        [Fact]
        public async Task Should_Delete_Setting_Record_When_Set_To_Null()
        {
            await _settingManager.SetForUserAsync(AbpIdentityTestDataBuilder.User1Id, "MySetting2", null);

            UsingDbContext(context =>
            {
                context.Settings.Count(
                    s =>
                        s.EntityType == UserSettingValueProvider.DefaultEntityType &&
                        s.EntityId == AbpIdentityTestDataBuilder.User1Id.ToString() &&
                        s.Name == "MySetting2"
                ).ShouldBe(0);
            });
        }

        [Fact]
        public async Task Should_Change_User_Setting()
        {
            (await _settingManager.GetOrNullForUserAsync("MySetting2", AbpIdentityTestDataBuilder.User1Id))
                .ShouldBe("user1-store-value");

            await _settingManager.SetForUserAsync(AbpIdentityTestDataBuilder.User1Id, "MySetting2", "user1-new-store-value");

            (await _settingManager.GetOrNullForUserAsync("MySetting2", AbpIdentityTestDataBuilder.User1Id))
                .ShouldBe("user1-new-store-value");

            UsingDbContext(context =>
            {
                var setting = context.Settings.Single(
                    s =>
                        s.EntityType == UserSettingValueProvider.DefaultEntityType &&
                        s.EntityId == AbpIdentityTestDataBuilder.User1Id.ToString() &&
                        s.Name == "MySetting2"
                );

                setting.Value.ShouldBe("user1-new-store-value");
            });
        }
    }
}