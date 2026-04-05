using MetroMania.Engine;
using MetroMania.Engine.Model;
using MetroMania.PlayerTemplate;
using MetroMania.PlayerTemplate.Levels;
using Spectre.Console;

// ── Banner ────────────────────────────────────────────────────────────────────
AnsiConsole.Write(
    new FigletText("MetroMania")
        .Centered()
        .Color(Color.DodgerBlue1));

AnsiConsole.Write(new Rule("[grey]Player Template[/]").RuleStyle("grey"));
AnsiConsole.WriteLine();

// ── Level info ────────────────────────────────────────────────────────────────
var level = Level1.Level;

var infoTable = new Table()
    .NoBorder()
    .HideHeaders()
    .AddColumn(new TableColumn("").RightAligned().PadRight(2))
    .AddColumn(new TableColumn(""));

infoTable.AddRow("[grey]Level[/]",    $"[bold white]{Markup.Escape(level.Title)}[/]");
infoTable.AddRow("[grey]Grid[/]",     $"[white]{level.GridWidth} × {level.GridHeight}[/]");
infoTable.AddRow("[grey]Seed[/]",     $"[white]{level.LevelData.Seed}[/]");
infoTable.AddRow("[grey]Max days[/]", $"[white]{level.LevelData.MaxDays}[/]");
infoTable.AddRow("[grey]Stations[/]", $"[white]{level.LevelData.Stations.Count}[/]");

AnsiConsole.Write(
    new Panel(infoTable)
        .Header("[yellow] Level Info [/]")
        .BorderColor(Color.Grey23)
        .Padding(1, 0));

AnsiConsole.WriteLine();

// ── Run simulation ────────────────────────────────────────────────────────────
GameResult result = null!;

AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots2)
    .SpinnerStyle(Style.Parse("dodgerblue1 bold"))
    .Start("[dim]Simulating...[/]", _ =>
    {
        var engine = new MetroManiaEngine();
        var runner = new MyMetroManiaRunner();
        result = engine.Run(runner, level: level, level.LevelData.MaxDays * 24);
    });

// ── Results ───────────────────────────────────────────────────────────────────
AnsiConsole.WriteLine();

var survived = result.DaysSurvived >= level.LevelData.MaxDays;
var outcomeColor  = survived ? Color.Green  : Color.Red;
var outcomeLabel  = survived ? "[green bold]✓  Completed[/]" : "[red bold]✗  Game Over[/]";
var daysMarkup    = survived
    ? $"[green]{result.DaysSurvived}[/] / {level.LevelData.MaxDays}"
    : $"[red]{result.DaysSurvived}[/] / {level.LevelData.MaxDays}";

var resultsTable = new Table()
    .Border(TableBorder.None)
    .HideHeaders()
    .AddColumn(new TableColumn("").RightAligned().PadRight(2))
    .AddColumn(new TableColumn(""));

resultsTable.AddRow("[grey]Score[/]",             $"[bold yellow]{result.TotalScore:N0}[/]");
resultsTable.AddRow("[grey]Days survived[/]",     daysMarkup);
resultsTable.AddRow("[grey]Passengers spawned[/]",$"[white]{result.TotalPassengersSpawned:N0}[/]");
resultsTable.AddRow("[grey]Player actions[/]",    $"[white]{result.NumberOfPlayerActions:N0}[/]");
resultsTable.AddRow("[grey]Processing time[/]",   $"[white]{result.ProcessingTime.TotalMilliseconds:F1} ms[/]");

AnsiConsole.Write(
    new Panel(resultsTable)
        .Header($"[bold] {outcomeLabel} [/]")
        .BorderColor(outcomeColor)
        .Padding(1, 0));

AnsiConsole.WriteLine();