Feature: Snapshots
  Verifies that the simulation produces the correct number of snapshots, that initial
  resources appear in the first snapshot, and that lines and trains appear in snapshots
  after the player actions that create them.

  Scenario: Running for N hours produces N snapshots
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 5 hours
    Then the simulation should have produced 5 snapshots

  Scenario: The first snapshot starts with score 0
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 1 hour
    Then the first snapshot should have score 0

  Scenario: Initial resources appear in the first snapshot as the week 1 gifts
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    When the simulation runs for 1 hour
    Then the last snapshot should contain 2 resources

  Scenario: A created line appears in snapshots after the CreateLine action
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    When the simulation runs for 2 hours
    Then there should be 1 line in the simulation

  Scenario: A deployed train appears in snapshots after the AddVehicleToLine action
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then there should be 1 train in the simulation
