Feature: Game Simulation
    The engine tracks game progress including days survived, hours elapsed,
    and game-over conditions triggered when a station accumulates 20+ passengers.

    Scenario: Game snapshot reflects the correct day and hour after a partial simulation
        Given an empty level
        When the simulation runs for 26 hours
        Then the snapshot should show day 2 and hour 1
        And the snapshot should show 26 total hours elapsed
        And the game should not be over

    Scenario: Game ends when a station accumulates too many passengers
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs until game over
        Then the score should be greater than 0
        And at least 20 passengers should have been spawned

    Scenario: DayOfWeek is correctly calculated across two weeks
        Given an empty level
        When the simulation runs for 337 hours
        Then the DayOfWeek values should cycle Monday through Sunday correctly
