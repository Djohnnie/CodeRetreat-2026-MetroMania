Feature: Game Snapshot Properties
  Verifies that GameSnapshot records contain correct time, state, and metadata,
  and that the SimulationResult accurately summarises the run.

  Scenario: The first snapshot has day 1 and hour 0
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 1 hour
    Then the last snapshot should have day 1 and hour 0

  Scenario: A deployed train starts with direction 1
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then the first train should have direction 1

  Scenario: LastAction reflects the player action when valid
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (3,0)
    When the simulation runs for 1 hour
    Then the last snapshot LastAction should be CreateLine

  Scenario: The simulation result TotalScore equals the last snapshot score
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 7 hours
    Then the simulation result TotalScore should equal the last snapshot score

  Scenario: The simulation result counts player actions excluding NoAction
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    When the simulation runs for 3 hours
    Then the simulation result NumberOfPlayerActions should be 2

  Scenario: All snapshots have sequential TotalHoursElapsed values
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 10 hours
    Then all snapshots should have sequential TotalHoursElapsed values starting at 0

  Scenario: The simulation result DaysSurvived reflects elapsed full days
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 48 hours
    Then the simulation result DaysSurvived should be 2

  Scenario: Game over snapshot is included in the snapshot history
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 100 hours
    Then the game over snapshot should be included in the snapshot history
