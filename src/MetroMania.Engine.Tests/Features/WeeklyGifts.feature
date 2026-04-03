Feature: Weekly Gifts
    Every Monday at hour 0 (day 2, 9, 16, ...) the engine calls OnWeeklyGiftReceived.
    The week number is 1-based: week 1 is the first Monday (abs tick 24 = day 2 hour 0),
    week 2 is abs tick 192 (day 9), week 3 is abs tick 360 (day 16), etc.
    The resource type is either taken from a deterministic override for that week number
    or drawn from a seeded RNG that always produces Line or Train (never Wagon).

    # Gift timing reference (number of ticks to process to receive N gifts):
    #   1 gift  → N=25   (includes abs tick 24, the first Monday)
    #   2 gifts → N=193  (includes abs tick 192, day 9 Monday)
    #   3 gifts → N=361  (includes abs tick 360, day 16 Monday)
    #   4 gifts → N=529  (includes abs tick 528, day 23 Monday)

    Scenario: No weekly gift fires on day 1, which is a Sunday
        Given an empty level
        When the simulation runs for 24 hours
        Then "OnWeeklyGiftReceived" should have fired 0 times

    Scenario: First weekly gift fires at the start of day 2 (first Monday)
        Given an empty level
        When the simulation runs for 25 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 1 time

    Scenario: Second weekly gift fires at the start of day 9 (second Monday)
        Given an empty level
        When the simulation runs for 193 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 2 times

    Scenario: Third weekly gift fires at the start of day 16 (third Monday)
        Given an empty level
        When the simulation runs for 361 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 3 times

    Scenario: Fourth weekly gift fires at the start of day 23 (fourth Monday)
        Given an empty level
        When the simulation runs for 529 hours
        Then "OnWeeklyGiftReceived" should have fired exactly 4 times

    Scenario Outline: Exact number of weekly gifts received matches elapsed Mondays
        Given an empty level
        When the simulation runs for <hours> hours
        Then "OnWeeklyGiftReceived" should have fired exactly <gifts> times

        Examples:
            | hours | gifts |
            | 24    | 0     |
            | 25    | 1     |
            | 192   | 1     |
            | 193   | 2     |
            | 360   | 2     |
            | 361   | 3     |
            | 528   | 3     |
            | 529   | 4     |

    Scenario: Week 1 override with Train is used on the first Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        When the simulation runs for 25 hours
        Then weekly gift 1 should be of type Train

    Scenario: Week 1 override with Line is used on the first Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 25 hours
        Then weekly gift 1 should be of type Line

    Scenario: Week 1 override with Wagon is used on the first Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Wagon
        When the simulation runs for 25 hours
        Then weekly gift 1 should be of type Wagon

    Scenario: Week 2 override overrides the second gift only
        Given an empty level with seed 42
        And a weekly gift override for week 2 with resource type Wagon
        When the simulation runs for 193 hours
        Then weekly gift 2 should be of type Wagon

    Scenario: Multiple week overrides each fire with the correct resource type
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 2 with resource type Wagon
        And a weekly gift override for week 3 with resource type Line
        When the simulation runs for 361 hours
        Then weekly gift 1 should be of type Train
        And weekly gift 2 should be of type Wagon
        And weekly gift 3 should be of type Line

    Scenario: Non-overridden weeks produce only Line or Train (never Wagon from RNG)
        Given an empty level with seed 42
        When the simulation runs for 529 hours
        Then all weekly gifts should be of type Line or Train

    Scenario: Non-overridden weeks produce only Line or Train for a different seed
        Given an empty level with seed 0
        When the simulation runs for 529 hours
        Then all weekly gifts should be of type Line or Train

    Scenario: Same seed produces identical weekly gift sequence across consecutive runs
        Given an empty level with seed 42
        When the simulation runs for 529 hours
        And the simulation runs again for 529 hours with the same seed
        Then both runs should have produced the same weekly gift sequence

    Scenario: Same seed determinism holds for seed 0
        Given an empty level with seed 0
        When the simulation runs for 529 hours
        And the simulation runs again for 529 hours with the same seed
        Then both runs should have produced the same weekly gift sequence

    Scenario: Same seed determinism holds for the maximum integer seed
        Given an empty level with seed 2147483647
        When the simulation runs for 529 hours
        And the simulation runs again for 529 hours with the same seed
        Then both runs should have produced the same weekly gift sequence

    Scenario: First weekly gift is received on day 2 at hour 0
        Given an empty level
        When the simulation runs for 25 hours
        Then weekly gift 1 should have been received on day 2 at hour 0

    Scenario: Second weekly gift is received on day 9 at hour 0
        Given an empty level
        When the simulation runs for 193 hours
        Then weekly gift 2 should have been received on day 9 at hour 0

    Scenario: Third weekly gift is received on day 16 at hour 0
        Given an empty level
        When the simulation runs for 361 hours
        Then weekly gift 3 should have been received on day 16 at hour 0

    Scenario: Overridden week 1 gift fires on the expected Monday
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Wagon
        When the simulation runs for 25 hours
        Then weekly gift 1 should be of type Wagon
        And weekly gift 1 should have been received on day 2 at hour 0

    Scenario: Four consecutive gifts are all received at the start of their respective Mondays
        Given an empty level
        When the simulation runs for 529 hours
        Then weekly gift 1 should have been received on day 2 at hour 0
        And weekly gift 2 should have been received on day 9 at hour 0
        And weekly gift 3 should have been received on day 16 at hour 0
        And weekly gift 4 should have been received on day 23 at hour 0
