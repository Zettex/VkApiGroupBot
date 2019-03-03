using System.Collections.Generic;
using System.Linq;

namespace VkJustTest
{
    public class UsersState
    {
        public IList<User> Users { get; private set; } = new List<User>();

        public User GetUser(long id)
        {
            return Users.Where(u => u.UserId == id).FirstOrDefault();
        }

        public void AddUser(long id)
        {
            Users.Add(new User() { UserId = id });
        }

        public void RemoveUser(long id)
        {
            var user = Users.Where(u => u.UserId == id).FirstOrDefault();
            Users.Remove(user);
        }
    }
}
