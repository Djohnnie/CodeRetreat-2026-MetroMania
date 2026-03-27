Feature: Event Ordering
    The game engine fires events on the IMetroManiaRunner in a strict three-phase order
    on every hour tick:

      Phase 1 — "Other" events fire first:
                OnStationSpawned, OnWeeklyGift, OnPassengerWaiting,
                OnStationOverrun, OnGameOver
      Phase 2 — OnDayStart fires second (only at midnight / hour 0)
      Phase 3 — OnHourTick fires last (every hour)

    Scenario: Station spawn fires before day start which fires before hour tick
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        When the simulation runs for 1 hour
        Then "OnStationSpawned" should have fired before "OnDayStart"
        And "OnDayStart" should have fired before "OnHourTick"

    Scenario: OnDayStart fires exactly once per day at midnight
        Given an empty level
        When the simulation runs for 48 hours
        Then "OnDayStart" should have fired exactly 2 times
        And "OnDayStart" should have fired for days 1 and 2

    Scenario: OnDayStart always fires immediately before OnHourTick on day boundaries
        Given an empty level
        When the simulation runs for 25 hours
        Then on each day boundary "OnDayStart" should fire directly before "OnHourTick"

    Scenario: OnHourTick fires every hour with the correct day and hour values
        Given an empty level
        When the simulation runs for 48 hours
        Then "OnHourTick" should have fired 48 times
        And each hour tick should report the correct day and hour

    Scenario: Weekly gift fires before day start on Monday
        Given an empty level
        When the simulation runs for 1 hour
        Then "OnWeeklyGift" should have fired before "OnDayStart"

    Scenario: Passenger waiting fires before day start when both occur on the same tick
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs for 25 hours
        Then the first "OnPassengerWaiting" should appear before the second "OnDayStart"

    Scenario: Station overrun notification fires before the player gets to act
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs for 11 hours
        Then "OnStationOverrun" should have fired directly before "OnHourTick"

    Scenario: Game over is the final event and ends the simulation immediately
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,9) with a spawn delay of 0 days
        When the simulation runs until game over
        Then "OnGameOver" should have fired exactly 1 time
        And "OnGameOver" should be the last event fired

    Scenario: All event types respect the three-phase ordering on day 1
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        When the simulation runs for 1 hour
        Then the first 4 events should be "OnStationSpawned", "OnWeeklyGift", "OnDayStart", "OnHourTick"

    Scenario: Non-midnight hours produce only hour tick events
        Given an empty level
        When the simulation runs for 3 hours
        Then "OnDayStart" should have fired exactly 1 time
        And "OnHourTick" should have fired 3 times
