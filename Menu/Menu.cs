using nng.Models;
using nng_one.Configs;
using nng_one.Containers;
using nng_one.FunctionParameters;
using nng_one.Interfaces;
using nng_one.Logging;
using VkNet.Model;

namespace nng_one.Menu;

public class Menu
{
    private readonly Config _config = ConfigProcessor.LoadConfig();
    private readonly DataModel _data = DataContainer.GetInstance().Model;
    private readonly InputHandler _inputHandler = InputHandler.GetInstance();

    public IFunctionParameter GetResult()
    {
        Logger.Clear(true);
        return _inputHandler.GetMainMenuInput() switch
        {
            MainMenuItem.Block => Block(),
            MainMenuItem.Unblock => Unblock(),
            MainMenuItem.Editors => Editors(),
            MainMenuItem.Search => Search(),
            MainMenuItem.Misc => Misc(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private IFunctionParameter Block()
    {
        if (_inputHandler.GetBoolInput("Начать блокировку пользователей в сообществах?"))
            return new BlockParameters(_data.Users.Where(x => !x.Deleted.HasValue).Select(x => x.Id), _data.GroupList,
                _config);
        Logger.Clear();
        return GetResult();
    }

    private IFunctionParameter Unblock()
    {
        var userChoice = _inputHandler.GetMenuInput(new[] {"Пользователя", "Пользователей"}, out var returnBack);
        if (returnBack)
        {
            Logger.Clear();
            return GetResult();
        }

        var users = userChoice == 0
            ? VkUserInput.GetUserInput().ToList()
            : null;

        var groupChoice =
            _inputHandler.GetMenuInput(new[] {"В сообществе", "В сообществах"}, out var groupReturnBack);
        if (groupReturnBack)
        {
            Logger.Clear();
            return Unblock();
        }

        var groups = groupChoice == 0
            ? VkUserInput.GetGroupInput().ToList()
            : _data.GroupList.Select(x => new Group {Id = x}).ToList();

        return new UnblockParameters(_config, groups, users);
    }

    private IFunctionParameter Editors()
    {
        var giveChoice = _inputHandler.GetMenuInput(new[] {"Выдача", "Снятие"}, out var returnBack) == 0
            ? EditorOperationType.Give
            : EditorOperationType.Fire;
        if (returnBack)
        {
            Logger.Clear();
            return GetResult();
        }

        List<User>? users = null;

        var userChoice = _inputHandler.GetMenuInput(new[] {"Пользователю", "Пользователям"}, out var userReturnBack);
        if (userReturnBack)
        {
            Logger.Clear();
            return Editors();
        }

        if (userChoice == 0)
        {
            users = VkUserInput.GetUserInput().ToList();
            return new EditorParameters(giveChoice, _config, users, _data.GroupList.Select(x => new Group {Id = x}));
        }

        var groupChoice =
            _inputHandler.GetMenuInput(new[] {"В сообществе", "В сообществах"}, out var groupReturnBack);
        if (groupReturnBack)
        {
            Logger.Clear();
            return Editors();
        }

        if (groupChoice == 1)
        {
            var phrase = giveChoice == EditorOperationType.Give ? "выдать" : "снять";
            if (!_inputHandler.GetBoolInput($"Вы уверены, что хотите {phrase} редактора пользователям в сообществах?"))
                return Editors();
        }

        var groupList = groupChoice == 0
            ? VkUserInput.GetGroupInput().ToList()
            : _data.GroupList.Select(x => new Group {Id = x}).ToList();

        return new EditorParameters(giveChoice, _config, users, groupList);
    }

    private IFunctionParameter Search()
    {
        var funcChoose = _inputHandler.GetMenuInput(new[]
        {
            "Редактора",
            "Несостыковок"
        }, out var returnBack);

        if (returnBack)
        {
            Logger.Clear();
            return GetResult();
        }

        switch (funcChoose)
        {
            case 0:
                var user = VkUserInput.GetUserInput();
                var groups = _data.GroupList.Select(x => new Group {Id = x});
                return new SearchParameters(user, groups, _config);
            case 1:
                if (!_inputHandler.GetBoolInput("Вы уверены, что хотите посмотреть несостыковки?"))
                    return Search();
                return new BanCompareParameters(_data.GroupList.ToList(), _data.Users
                    .Where(x => !x.Deleted.HasValue).Select(x => x.Id), _config);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IFunctionParameter Misc()
    {
        var funcChoice = _inputHandler.GetMenuInput(new[]
        {
            "Репост записи",
            "Удаление всех записей со стены", "Статистика", "Снятие собачек"
        }, out var returnBack);
        if (returnBack)
        {
            Logger.Clear();
            return GetResult();
        }

        switch (funcChoice)
        {
            case 0:
                var post = VkUserInput.GetPostInput();
                return new GroupWallParameters(_config, GroupWallParametersType.Repost,
                    _data.GroupList.Select(x => new Group {Id = x}), post);
            case 1:
                if (!_inputHandler.GetBoolInput("Вы уверены, что хотите начать сравнение несостыковок?"))
                    return Misc();
                return new GroupWallParameters(_config, GroupWallParametersType.DeleteAllPosts,
                    _data.GroupList.Select(x => new Group {Id = x}), null);
            case 2:
                return new MiscParameters(_config, MiscFunctionType.Stats, null);
            case 3:
                return new MiscParameters(_config, MiscFunctionType.RemoveBanned,
                    _data.GroupList.Select(x => new Group {Id = x}));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
