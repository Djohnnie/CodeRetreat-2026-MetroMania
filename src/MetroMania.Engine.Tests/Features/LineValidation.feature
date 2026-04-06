Feature: Line Validation
  Verifies that the engine correctly rejects invalid CreateLine, ExtendLineFromTerminal,
  and ExtendLineInBetween player actions with the appropriate error codes, and that
  rejected actions leave the game state unchanged.

  Background:
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Triangle station at (6,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Diamond station at (0,3) with a spawn delay of 0 days and no passenger spawn phases

  # ── CreateLine ──────────────────────────────────────────────────────

  Scenario: Creating a line with a non-existent resource triggers error 100
    Given the runner will attempt to create a line with a non-existent resource
    When the simulation runs for 1 hour
    Then OnInvalidPlayerAction should have fired with code 100

  Scenario: Creating a line from a station to itself triggers error 102
    Given the level has 1 initial Line
    And the runner will attempt to create a line from station (0,0) to itself
    When the simulation runs for 1 hour
    Then OnInvalidPlayerAction should have fired with code 102

  Scenario: An invalid CreateLine action does not create a line
    Given the runner will attempt to create a line with a non-existent resource
    When the simulation runs for 1 hour
    Then there should be 0 lines in the simulation

  Scenario: An invalid CreateLine action records NoAction in the snapshot
    Given the runner will attempt to create a line with a non-existent resource
    When the simulation runs for 1 hour
    Then the last snapshot LastAction should be NoAction

  # ── ExtendLineFromTerminal ──────────────────────────────────────────

  Scenario: Extending a line from a non-terminal station triggers error 105
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will extend the first line from station (3,0) to station (6,0)
    And the runner will attempt to extend the first line from non-terminal (3,0) to (0,3)
    When the simulation runs for 3 hours
    Then OnInvalidPlayerAction should have fired with code 105

  Scenario: Extending a line to a station already on it triggers error 106
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to extend the first line from (3,0) back to (0,0)
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 106

  Scenario: Extending a non-existent line triggers error 104
    Given the level has 1 initial Line
    And the runner will attempt to extend a non-existent line from (0,0) to (3,0)
    When the simulation runs for 1 hour
    Then OnInvalidPlayerAction should have fired with code 104

  Scenario: LastAction reflects ExtendLineFromTerminal when valid
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will extend the first line from station (3,0) to station (6,0)
    When the simulation runs for 2 hours
    Then the last snapshot LastAction should be ExtendLineFromTerminal

  # ── ExtendLineInBetween ─────────────────────────────────────────────

  Scenario: Inserting a station between two consecutive stations succeeds
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (6,0)
    And the runner will insert station (3,0) between stations (0,0) and (6,0) on the first line
    When the simulation runs for 2 hours
    Then the first line should have 3 stations
    And the last snapshot LastAction should be ExtendLineInBetween

  Scenario: Inserting on a non-existent line triggers error 107
    Given the level has 1 initial Line
    And the runner will attempt to insert station (3,0) on a non-existent line between (0,0) and (6,0)
    When the simulation runs for 1 hour
    Then OnInvalidPlayerAction should have fired with code 107

  Scenario: Inserting between non-consecutive stations triggers error 108
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will extend the first line from station (3,0) to station (6,0)
    And the runner will attempt to insert station (0,3) between non-consecutive stations (0,0) and (6,0) on the first line
    When the simulation runs for 3 hours
    Then OnInvalidPlayerAction should have fired with code 108

  Scenario: Inserting a station already on the line triggers error 109
    Given the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (6,0)
    And the runner will attempt to insert existing station (0,0) between (0,0) and (6,0) on the first line
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 109
