using MetroMania.Domain.Entities;
using MetroMania.Engine.Model;
using Spectre.Console;

namespace MetroMania.PlayerTemplate;

internal static class SimulationPrinter
{
    public static void PrintBanner()
    {
        AnsiConsole.Write(
            new FigletText("MetroMania")
                .Centered()
                .Color(Color.DodgerBlue1));

        AnsiConsole.Write(new Rule("[grey]Player Template[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    public static void PrintLevelInfo(Level level)
    {
        var table = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("").RightAligned().PadRight(2))
            .AddColumn(new TableColumn(""));

        table.AddRow("[grey]Level[/]",    $"[bold white]{Markup.Escape(level.Title)}[/]");
        table.AddRow("[grey]Grid[/]",     $"[white]{level.GridWidth} × {level.GridHeight}[/]");
        table.AddRow("[grey]Seed[/]",     $"[white]{level.LevelData.Seed}[/]");
        table.AddRow("[grey]Max days[/]", $"[white]{level.LevelData.MaxDays}[/]");
        table.AddRow("[grey]Stations[/]", $"[white]{level.LevelData.Stations.Count}[/]");

        AnsiConsole.Write(
            new Panel(table)
                .Header("[yellow] Level Info [/]")
                .BorderColor(Color.Grey23)
                .Padding(1, 0));

        AnsiConsole.WriteLine();
    }

    public static void PrintResults(GameResult result, Level level)
    {
        AnsiConsole.WriteLine();

        var survived = result.DaysSurvived >= level.LevelData.MaxDays;
        var outcomeColor = survived ? Color.Green : Color.Red;
        var outcomeLabel = survived ? "[green bold]✓  Completed[/]" : "[red bold]✗  Game Over[/]";
        var daysMarkup = survived
            ? $"[green]{result.DaysSurvived}[/] / {level.LevelData.MaxDays}"
            : $"[red]{result.DaysSurvived}[/] / {level.LevelData.MaxDays}";

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").RightAligned().PadRight(2))
            .AddColumn(new TableColumn(""));

        table.AddRow("[grey]Score[/]",              $"[bold yellow]{result.TotalScore:N0}[/]");
        table.AddRow("[grey]Days survived[/]",      daysMarkup);
        table.AddRow("[grey]Passengers spawned[/]", $"[white]{result.TotalPassengersSpawned:N0}[/]");
        table.AddRow("[grey]Player actions[/]",     $"[white]{result.NumberOfPlayerActions:N0}[/]");
        table.AddRow("[grey]Processing time[/]",    $"[white]{result.ProcessingTime.TotalMilliseconds:F1} ms[/]");

        AnsiConsole.Write(
            new Panel(table)
                .Header($"[bold] {outcomeLabel} [/]")
                .BorderColor(outcomeColor)
                .Padding(1, 0));

        AnsiConsole.WriteLine();
    }

    public static GameResult RunWithSpinner(Level level, MyMetroManiaRunner runner)
    {
        GameResult result = null!;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("dodgerblue1 bold"))
            .Start("[dim]Simulating...[/]", _ =>
            {
                var engine = new MetroMania.Engine.MetroManiaEngine();
                result = engine.Run(runner, level: level, level.LevelData.MaxDays * 24);
            });

        return result;
    }
}
