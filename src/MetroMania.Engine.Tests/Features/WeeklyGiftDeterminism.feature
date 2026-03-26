Feature: Weekly Gift Determinism
    The engine seeds its random number generator with the LevelData seed,
    ensuring that OnWeeklyGift always triggers the same resource type
    sequence on every consecutive run of the same level.

    Scenario Outline: Same seed produces identical weekly gift sequence across consecutive runs
        Given an empty level with seed <seed>
        When the simulation runs for 700 hours
        And the simulation runs again for 700 hours with the same seed
        Then both runs should have produced the same weekly gift sequence
        And at least 4 weekly gifts should have been produced per run

        Examples:
            | seed       |
            | 42         |
            | 0          |
            | 2147483647 |

    Scenario: Overridden weeks produce the specified resource type
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 2 with resource type Wagon
        And a weekly gift override for week 3 with resource type Line
        When the simulation runs for 700 hours
        Then weekly gift 1 should be Train
        And weekly gift 2 should be Wagon
        And weekly gift 3 should be Line

    Scenario: Non-overridden weeks still produce random gifts
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Wagon
        When the simulation runs for 700 hours
        Then weekly gift 1 should be Wagon
        And at least 4 weekly gifts should have been produced per run

    Scenario: Mixed overrides and random gifts are deterministic across runs
        Given an empty level with seed 42
        And a weekly gift override for week 2 with resource type Train
        When the simulation runs for 700 hours
        And the simulation runs again for 700 hours with the same seed
        Then both runs should have produced the same weekly gift sequence
        And weekly gift 2 should be Train
