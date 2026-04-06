Feature: Advanced Passenger Pickup and Drop-off
  Verifies advanced pickup rules: at most one operation per tick, oldest passenger
  is picked up first, and the delivery-before-transfer priority order.

  Scenario: A train picks up at most one passenger per tick
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the vehicle capacity is 6
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 3 hours
    Then the first train should have 1 passenger on board

  Scenario: The oldest waiting passenger is picked up first
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 3 hours
    Then the first passenger on the first train should have SpawnedAtHour 0

  Scenario: A train drops off at most one passenger per tick
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the vehicle capacity is 2
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 8 hours
    Then the score should be 1
