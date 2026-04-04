Feature: Game Over
  Verifies the game-over condition fires exactly once when a station reaches 20 passengers,
  halts the simulation early, and identifies the correct overrun station.

  Scenario: OnGameOver fires when a station reaches 20 passengers
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 21 hours
    Then "OnGameOver" should have fired exactly 1 time

  Scenario: The simulation stops early after game over
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 21 hours
    Then the simulation should have produced fewer than 25 snapshots

  Scenario: OnGameOver does not fire before the threshold is reached
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 20 hours
    Then "OnGameOver" should have fired 0 times

  Scenario: OnGameOver fires only once even when the simulation is allowed to run much longer
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 1000 hours
    Then "OnGameOver" should have fired exactly 1 time

  Scenario: OnGameOver reports the overrun station type
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 21 hours
    Then OnGameOver should have fired with a Circle station
