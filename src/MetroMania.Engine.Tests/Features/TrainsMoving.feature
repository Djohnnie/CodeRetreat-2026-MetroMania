Feature: Trains Moving
  Verifies that trains are deployed, move along line paths, reverse at terminals,
  and can cross each other without blocking.
  
  All scenarios use stations at known grid positions and seed initial resources
  so actions take effect from tick 0 without waiting for weekly gifts.

  Scenario: A deployed train appears in the simulation
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then there should be 1 train in the simulation

  Scenario: A newly deployed train starts at the deployment station
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then the train should be at tile (0,0)

  Scenario: A train moves one tile per hour along the line
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 3 hours
    Then the train should be at tile (1,0)

  Scenario: A train reaches the far terminal after traversing the full path
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 7 hours
    Then the train should be at tile (5,0)

  Scenario: A train reverses after reaching the terminal
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 8 hours
    Then the train should be at tile (4,0)

  Scenario: A train completes a full round trip
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 12 hours
    Then the train should be at tile (0,0)

  Scenario: Two trains on the same line can cross freely when there are no passengers
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (4,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (4,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will deploy a train on the first line at station (4,0)
    When the simulation runs for 6 hours
    Then train 1 should be at tile (4,0)
    And train 2 should be at tile (1,0)
