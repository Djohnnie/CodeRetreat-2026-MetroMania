Feature: Station Crowded
  Verifies that OnStationCrowded fires every tick a station has 10 or more waiting passengers,
  stops being called once game over takes over, and reports the correct station.

  Scenario: OnStationCrowded fires once when a station first hits 10 passengers
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 11 hours
    Then "OnStationCrowded" should have fired exactly 1 time

  Scenario: OnStationCrowded does not fire before the threshold
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 10 hours
    Then "OnStationCrowded" should have fired 0 times

  Scenario: OnStationCrowded fires every tick while the station remains crowded
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 15 hours
    Then "OnStationCrowded" should have fired exactly 5 times

  Scenario: OnStationCrowded reports the crowded station type
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 11 hours
    Then OnStationCrowded should have fired with a Circle station

  Scenario: OnStationCrowded does not fire on the game-over tick
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and no passenger spawn phases
    When the simulation runs for 21 hours
    Then "OnStationCrowded" should have fired exactly 10 times
