Feature: Station Spawning
    Metro stations appear on the map based on their SpawnDelayDays configuration.
    A delay of 0 means the station appears immediately at the start of the game.
    A delay of N days means the station appears at midnight on day N+1.

    Scenario Outline: Stations spawn at the correct time based on their delay
        Given a level with a <type> station at (<x>,<y>) with a spawn delay of <delay> days
        When the simulation runs for <hours> hours
        Then the <type> station at (<x>,<y>) <expectation>

        Examples: No delay — station spawns immediately on day 1
            | type   | x | y | delay | hours | expectation             |
            | Circle | 0 | 0 | 0     | 1     | should have spawned     |

        Examples: 1-day delay — station does not spawn on day 1 but spawns on day 2 at midnight
            | type   | x | y | delay | hours | expectation             |
            | Circle | 0 | 0 | 1     | 24    | should not have spawned |
            | Circle | 0 | 0 | 1     | 25    | should have spawned     |

        Examples: 5-day delay — station does not spawn before day 6 but spawns on day 6
            | type     | x | y | delay | hours | expectation             |
            | Triangle | 0 | 0 | 5     | 120   | should not have spawned |
            | Triangle | 0 | 0 | 5     | 121   | should have spawned     |

    Scenario: Only the immediate station has spawned after 1 hour when multiple stations have different delays
        Given a level with the following stations:
            | X | Y | Type     | SpawnDelay |
            | 0 | 0 | Circle   | 0          |
            | 1 | 0 | Triangle | 2          |
        When the simulation runs for 1 hour
        Then the Circle station at (0,0) should have spawned
        And the Triangle station at (1,0) should not have spawned

    Scenario: Both stations have spawned once enough game days have passed
        Given a level with the following stations:
            | X | Y | Type     | SpawnDelay |
            | 0 | 0 | Circle   | 0          |
            | 1 | 0 | Triangle | 2          |
        When the simulation runs for 49 hours
        Then the Circle station at (0,0) should have spawned
        And the Triangle station at (1,0) should have spawned

    Scenario: An immediately spawned station is present in the game snapshot
        Given a level with a Diamond station at (3,4) with a spawn delay of 0 days
        When the simulation runs for 1 hour
        Then the game snapshot should contain a Diamond station at (3,4)

    Scenario: A station that has not yet spawned is absent from the game snapshot
        Given a level with a Diamond station at (3,4) with a spawn delay of 3 days
        When the simulation runs for 24 hours
        Then the game snapshot should contain no stations
