Feature: Determinism
  Verifies that the simulation produces identical results when run with the same
  level configuration and seed. All randomness (passenger destinations, weekly
  gifts) is seeded so replays are perfectly reproducible.

  Scenario: Same level and seed produce identical passenger spawn counts across runs
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 12 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and passengers every 12 hours
    When the simulation runs for 50 hours
    And the simulation runs again for 50 hours with the same seed
    Then both runs should have produced the same number of passenger spawn events

  Scenario: Same level and seed produce identical snapshot counts and scores
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 10 hours
    Then the simulation should have produced 10 snapshots
    And the score should be 1
