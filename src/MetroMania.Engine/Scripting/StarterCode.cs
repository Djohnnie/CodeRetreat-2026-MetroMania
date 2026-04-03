namespace MetroMania.Engine.Scripting;

public static class StarterCode
{
    public const string Template = """
        public class MyMetroManiaRunner : IMetroManiaRunner
        {
            public PlayerAction OnHourTick(GameSnapshot snapshot) => PlayerAction.None;

            public void OnDayStart(GameSnapshot snapshot) { }

            public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }

            public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType) { }

            public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }

            public void OnStationOverrun(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }

            public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }
        }
        """;
}