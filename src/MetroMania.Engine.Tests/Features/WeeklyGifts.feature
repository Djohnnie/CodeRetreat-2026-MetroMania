Feature: Weekly Gifts
    Every Monday at hour 0 (day 1, 8, 15, ...) the engine calls OnWeeklyGiftReceived.
    The week number is 1-based: week 1 is the first Monday (abs tick 0 = day 1 hour 0),
    week 2 is abs tick 168 (day 8), week 3 is abs tick 336 (day 15), etc.
    The resource type is either taken from a deterministic override for that week number
    or drawn from a seeded RNG that always produces Line or Train.

    # Gift timing reference (number of ticks to process to receive N gifts):
    #   1 gift  → N=1   (includes abs tick 0, day 1 = first Monday)
    #   2 gifts → N=169  (includes abs tick 168, day 8 Monday)
    #   3 gifts → N=337  (includes abs tick 336, day 15 Monday)
    #   4 gifts → N=505  (includes abs tick 504, day 22 Monday)

    Scenario: Weekly gift fires on day 1 (which is a Monday)
        Given an empty level
        When the simulation runs for 1 hour
        Then "OnWeeklyGiftReceived" should have fired exactly 1 time

    Scenario: No second weekly gift fires before day 8
        Given an empty level
        When the simulation runs for 168 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 1 time

    Scenario: First weekly gift fires at the start of day 1 (first Monday)
        Given an empty level
        When the simulation runs for 1 hour
        Then "OnWeeklyGiftReceived" should have fired exactly 1 time

    Scenario: Second weekly gift fires at the start of day 8 (second Monday)
        Given an empty level
        When the simulation runs for 169 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 2 times

    Scenario: Third weekly gift fires at the start of day 15 (third Monday)
        Given an empty level
        When the simulation runs for 337 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 3 times

    Scenario: Fourth weekly gift fires at the start of day 22 (fourth Monday)
        Given an empty level
        When the simulation runs for 505 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 4 times

    Scenario Outline: Exact number of weekly gifts received matches elapsed Mondays
        Given an empty level
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

    Scenario: Week 2 override overrides the second gift only
        Given an empty level with seed 42
        And a weekly gift override for week 2 with resource type Train
        When the simulation runs for 169 hours
        Then weekly gift 2 should be of type Train

    Scenario: Multiple week overrides each fire with the correct resource type
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 2 with resource type Line
        And a weekly gift override for week 3 with resource type Line
        When the simulation runs for 337 hours
        Then weekly gift 1 should be of type Train
        And weekly gift 2 should be of type Line
        And weekly gift 3 should be of type Line

    Scenario: Non-overridden weeks produce only Line or Train
        Given an empty level with seed 42
        When the simulation runs for 505 hours
        Then all weekly gifts should be of type Line or Train

    Scenario: Non-overridden weeks produce only Line or Train for a different seed
        Given an empty level with seed 0
        When the simulation runs for 505 hours
        Then all weekly gifts should be of type Line or Train

    Scenario: Same seed produces identical weekly gift sequence across consecutive runs
        Given an empty level with seed 42
        When the simulation runs for 505 hours
        And the simulation runs again for 505 hours with the same seed
        Then both runs should have produced the same weekly gift sequence

    Scenario: Same seed determinism holds for seed 0
        Given an empty level with seed 0
        When the simulation runs for 505 hours
        And the simulation runs again for 505 hours with the same seed
        Then both runs should have produced the same weekly gift sequence

    Scenario: Same seed determinism holds for the maximum integer seed
        Given an empty level with seed 2147483647
        When the simulation runs for 505 hours
        And the simulation runs again for 505 hours with the same seed
        Then both runs should have produced the same weekly gift sequence

    Scenario: First weekly gift is received on day 1 at hour 0
        Given an empty level
        When the simulation runs for 1 hour
        Then weekly gift 1 should have been received on day 1 at hour 0

    Scenario: Second weekly gift is received on day 8 at hour 0
        Given an empty level
        When the simulation runs for 169 hours
        Then weekly gift 2 should have been received on day 8 at hour 0

    Scenario: Third weekly gift is received on day 15 at hour 0
        Given an empty level
        When the simulation runs for 337 hours
        Then weekly gift 3 should have been received on day 15 at hour 0

    Scenario: Overridden week 1 gift fires on the expected Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        When the simulation runs for 1 hour
        Then weekly gift 1 should be of type Train
        And weekly gift 1 should have been received on day 1 at hour 0

    Scenario: Four consecutive gifts are all received at the start of their respective Mondays
        Given an empty level
        When the simulation runs for 505 hours
        Then weekly gift 1 should have been received on day 1 at hour 0
        And weekly gift 2 should have been received on day 8 at hour 0
        And weekly gift 3 should have been received on day 15 at hour 0
        And weekly gift 4 should have been received on day 22 at hour 0
