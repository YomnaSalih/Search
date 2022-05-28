using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Search.Startup))]
namespace Search
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
