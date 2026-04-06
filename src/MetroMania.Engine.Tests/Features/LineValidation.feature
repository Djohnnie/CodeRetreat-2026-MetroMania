Feature: Line Validation
  Verifies that the engine correctly rejects invalid CreateLine player actions
  with the appropriate error codes, and that rejected actions leave the game
  state unchanged.

  Background:
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Triangle station at (6,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Diamond station at (0,3) with a spawn delay of 0 days and no passenger spawn phases

  Scenario: Creating a line with a non-existent resource triggers error 100
    Given the runner will attempt to create a line with a non-existent resource
    When the simulation runs for 1 hour
    Then OnInvalidPlayerAction should have fired with code 100

  Scenario: Creating a line from a station to itself triggers error 102
    Given the level has 1 initial Line
    And the runner will attempt to create a line from station (0,0) to itself
    When the simulation runs for 1 hour
    Then OnInvalidPlayerAction should have fired with code 102

  Scenario: Extending a line from a non-terminal station triggers error 104
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will extend the first line from station (3,0) to station (6,0)
    And the runner will attempt to extend the first line from non-terminal (3,0) to (0,3)
    When the simulation runs for 3 hours
    Then OnInvalidPlayerAction should have fired with code 104

  Scenario: Extending a line to a station already on it triggers error 105
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to extend the first line from (3,0) back to (0,0)
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 105

  Scenario: An invalid CreateLine action does not create a line
    Given the runner will attempt to create a line with a non-existent resource
    When the simulation runs for 1 hour
    Then there should be 0 lines in the simulation

  Scenario: An invalid CreateLine action records NoAction in the snapshot
    Given the runner will attempt to create a line with a non-existent resource
    When the simulation runs for 1 hour
    Then the last snapshot LastAction should be NoAction
