Feature: Weekly Gifts
    Every Monday at hour 0 (day 1, 8, 15, ...) the engine checks for a weekly gift override.
    The week number is 1-based: week 1 is the first Monday (abs tick 0 = day 1 hour 0),
    week 2 is abs tick 168 (day 8), week 3 is abs tick 336 (day 15), etc.
    Only weeks with an explicit override in the level data produce a gift.
    Weeks without an override are silently skipped — no random gift is awarded.
    This gives level designers complete control over the gifting schedule.

    # Gift timing reference (number of ticks to process to reach N-th Monday):
    #   Monday 1 → N=1   (includes abs tick 0, day 1)
    #   Monday 2 → N=169  (includes abs tick 168, day 8)
    #   Monday 3 → N=337  (includes abs tick 336, day 15)
    #   Monday 4 → N=505  (includes abs tick 504, day 22)

    Scenario: No gift fires on an empty level without overrides
        Given an empty level
        When the simulation runs for 1 hour
        Then "OnWeeklyGiftReceived" should have fired exactly 0 times

    Scenario: No gifts fire across multiple weeks without overrides
        Given an empty level
        When the simulation runs for 505 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 0 times

    Scenario: Week 1 override with Train is used on the first Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        When the simulation runs for 1 hour
        Then weekly gift 1 should be of type Train

    Scenario: Week 1 override with Line is used on the first Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 1 hour
        Then weekly gift 1 should be of type Line

    Scenario: Only overridden weeks produce gifts
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 3 with resource type Line
        When the simulation runs for 505 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 2 times
        And weekly gift 1 should be of type Train
        And weekly gift 2 should be of type Line

    Scenario: Week 2 override fires on the second Monday
        Given an empty level with seed 42
        And a weekly gift override for week 2 with resource type Train
        When the simulation runs for 169 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 1 time
        And weekly gift 1 should be of type Train

    Scenario: Multiple week overrides each fire with the correct resource type
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 2 with resource type Line
        And a weekly gift override for week 3 with resource type Line
        When the simulation runs for 337 hours
        Then weekly gift 1 should be of type Train
        And weekly gift 2 should be of type Line
        And weekly gift 3 should be of type Line

    Scenario: Overridden week 1 gift fires on the expected Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        When the simulation runs for 1 hour
        Then weekly gift 1 should be of type Train
        And weekly gift 1 should have been received on day 1 at hour 0

    Scenario: Four consecutive overridden gifts are received at the start of their respective Mondays
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Line
        And a weekly gift override for week 2 with resource type Train
        And a weekly gift override for week 3 with resource type Line
        And a weekly gift override for week 4 with resource type Train
        When the simulation runs for 505 hours
        Then weekly gift 1 should have been received on day 1 at hour 0
        And weekly gift 2 should have been received on day 8 at hour 0
        And weekly gift 3 should have been received on day 15 at hour 0
        And weekly gift 4 should have been received on day 22 at hour 0

    Scenario: Same overrides produce identical gifts across consecutive runs
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 2 with resource type Line
        And a weekly gift override for week 3 with resource type Train
        And a weekly gift override for week 4 with resource type Line
        When the simulation runs for 505 hours
        And the simulation runs again for 505 hours with the same seed
        Then both runs should have produced the same weekly gift sequence

    Scenario Outline: Gift count matches number of overridden weeks within the simulated timeframe
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Line
        And a weekly gift override for week 2 with resource type Train
        And a weekly gift override for week 3 with resource type Line
        And a weekly gift override for week 4 with resource type Train
        When the simulation runs for <hours> hours
        Then "OnWeeklyGiftReceived" should have fired exactly <gifts> times

        Examples:
            | hours | gifts |
            | 0     | 0     |
            | 1     | 1     |
            | 168   | 1     |
            | 169   | 2     |
            | 336   | 2     |
            | 337   | 3     |
            | 504   | 3     |
            | 505   | 4     |

    Scenario: Multiple gifts in the same week are all awarded
        Given an empty level with seed 42
        And a weekly gift override for week 2 with resource type Line
        And a weekly gift override for week 2 with resource type Train
        When the simulation runs for 169 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 2 times
        And weekly gift 1 should be of type Line
        And weekly gift 2 should be of type Train

    Scenario: Multiple gifts in the same week each add a resource to the snapshot
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Line
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 1 hour
        Then "OnWeeklyGiftReceived" should have fired exactly 3 times
        And weekly gift 1 should be of type Line
        And weekly gift 2 should be of type Train
        And weekly gift 3 should be of type Line

    Scenario: Weeks with multiple gifts and weeks with single gifts can coexist
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Line
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 2 with resource type Line
        And a weekly gift override for week 3 with resource type Train
        And a weekly gift override for week 3 with resource type Line
        When the simulation runs for 337 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 5 times
        And weekly gift 1 should be of type Line
        And weekly gift 2 should be of type Train
        And weekly gift 3 should be of type Line
        And weekly gift 4 should be of type Train
        And weekly gift 5 should be of type Line
