Feature: Game Over Thresholds
    The engine monitors passenger counts per station. A station with 10 or more
    passengers triggers OnStationOverrun. A station with 20 or more passengers
    triggers OnGameOver and ends the simulation immediately, before OnHourTick
    fires for that tick.

    Scenario: Station overrun fires when a station reaches exactly 10 passengers
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        When the simulation runs for 11 hours
        Then "OnStationOverrun" should have fired exactly 1 time
        And the first overrun event should have 10 passengers at (0,0)

    Scenario: Game over fires when a station reaches exactly 20 passengers
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        When the simulation runs until game over
        Then "OnGameOver" should have fired exactly 1 time
        And the game over event should have 20 passengers at (0,0)

    Scenario: No OnHourTick fires on the same tick as game over
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        When the simulation runs until game over
        Then "OnHourTick" should have fired 20 times
        And "OnGameOver" should be the last event fired
