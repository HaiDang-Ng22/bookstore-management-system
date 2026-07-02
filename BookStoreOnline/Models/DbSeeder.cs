using BookStoreOnline.Core;

namespace BookStoreOnline.Models
{
    public static class DbSeeder
    {
        public static void Seed()
        {
            using (var db = new NhaSachEntities3())
            {
                var roleService = new RoleService(db);
                roleService.EnsureRoleTableExists();
            }
        }
    }
}