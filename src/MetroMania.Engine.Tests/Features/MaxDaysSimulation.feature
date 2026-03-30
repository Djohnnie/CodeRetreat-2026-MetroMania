Feature: Max Days Simulation Limit
    The engine respects a maximum number of days configured on the level.
    When the limit is reached the simulation ends cleanly without triggering
    a game-over, returning the score and stats from the last completed day.

    Scenario: Simulation stops after the configured max days
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 999 hours
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days and passengers every 999 hours
        And the level has a max days limit of 3 days
        When the simulation runs until game over
        Then the result should show exactly 3 days survived
        And "OnGameOver" should have fired exactly 0 times

    Scenario: Simulation runs exactly MaxDays multiplied by 24 hour ticks
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 999 hours
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days and passengers every 999 hours
        And the level has a max days limit of 5 days
        When the simulation runs until game over
        Then the simulation should have run exactly 120 hour ticks

    Scenario: Game over still takes priority when it occurs before max days
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days and passengers every 1 hours
        And the level has a max days limit of 100 days
        When the simulation runs until game over
        Then "OnGameOver" should have fired exactly 1 times
        And the result should show exactly 1 days survived
