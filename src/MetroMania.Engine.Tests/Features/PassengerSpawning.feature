Feature: Passenger Spawning
    Passengers spawn at metro stations based on each station's PassengerSpawnPhases.
    Each phase defines a day threshold (AfterDays) and a spawn frequency (FrequencyInHours).
    The active phase is the one with the highest AfterDays that the station has reached.
    Stations without spawn phases never spawn passengers. When no passengers can spawn
    and no game-over occurs, the simulation runs indefinitely and must be cancelled
    via a CancellationToken.

    Scenario: Single station with one spawn phase spawns passengers at the correct frequency
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 12 hours
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs for 49 hours
        Then "OnPassengerWaiting" should have fired exactly 4 times
        And the passenger waiting events should be:
            | Day | Hour | X | Y | PassengerCount |
            | 1   | 12   | 0 | 0 | 1              |
            | 2   | 0    | 0 | 0 | 2              |
            | 2   | 12   | 0 | 0 | 3              |
            | 3   | 0    | 0 | 0 | 4              |

    Scenario: Station without spawn phases never spawns passengers and simulation is cancelled
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
        And the simulation will be cancelled after 200 hours
        When the simulation runs until game over or cancellation
        Then the simulation should have been cancelled
        And "OnPassengerWaiting" should have fired exactly 0 times

    Scenario: Station with multiple spawn phases transitions to faster frequency
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 0         | 24               |
            | 2         | 12               |
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs for 73 hours
        Then "OnPassengerWaiting" should have fired exactly 4 times
        And the passenger waiting events should be:
            | Day | Hour | X | Y | PassengerCount |
            | 2   | 0    | 0 | 0 | 1              |
            | 3   | 0    | 0 | 0 | 2              |
            | 3   | 12   | 0 | 0 | 3              |
            | 4   | 0    | 0 | 0 | 4              |

    Scenario: Multiple stations with different spawn phases fire events in correct order
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and the following spawn phases:
            | AfterDays | FrequencyInHours |
            | 0         | 24               |
            | 3         | 12               |
        And a level with a Triangle station at (1,0) with a spawn delay of 1 day and passengers every 24 hours
        When the simulation runs for 97 hours
        Then "OnPassengerWaiting" should have fired exactly 8 times
        And the passenger waiting events should be:
            | Day | Hour | X | Y | PassengerCount |
            | 2   | 0    | 0 | 0 | 1              |
            | 3   | 0    | 0 | 0 | 2              |
            | 3   | 0    | 1 | 0 | 1              |
            | 4   | 0    | 0 | 0 | 3              |
            | 4   | 0    | 1 | 0 | 2              |
            | 4   | 12   | 0 | 0 | 4              |
            | 5   | 0    | 0 | 0 | 5              |
            | 5   | 0    | 1 | 0 | 3              |

    Scenario: Passenger destination type always differs from origin station type
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs for 11 hours
        Then all passengers should have a destination type different from their origin station type

    Scenario: Station with spawn delay and immediate phase spawns passengers after delay
        Given a level with a Circle station at (0,0) with a spawn delay of 2 days and passengers every 24 hours
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs for 97 hours
        Then "OnPassengerWaiting" should have fired exactly 2 times
        And the passenger waiting events should be:
            | Day | Hour | X | Y | PassengerCount |
            | 4   | 0    | 0 | 0 | 1              |
            | 5   | 0    | 0 | 0 | 2              |

    Scenario: Station does not spawn passengers when no other station types exist
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And the simulation will be cancelled after 50 hours
        When the simulation runs until game over or cancellation
        Then the simulation should have been cancelled
        And "OnPassengerWaiting" should have fired exactly 0 times

    Scenario: Passengers can only have destination types of actually spawned stations
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        When the simulation runs for 5 hours
        Then all passengers should have a destination type that exists among spawned station types
