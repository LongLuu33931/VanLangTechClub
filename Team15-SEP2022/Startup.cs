using Hangfire;
using Microsoft.Owin;
using Owin;
using Team15_SEP2022.Models;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;

[assembly: OwinStartupAttribute(typeof(Team15_SEP2022.Startup))]
namespace Team15_SEP2022
{
    public partial class Startup
    {
        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            GlobalConfiguration.Configuration.UseSqlServerStorage("DefaultConnection");
            app.UseHangfireDashboard("/myJobDashboard");
            app.UseHangfireServer();
        }
    }
}
