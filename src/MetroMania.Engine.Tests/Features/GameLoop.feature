Feature: Game Loop
    The engine's main tick loop drives simulation progress. Each iteration represents
    one game hour. "Running for N hours" processes ticks 0 through N-1 (inclusive);
    TotalHoursElapsed in each snapshot equals its zero-based tick index.
    Day 1 starts on a Monday (DayOfWeek 1) and advances one day per 24 ticks.

    Scenario: Running for 0 hours produces no snapshots and fires no events
        Given an empty level
        When the simulation runs for 0 hours
        Then "OnDayStart" should have fired 0 times
        And "OnHourTicked" should have fired 0 times

    Scenario: Running for 1 hour fires OnDayStart and OnHourTicked exactly once
        Given an empty level
        When the simulation runs for 1 hour
        Then "OnDayStart" should have fired exactly 1 time
        And "OnHourTicked" should have fired exactly 1 time

    Scenario: Running for 24 hours fires OnDayStart once and OnHourTicked 24 times
        Given an empty level
        When the simulation runs for 24 hours
        Then "OnDayStart" should have fired exactly 1 time
        And "OnHourTicked" should have fired exactly 24 times

    Scenario: Running for 48 hours fires OnDayStart twice and OnHourTicked 48 times
        Given an empty level
        When the simulation runs for 48 hours
        Then "OnDayStart" should have fired exactly 2 times
        And "OnHourTicked" should have fired exactly 48 times

    Scenario: Running for 72 hours fires OnDayStart three times
        Given an empty level
        When the simulation runs for 72 hours
        Then "OnDayStart" should have fired exactly 3 times

    Scenario Outline: OnDayStart fires exactly once per calendar day
        Given an empty level
        When the simulation runs for <hours> hours
        Then "OnDayStart" should have fired exactly <days> times

        Examples:
            | hours | days |
            | 1     | 1    |
            | 23    | 1    |
            | 24    | 1    |
            | 25    | 2    |
            | 47    | 2    |
            | 48    | 2    |
            | 49    | 3    |
            | 168   | 7    |
            | 169   | 8    |

    Scenario: The first tick reports day 1 and hour 0
        Given an empty level
        When the simulation runs for 1 hour
        Then the last tick should report day 1 and hour 0

    Scenario: The last tick of the first day reports day 1 and hour 23
        Given an empty level
        When the simulation runs for 24 hours
        Then the last tick should report day 1 and hour 23

    Scenario: The first tick of the second day reports day 2 and hour 0
        Given an empty level
        When the simulation runs for 25 hours
        Then the last tick should report day 2 and hour 0

    Scenario: The last tick of the second day reports day 2 and hour 23
        Given an empty level
        When the simulation runs for 48 hours
        Then the last tick should report day 2 and hour 23

    Scenario: TotalHoursElapsed on the first tick is 0
        Given an empty level
        When the simulation runs for 1 hour
        Then the last snapshot TotalHoursElapsed should be 0

    Scenario: TotalHoursElapsed on the last tick equals N minus 1 for N hours
        Given an empty level
        When the simulation runs for 50 hours
        Then the last snapshot TotalHoursElapsed should be 49

    Scenario: Day 1 is a Monday
        Given an empty level
        When the simulation runs for 1 hour
        Then the last tick DayOfWeek should be Monday

    Scenario: Day 2 is a Tuesday
        Given an empty level
        When the simulation runs for 25 hours
        Then the last tick DayOfWeek should be Tuesday

    Scenario Outline: DayOfWeek advances one step each game day
        Given an empty level
        When the simulation runs for <hours> hours
        Then the last tick DayOfWeek should be <dayOfWeek>

        Examples:
            | hours | dayOfWeek |
            | 1     | Monday    |
            | 25    | Tuesday   |
            | 49    | Wednesday |
            | 73    | Thursday  |
            | 97    | Friday    |
            | 121   | Saturday  |
            | 145   | Sunday    |
            | 169   | Monday    |

    Scenario: DayOfWeek completes a full week and returns to Monday on day 8
        Given an empty level
        When the simulation runs for 169 hours
        Then the last tick DayOfWeek should be Monday
        And "OnDayStart" should have fired exactly 8 times

    Scenario: OnDayStart is not fired on non-midnight hours
        Given an empty level
        When the simulation runs for 3 hours
        Then "OnDayStart" should have fired exactly 1 time
        And "OnHourTicked" should have fired exactly 3 times

    Scenario: OnHourTicked fires for every hour including midnight
        Given an empty level
        When the simulation runs for 2 hours
        Then "OnHourTicked" should have fired exactly 2 times

    Scenario: All tick DayOfWeek values follow the Monday-starting cycle
        Given an empty level
        When the simulation runs for 337 hours
        Then all tick DayOfWeek values should follow the Monday-starting cycle
