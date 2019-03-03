using VkNet.Model.Keyboard;

namespace VkJustTest
{
    public class User
    {
        public long UserId { get; set; }

        public bool RequestToJoinGroptSent { get; set; } = false;

        public MessageKeyboard LastKeyboard { get; set; } = new KeyboardBuilder().Clear().Build();

        public string LastQuestion { get; set; }
    }
}
