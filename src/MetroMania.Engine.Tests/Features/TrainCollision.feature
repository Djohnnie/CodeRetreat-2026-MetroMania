Feature: Train Collision
  Verifies collision rules: two trains cannot occupy the same station tile simultaneously,
  a line cannot hold more trains than it has stations, and trains cross freely on non-station tiles.

  Scenario: Deploying a train on a tile already occupied by another train is rejected
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (4,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (4,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 3 hours
    Then OnInvalidPlayerAction should have fired with code 205

  Scenario: Deploying a third train on a two-station line is rejected
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (4,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 3 initial Trains
    And the runner will create a line between stations at (0,0) and (4,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will deploy a train on the first line at station (4,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 4 hours
    Then OnInvalidPlayerAction should have fired with code 204

  Scenario: Two trains on the same line can cross on non-station tiles
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (4,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (4,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will deploy a train on the first line at station (4,0)
    When the simulation runs for 6 hours
    Then train 1 should be at tile (4,0)
    And train 2 should be at tile (1,0)
