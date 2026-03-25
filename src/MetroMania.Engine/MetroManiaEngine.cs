using System.Diagnostics;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Engine;

public class MetroManiaEngine
{
    public GameResult Run(IMetroManiaRunner runner, Level level)
    {
        var stopwatch = Stopwatch.StartNew();
        var random = new Random(42);
        var activeStations = new Dictionary<Location, StationState>();
        var stationTypes = Enum.GetValues<StationType>();

        int totalPassengersSpawned = 0;
        int score = 0;
        int day = 0;
        bool gameOver = false;

        while (!gameOver)
        {
            day++;
            var dayOfWeek = (DayOfWeek)(day % 7);

            runner.OnDayStart(day, dayOfWeek);

            // Every Monday at 0h, gift a random resource
            if (dayOfWeek == DayOfWeek.Monday)
            {
                var gift = (ResourceType)random.Next(3);
                runner.OnWeeklyGift(gift);
            }

            // Spawn stations scheduled for this day
            foreach (var spawn in level.LevelData.Stations)
            {
                var location = new Location(spawn.GridX, spawn.GridY);
                if (spawn.SpawnDelayDays == day && !activeStations.ContainsKey(location))
                {
                    activeStations[location] = new StationState(spawn.StationType);
                    runner.OnStationSpawned(location, spawn.StationType);
                }
            }

            // Hour loop
            for (int hour = 0; hour < 24; hour++)
            {
                // Spawn a passenger at a random active station (~33% chance each hour)
                if (activeStations.Count > 0 && random.Next(3) == 0)
                {
                    var stations = activeStations.ToList();
                    var (location, state) = stations[random.Next(stations.Count)];

                    // Passenger wants a station type different from the one they're at
                    StationType destType;
                    do { destType = stationTypes[random.Next(stationTypes.Length)]; }
                    while (destType == state.Type);

                    state.Passengers.Add(new Passenger(destType));
                    totalPassengersSpawned++;

                    runner.OnPassengerWaiting(location, state.Passengers.AsReadOnly());
                }

                // Let the player act
                runner.OnHourTick(day, hour);
                score++;

                // Check station passenger counts
                foreach (var (location, state) in activeStations)
                {
                    if (state.Passengers.Count >= 20)
                    {
                        runner.OnGameOver(location, state.Passengers.AsReadOnly());
                        gameOver = true;
                        break;
                    }

                    if (state.Passengers.Count >= 10)
                    {
                        runner.OnStationOverrun(location, state.Passengers.AsReadOnly());
                    }
                }

                if (gameOver) break;
            }
        }

        stopwatch.Stop();

        return new GameResult
        {
            Score = score,
            TimeTaken = stopwatch.Elapsed,
            DaysSurvived = day,
            TotalPassengersSpawned = totalPassengersSpawned
        };
    }

    private class StationState(StationType type)
    {
        public StationType Type { get; } = type;
        public List<Passenger> Passengers { get; } = [];
    }
}