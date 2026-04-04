Feature: Passenger Delivery
  Verifies that passengers are dropped off at matching-type stations and score is awarded.
  Uses a 4-tile path (0,0)→(1,0)→(2,0)→(3,0) with one Circle and one Rectangle station.

  Scenario: Score is 1 after the first successful delivery
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 7 hours
    Then the score should be 1

  Scenario: The delivering train has 0 passengers on board after the drop-off
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 7 hours
    Then the first train should have 0 passengers on board

  Scenario: No score is awarded before the train reaches the destination
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 6 hours
    Then the score should be 0

  Scenario: Score is 2 after two round trips with capacity 1
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Lines and 1 initial Trains
    And the vehicle capacity is 1
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 15 hours
    Then the score should be 2
