Feature: Player Action Validation
  Verifies that RemoveLine and RemoveVehicle actions are correctly rejected as
  not yet implemented (error code -1), and that rejected actions preserve the
  existing game state.

  Scenario: RemoveLine triggers error code -1
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first line
    When the simulation runs for 3 hours
    Then OnInvalidPlayerAction should have fired with code -1

  Scenario: RemoveVehicle triggers error code -1
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first train
    When the simulation runs for 3 hours
    Then OnInvalidPlayerAction should have fired with code -1

  Scenario: RemoveLine does not remove the line
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove the first line
    When the simulation runs for 2 hours
    Then there should be 1 line in the simulation

  Scenario: RemoveVehicle does not remove the train
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first train
    When the simulation runs for 3 hours
    Then there should be 1 train in the simulation
