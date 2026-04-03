Feature: Station Spawning
    Metro stations appear on the map based on their SpawnDelayDays configuration.
    A delay of N means the station spawns at hour 0 of day N+1 (spawnDay = N+1).
    OnStationSpawned fires exactly once per station at that moment and the station
    is added to the snapshot's Stations dictionary for all subsequent ticks.

    Scenario: A station with delay 0 spawns on the first tick
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        When the simulation runs for 1 hour
        Then the Circle station at (0,0) should have spawned

    Scenario: A station with delay 0 spawns exactly once across many ticks
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        When the simulation runs for 48 hours
        Then "OnStationSpawned" should have fired exactly 1 time

    Scenario: A station with delay 1 does not spawn within the first 24 ticks
        Given a level with a Circle station at (0,0) with a spawn delay of 1 day
        When the simulation runs for 24 hours
        Then the Circle station at (0,0) should not have spawned

    Scenario: A station with delay 1 spawns at the start of day 2
        Given a level with a Circle station at (0,0) with a spawn delay of 1 day
        When the simulation runs for 25 hours
        Then the Circle station at (0,0) should have spawned

    Scenario: A station with delay 1 spawns exactly once over 72 hours
        Given a level with a Circle station at (0,0) with a spawn delay of 1 day
        When the simulation runs for 72 hours
        Then "OnStationSpawned" should have fired exactly 1 time

    Scenario: A station with delay 2 does not spawn within the first 48 ticks
        Given a level with a Triangle station at (2,3) with a spawn delay of 2 days
        When the simulation runs for 48 hours
        Then the Triangle station at (2,3) should not have spawned

    Scenario: A station with delay 2 spawns at the start of day 3
        Given a level with a Triangle station at (2,3) with a spawn delay of 2 days
        When the simulation runs for 49 hours
        Then the Triangle station at (2,3) should have spawned

    Scenario: A station with delay 5 does not spawn within the first 120 ticks
        Given a level with a Diamond station at (5,5) with a spawn delay of 5 days
        When the simulation runs for 120 hours
        Then the Diamond station at (5,5) should not have spawned

    Scenario: A station with delay 5 spawns at the start of day 6
        Given a level with a Diamond station at (5,5) with a spawn delay of 5 days
        When the simulation runs for 121 hours
        Then the Diamond station at (5,5) should have spawned

    Scenario Outline: Stations spawn at the correct tick based on their delay
        Given a level with a <type> station at (<x>,<y>) with a spawn delay of <delay> days
        When the simulation runs for <hours> hours
        Then the <type> station at (<x>,<y>) <expectation>

        Examples: Last tick before spawn day — station not yet present
            | type      | x | y | delay | hours | expectation             |
            | Rectangle | 1 | 0 | 1     | 24    | should not have spawned |
            | Triangle  | 2 | 0 | 2     | 48    | should not have spawned |
            | Diamond   | 3 | 0 | 3     | 72    | should not have spawned |
            | Pentagon  | 4 | 0 | 4     | 96    | should not have spawned |
            | Star      | 5 | 0 | 5     | 120   | should not have spawned |

        Examples: First tick of spawn day — station appears
            | type      | x | y | delay | hours | expectation             |
            | Circle    | 0 | 0 | 0     | 1     | should have spawned     |
            | Rectangle | 1 | 0 | 1     | 25    | should have spawned     |
            | Triangle  | 2 | 0 | 2     | 49    | should have spawned     |
            | Diamond   | 3 | 0 | 3     | 73    | should have spawned     |
            | Pentagon  | 4 | 0 | 4     | 97    | should have spawned     |
            | Star      | 5 | 0 | 5     | 121   | should have spawned     |

    Scenario: A spawned station is present in the game snapshot
        Given a level with a Diamond station at (3,4) with a spawn delay of 0 days
        When the simulation runs for 1 hour
        Then the game snapshot should contain a Diamond station at (3,4)

    Scenario: A station that has not yet spawned is absent from the game snapshot
        Given a level with a Diamond station at (3,4) with a spawn delay of 3 days
        When the simulation runs for 24 hours
        Then the game snapshot should contain no stations

    Scenario: All spawned stations are assigned unique IDs
        Given a level with the following stations:
            | X | Y | Type      | SpawnDelay |
            | 0 | 0 | Circle    | 0          |
            | 1 | 0 | Triangle  | 0          |
            | 2 | 0 | Diamond   | 0          |
            | 3 | 0 | Rectangle | 0          |
            | 4 | 0 | Pentagon  | 0          |
        When the simulation runs for 1 hour
        Then all spawned stations should have unique IDs
        And "OnStationSpawned" should have fired exactly 5 times

    Scenario: Only the immediate station has spawned when a later station has not yet reached its day
        Given a level with the following stations:
            | X | Y | Type     | SpawnDelay |
            | 0 | 0 | Circle   | 0          |
            | 1 | 0 | Triangle | 2          |
        When the simulation runs for 1 hour
        Then the Circle station at (0,0) should have spawned
        And the Triangle station at (1,0) should not have spawned

    Scenario: Both stations have spawned once enough days have passed
        Given a level with the following stations:
            | X | Y | Type     | SpawnDelay |
            | 0 | 0 | Circle   | 0          |
            | 1 | 0 | Triangle | 2          |
        When the simulation runs for 49 hours
        Then the Circle station at (0,0) should have spawned
        And the Triangle station at (1,0) should have spawned
        And "OnStationSpawned" should have fired exactly 2 times

    Scenario: A station does not re-spawn on subsequent days
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        When the simulation runs for 168 hours
        Then "OnStationSpawned" should have fired exactly 1 time

    Scenario: All six station types can be spawned
        Given a level with the following stations:
            | X | Y | Type      | SpawnDelay |
            | 0 | 0 | Circle    | 0          |
            | 1 | 0 | Rectangle | 0          |
            | 2 | 0 | Triangle  | 0          |
            | 3 | 0 | Diamond   | 0          |
            | 4 | 0 | Pentagon  | 0          |
            | 5 | 0 | Star      | 0          |
        When the simulation runs for 1 hour
        Then "OnStationSpawned" should have fired exactly 6 times
        And the Circle station at (0,0) should have spawned
        And the Rectangle station at (1,0) should have spawned
        And the Triangle station at (2,0) should have spawned
        And the Diamond station at (3,0) should have spawned
        And the Pentagon station at (4,0) should have spawned
        And the Star station at (5,0) should have spawned

    Scenario: A station at a non-zero grid location spawns correctly
        Given a level with a Star station at (7,3) with a spawn delay of 0 days
        When the simulation runs for 1 hour
        Then the Star station at (7,3) should have spawned
        And the game snapshot should contain a Star station at (7,3)

    Scenario: Three stations with staggered delays all spawn in the correct order
        Given a level with the following stations:
            | X | Y | Type      | SpawnDelay |
            | 0 | 0 | Circle    | 0          |
            | 1 | 0 | Triangle  | 1          |
            | 2 | 0 | Pentagon  | 3          |
        When the simulation runs for 73 hours
        Then the Circle station at (0,0) should have spawned
        And the Triangle station at (1,0) should have spawned
        And the Pentagon station at (2,0) should have spawned
        And "OnStationSpawned" should have fired exactly 3 times

    Scenario: Third station has not yet spawned when only first two days have passed
        Given a level with the following stations:
            | X | Y | Type      | SpawnDelay |
            | 0 | 0 | Circle    | 0          |
            | 1 | 0 | Triangle  | 1          |
            | 2 | 0 | Pentagon  | 3          |
        When the simulation runs for 48 hours
        Then the Circle station at (0,0) should have spawned
        And the Triangle station at (1,0) should have spawned
        And the Pentagon station at (2,0) should not have spawned
        And "OnStationSpawned" should have fired exactly 2 times
