﻿using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrockAllen.IdentityReboot.Tests
{
    public class IdentityRebootTestBase
    {
        protected TestIdentityRebootUserManager manager;
        protected TestUserStore store;
        protected TestUser user;

        protected const string username = "test";
        protected const string password = "pass123";

        [TestInitialize]
        public virtual void Init()
        {
            store = new TestUserStore();
            manager = new TestIdentityRebootUserManager(store, 100);

            user = new TestUser()
            {
                UserName = username
            };
            var result = manager.Create(user, password);
            Assert.IsTrue(result.Succeeded);
        }
    }
}
