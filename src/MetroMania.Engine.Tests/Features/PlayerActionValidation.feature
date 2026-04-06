Feature: Player Action Validation
  Verifies that RemoveLine and RemoveVehicle player actions are correctly
  validated and applied.

  Scenario: RemoveLine removes an empty line with no trains
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove the first line
    When the simulation runs for 2 hours
    Then there should be 0 lines in the simulation
    And "OnLineRemoved" should have fired exactly 1 times
    And there should be 0 in-use Line resources

  Scenario: RemoveVehicle immediately removes an empty train
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first train
    When the simulation runs for 3 hours
    Then there should be 0 trains in the simulation
    And there should be 0 in-use Train resources

  Scenario: RemoveVehicle on non-existent train triggers error
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove a non-existent train
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 300

  Scenario: RemoveLine on non-existent line triggers error
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove a non-existent line
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 400
