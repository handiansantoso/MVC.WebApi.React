using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AdCenter.Startup))]
namespace AdCenter
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
