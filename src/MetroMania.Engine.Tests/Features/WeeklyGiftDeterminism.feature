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
