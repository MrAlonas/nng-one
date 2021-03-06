using System.Text;
using nng.Data;
using nng.VkFrameworks;
using nng_one.Configs;
using nng_one.Containers;
using nng_one.Exceptions;
using nng_one.Helpers;
using nng_one.Logging;
using Sentry;

namespace nng_one;

public static class CommandLineArguments
{
    public const string DebugMode = "debug";
}

public static class Program
{
    public static readonly Version Version = new(1, 0);
    public static bool DebugMode { get; private set; }
    private static bool SentryEnabled { get; set; }

    private static void CommandLineProcessor(IEnumerable<string> list)
    {
        var strings = list.Select(x => x.Replace("--", string.Empty));
        foreach (var commandLine in strings)
            if (commandLine.ToLower().Trim() == CommandLineArguments.DebugMode)
                DebugMode = true;
    }

    private static void Main(string[] args)
    {
        if (args is {Length: > 0}) CommandLineProcessor(args);
        var windows = OperatingSystem.IsWindows();

        var debug = DebugMode ? " debug" : string.Empty;
        Console.Title = $"nng one v{Version.Major}.{Version.Minor}{debug}";

        Console.InputEncoding = windows ? Encoding.Unicode : Encoding.UTF8;
        Console.OutputEncoding = windows ? Encoding.Unicode : Encoding.Default;

        Console.CancelKeyPress += delegate { Console.ResetColor(); };
        Console.ResetColor();

        using (SentrySdk.Init(o =>
               {
                   o.Dsn = "https://ca8c3bff58144b4ca6d677ee5c80b376@o555933.ingest.sentry.io/5905813";
                   o.Environment = "public";
               }))
        {
            while (true)
                try
                {
                    var exit = false;
                    while (!exit) exit = SetUp();
                    while (true) Startup.Start();
                }
                catch (Exception e)
                {
                    Logger.Log($"{e.GetType()}: {e.Message}", LogType.Error);
                    if (SentryEnabled) SentrySdk.CaptureException(e);
                    Logger.Idle();
                }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private static bool SetUp()
    {
        Console.Clear();

        Config config;

        try
        {
            config = ConfigProcessor.LoadConfig();
        }
        catch (ConfigNotFoundException)
        {
            config = new Config
            {
                BanReason = string.Empty,
                CaptchaBypass = false,
                DataUrl = string.Empty,
                Sentry = true,
                SwitchCallback = true,
                Token = string.Empty
            };
            ConfigProcessor.SaveConfig(config);
        }

        if (config.Token.Length < 1)
        {
            Logger.Clear();
            ConfigDialog.SetUpConfig();
            return false;
        }

        VkFramework framework;
        try
        {
            framework = new VkFramework(config.Token);
        }
        catch (Exception)
        {
            Logger.Log("Недействительный токен", LogType.Error);
            ConfigDialog.SetUpToken();
            return false;
        }

        VkFramework.OnCaptchaWait += delegate(object? _, CaptchaEventArgs time)
        {
            Logger.Log($"Каптча, ожидаем {time.SecondsToWait} секунд");
        };

        if (!config.CaptchaBypass) framework.SetCaptchaSolver(new CaptchaHandler());
        else framework.ResetCaptchaSolver();

        VkFrameworkContainer.GetInstance().SetFramework(framework);

        var currentUser = framework.CurrentUser;
        var logContainer = LogContainer.GetInstance();
        logContainer.Messages.Clear();

        logContainer.Messages
            .Add(new Message($"Добро пожаловать, {currentUser.FirstName} | Ваш ID: {currentUser.Id}",
                LogType.InfoVersionShow));

        if (UpdateHelper.IfUpdateNeed(out var version))
            logContainer.Messages.Add(new Message(
                $"Версия v{Version.Major}.{Version.Minor} устарела, пожалуйста, обновитесь до {version}",
                LogType.Debug, forceSend: true));

        SentryEnabled = config.Sentry;

        DataContainer.GetInstance().SetModel(DataHelper.GetData(config.DataUrl));
        return true;
    }
}
