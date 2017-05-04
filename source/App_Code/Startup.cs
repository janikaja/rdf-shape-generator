using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Bakalaura_darbs.Startup))]
namespace Bakalaura_darbs
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
