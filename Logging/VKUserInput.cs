using nng.VkFrameworks;
using nng_one.Containers;
using VkNet.Model;

namespace nng_one.Logging;

public static class VkUserInput
{
    private static readonly VkFramework VkFramework = VkFrameworkContainer.GetInstance().VkFramework;
    private static readonly InputHandler InputHandler = InputHandler.GetInstance();

    public static IEnumerable<User> GetUserInput()
    {
        var input = InputHandler.GetStringInput("Введите ID пользователя");
        try
        {
            var users = VkFramework.GetUsers(input.Split(",")).ToList();
            if (!users.Any()) throw new Exception("Пользователь не найден");
            return users;
        }
        catch (Exception e)
        {
            Logger.Log("Не удалось получить пользователя", LogType.Error);
            Logger.Log(e);
            return GetUserInput();
        }
    }

    public static IEnumerable<Group> GetGroupInput()
    {
        var input = InputHandler.GetStringInput("Введите ID сообщества");
        try
        {
            var user = VkFramework.GetGroups(input.Split(","));
            return user;
        }
        catch (Exception e)
        {
            Logger.Log("Не удалось получить пользователя", LogType.Error);
            Logger.Log(e);
            return GetGroupInput();
        }
    }

    public static string GetPostInput()
    {
        var input = InputHandler.GetStringInput("Введите ID поста (wall-000000000_0000)");
        while (input.Length < 6 || !input.Contains("wall"))
        {
            Logger.Log("Неправильный ID поста", LogType.Error);
            input = GetPostInput();
        }

        return input;
    }
}
