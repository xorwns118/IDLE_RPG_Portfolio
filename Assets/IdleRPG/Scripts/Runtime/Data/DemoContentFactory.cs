using IdleRPG.Domain.Data;
using IdleRPG.Runtime.Configuration;

namespace IdleRPG.Runtime.Data
{
    public static class DemoContentFactory
    {
        public static RuntimeContentDatabase CreateWeek1Database()
        {
            return MvpGameContentSettings.CreateDefault().CreateDatabase();
        }
    }
}
