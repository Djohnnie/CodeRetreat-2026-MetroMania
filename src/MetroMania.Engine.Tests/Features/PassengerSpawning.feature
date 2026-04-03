Feature: Passenger Spawning
    Passengers spawn at metro stations based on each station's PassengerSpawnPhases.
    The active phase is the one with the highest AfterDays threshold the station has reached.
    hoursAlive counts from the moment the station appeared on the map. A passenger spawns
    when hoursAlive % FrequencyInHours == 0 (including at hoursAlive=0, the tick of spawn).
    Passengers' destination types are drawn from all station types except the origin type.

    # Exact spawn counts used in this file:
    #   delay=0, freq=24h: spawns at tick 0,24,48,72  →  N=73  gives 4 spawns
    #   delay=0, freq=12h: spawns at tick 0,12,24,36  →  N=37  gives 4 spawns
    #   delay=0, freq=1h:  spawns every tick           →  N=5   gives 5 spawns
    #   delay=2, freq=24h: spawns at tick 48,72        →  N=73  gives 2 spawns
    #   delay=1, freq=1h:  no spawn before tick 24     →  N=24  gives 0 spawns

    Scenario: Station without spawn phases never spawns passengers
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 100 hours
        Then "OnPassengerSpawned" should have fired 0 times

    Scenario: Station with freq=24h spawns at the correct intervals over 73 ticks
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 73 hours
        Then "OnPassengerSpawned" should have fired exactly 4 times

    Scenario: Station with freq=12h spawns at the correct intervals over 37 ticks
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 12 hours
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 37 hours
        Then "OnPassengerSpawned" should have fired exactly 4 times

    Scenario: Station with freq=1h spawns a passenger every single tick
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 5 hours
        Then "OnPassengerSpawned" should have fired exactly 5 times

    Scenario: Passengers also spawn on the very tick the station appears (hoursAlive=0)
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 1 hour
        Then "OnPassengerSpawned" should have fired exactly 1 time

    Scenario: Spawn delay shifts all passenger events relative to absolute tick count
        Given a level with a Circle station at (0,0) with a spawn delay of 2 days and passengers every 24 hours
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 73 hours
        Then "OnPassengerSpawned" should have fired exactly 2 times

    Scenario: No passengers spawn before a delayed station appears on the map
        Given a level with a Circle station at (0,0) with a spawn delay of 1 day and passengers every 1 hour
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 24 hours
        Then "OnPassengerSpawned" should have fired 0 times

    Scenario: Passenger spawning begins at the exact tick the delayed station appears
        Given a level with a Circle station at (0,0) with a spawn delay of 1 day and passengers every 24 hours
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 25 hours
        Then "OnPassengerSpawned" should have fired exactly 1 time

    Scenario: Phase transition doubles spawn frequency after its day threshold is reached
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 0         | 24               |
            | 2         | 12               |
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 61 hours
        Then "OnPassengerSpawned" should have fired exactly 4 times

    Scenario: Three phases each increase frequency and activate at their respective thresholds
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 0         | 24               |
            | 2         | 12               |
            | 4         | 6                |
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 127 hours
        Then "OnPassengerSpawned" should have fired exactly 12 times

    Scenario: Phase with zero frequency suppresses passenger spawning
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 0         | 0                |
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 48 hours
        Then "OnPassengerSpawned" should have fired 0 times

    Scenario: Phase becomes active only after its AfterDays threshold is reached
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 3         | 24               |
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 72 hours
        Then "OnPassengerSpawned" should have fired 0 times

    Scenario: Phase activates exactly when its AfterDays is reached
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 3         | 24               |
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 73 hours
        Then "OnPassengerSpawned" should have fired exactly 1 time

    Scenario: Passenger destination type is never the same as the origin station type
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 10 hours
        Then all spawned passengers should have a destination type different from Circle

    Scenario: OnPassengerSpawned is called with the correct station ID
        Given a level with a Circle station at (2,3) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 1 hour
        Then all passenger spawn events should reference the station at (2,3)

    Scenario: Multiple stations both spawn passengers independently
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days and passengers every 24 hours
        When the simulation runs for 25 hours
        Then "OnPassengerSpawned" should have fired exactly 4 times

    Scenario: Passengers accumulate in the snapshot over time
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 5 hours
        Then the last snapshot should contain 5 passengers

    Scenario: Each passenger is assigned a unique ID
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 5 hours
        Then all spawned passengers should have unique IDs

    Scenario: Two stations with staggered delays both accumulate passengers correctly
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 1 day and passengers every 24 hours
        When the simulation runs for 49 hours
        Then "OnPassengerSpawned" should have fired exactly 5 times

    Scenario: Station with delayed spawn and multiple phases counts hoursAlive from its spawn tick
        Given a level with a Circle station at (0,0) with a spawn delay of 1 day and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 0         | 24               |
            | 2         | 12               |
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 85 hours
        Then "OnPassengerSpawned" should have fired exactly 4 times
