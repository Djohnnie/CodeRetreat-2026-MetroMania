Feature: Event Ordering
    Within each tick the engine fires events in a strict order:
      1. OnDayStart          — only at hour 0 of each day
      2. OnStationSpawned    — once per station that spawns this tick
      3. OnPassengerSpawned  — once per passenger that spawns this tick
      4. OnWeeklyGiftReceived — only on Monday (day 1, 8, 15, ...) at hour 0
      5. OnHourTicked        — every tick, always last

    Scenario: On the first tick OnDayStart fires before OnHourTicked
        Given an empty level
        When the simulation runs for 1 hour
        Then "OnDayStart" should have fired before "OnHourTicked"

    Scenario: First-tick event sequence on an empty level is OnDayStart, OnWeeklyGiftReceived, OnHourTicked
        Given an empty level
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 1 hour
        Then the event log should be "OnDayStart", "OnWeeklyGiftReceived", "OnHourTicked"

    Scenario: Station spawn fires after OnDayStart and before OnHourTicked
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        When the simulation runs for 1 hour
        Then "OnDayStart" should have fired before "OnStationSpawned"
        And "OnStationSpawned" should have fired before "OnHourTicked"

    Scenario: First-tick event sequence with a delay-0 station is OnDayStart, OnStationSpawned, OnWeeklyGiftReceived, OnHourTicked
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 1 hour
        Then the last 4 events should be "OnDayStart", "OnStationSpawned", "OnWeeklyGiftReceived", "OnHourTicked"

    Scenario: Passenger spawn fires after station spawn and before OnHourTicked
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        When the simulation runs for 1 hour
        Then "OnStationSpawned" should have fired before "OnPassengerSpawned"
        And "OnPassengerSpawned" should have fired before "OnHourTicked"

    Scenario: OnDayStart does not fire on the second tick (hour 1)
        Given an empty level
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 2 hours
        Then the event log should start with "OnDayStart", "OnWeeklyGiftReceived", "OnHourTicked"

    Scenario: OnHourTicked is always the last event in every tick
        Given an empty level
        When the simulation runs for 3 hours
        Then "OnHourTicked" should be the last event fired

    Scenario: Weekly gift fires after OnDayStart and before OnHourTicked on Monday
        Given an empty level
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 1 hour
        Then "OnDayStart" should have fired before "OnWeeklyGiftReceived"
        And "OnWeeklyGiftReceived" should have fired before "OnHourTicked"

    Scenario: Weekly gift fires exactly once in the first week when overridden
        Given an empty level
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 48 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 1 time

    Scenario: Monday tick sequence on an empty level is OnDayStart, OnWeeklyGiftReceived, OnHourTicked
        Given an empty level
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 1 hour
        Then the last 3 events should be "OnDayStart", "OnWeeklyGiftReceived", "OnHourTicked"

    Scenario: When a station spawns on Monday the full sequence includes all four events
        Given a level with a Circle station at (0,0) with a spawn delay of 7 days
        And a weekly gift override for week 2 with resource type Line
        When the simulation runs for 169 hours
        Then the last 4 events should be "OnDayStart", "OnStationSpawned", "OnWeeklyGiftReceived", "OnHourTicked"

    Scenario: Passenger spawn event falls between station spawn and weekly gift on a Monday spawn tick
        Given a level with a Circle station at (0,0) with a spawn delay of 7 days and passengers every 24 hours
        And a level with a Triangle station at (9,0) with a spawn delay of 0 days and no passenger spawn phases
        And a weekly gift override for week 2 with resource type Line
        When the simulation runs for 169 hours
        Then the last 5 events should be "OnDayStart", "OnStationSpawned", "OnPassengerSpawned", "OnWeeklyGiftReceived", "OnHourTicked"

    Scenario: OnDayStart fires exactly once at midnight of each new day across 3 days
        Given an empty level
        When the simulation runs for 72 hours
        Then "OnDayStart" should have fired exactly 3 times

    Scenario: OnHourTicked fires for every single hour of the simulation
        Given an empty level
        When the simulation runs for 72 hours
        Then "OnHourTicked" should have fired exactly 72 times

    Scenario: Two stations spawning on the same tick both fire OnStationSpawned before OnHourTicked
        Given a level with the following stations:
            | X | Y | Type     | SpawnDelay |
            | 0 | 0 | Circle   | 0          |
            | 1 | 0 | Triangle | 0          |
        When the simulation runs for 1 hour
        Then "OnStationSpawned" should have fired before "OnHourTicked"
        And "OnStationSpawned" should have fired exactly 2 times
