using nng.VkFrameworks;
using nng_one.Containers;
using nng_one.FunctionParameters;
using nng_one.Logging;

namespace nng_one.Functions;

public static class Search
{
    private static readonly VkFramework VkFramework = VkFrameworkContainer.GetInstance().VkFramework;

    public static void Process(SearchParameters searchParameters)
    {
        foreach (var group in searchParameters.Groups) SearchGroup(group.Id, searchParameters.Users.Select(x => x.Id));
    }

    private static void SearchGroup(long group, IEnumerable<long> users)
    {
        var data = VkFramework.GetGroupData(group);
        var workUsers = data.Managers.Where(x => users.Any(y => x.Id == y)).ToList();
        if (!workUsers.Any())
        {
            Logger.Log($"В сообществе {group} редакторов найдено не было");
            return;
        }

        foreach (var workUser in workUsers) Logger.Log($"Пользоветель {workUser.Id} найден в сообществе {group}");
    }
}
