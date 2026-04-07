using MetroMania.PlayerTemplate;
using MetroMania.PlayerTemplate.Levels;

SimulationPrinter.PrintBanner();

var level  = Level1.Level;
var runner = new MyMetroManiaRunner();

SimulationPrinter.PrintLevelInfo(level);

var result = SimulationPrinter.RunWithSpinner(level, runner);

SimulationPrinter.PrintResults(result, level);