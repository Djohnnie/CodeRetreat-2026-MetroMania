Feature: Passenger Pickup
  Verifies that trains pick up waiting passengers, leave the station empty,
  stay at the station tile for one tick while loading, and respect vehicle capacity.

  Scenario: A train picks up a waiting passenger
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 3 hours
    Then the first train should have 1 passenger on board

  Scenario: The origin station is empty after a train picks up the passenger
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 3 hours
    Then there should be 0 passengers waiting at station (0,0)

  Scenario: A train stays at the station tile for the pickup tick
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 3 hours
    Then the train should be at tile (0,0)

  Scenario: A train does not exceed vehicle capacity
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the vehicle capacity is 1
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 4 hours
    Then the first train should have 1 passenger on board
